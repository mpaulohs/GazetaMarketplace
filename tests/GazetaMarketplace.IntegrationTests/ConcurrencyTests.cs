using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// "Sempre resta ao menos um Administrador ativo" (US-014-S09) com duas requisições ao mesmo tempo. O SQLite dos testes de unidade serializa
/// tudo e não prova a transação serializável; aqui são dois escopos, dois contextos e duas conexões num SQL Server de verdade.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ConcurrencyTests
#pragma warning restore CA1515
{
    private const string Password = "Senha@Forte1";

    private static async Task<UserManagementResult> ChangeRoleAsync(IntegrationWebFactory factory, int id, string role)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IUserManagement>().ChangeRoleAsync(id, role, CancellationToken.None);
    }

    private static async Task<UserManagementResult> DeactivateAsync(IntegrationWebFactory factory, int id)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IUserManagement>().DeactivateAsync(id, CancellationToken.None);
    }

    private static async Task<int> ActiveAdministratorsAsync(IntegrationWebFactory factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        return (await users.GetUsersInRoleAsync(RoleNames.Administrator)).Count(u => u.IsActive);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DoisAdministradoresSeRebaixandoAoMesmoTempo_UmSoConsegue_ESempreRestaUmAtivo()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());

        for (int round = 1; round <= 8; round++)
        {
            AppUser a = await factory.CreateUserAsync($"a{round}@exemplo.com.br", "Admin A " + round, Password, RoleNames.Administrator);
            AppUser b = await factory.CreateUserAsync($"b{round}@exemplo.com.br", "Admin B " + round, Password, RoleNames.Administrator);
            // Os Administradores das rodadas anteriores saem do caminho, para só estes dois estarem ativos
            await DeactivateOthersAsync(factory, a.Id, b.Id);

            UserManagementResult[] results = await Task.WhenAll(
                Task.Run(() => ChangeRoleAsync(factory, a.Id, RoleNames.Writer)),
                Task.Run(() => ChangeRoleAsync(factory, b.Id, RoleNames.Writer)));

            Assert.AreEqual(1, results.Count(r => r.Succeeded), $"rodada {round}: exatamente um dos dois consegue");
            Assert.AreEqual(1, results.Count(r => !r.Succeeded && r.Errors.Any(e => e.Message == UserManagementMessages.LastAdministrator)));
            Assert.AreEqual(1, await ActiveAdministratorsAsync(factory), $"rodada {round}: sobra um Administrador ativo");
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DoisAdministradoresSeDesativandoAoMesmoTempo_UmSoConsegue_ESempreRestaUmAtivo()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());

        for (int round = 1; round <= 8; round++)
        {
            AppUser a = await factory.CreateUserAsync($"a{round}@exemplo.com.br", "Admin A " + round, Password, RoleNames.Administrator);
            AppUser b = await factory.CreateUserAsync($"b{round}@exemplo.com.br", "Admin B " + round, Password, RoleNames.Administrator);
            await DeactivateOthersAsync(factory, a.Id, b.Id);

            UserManagementResult[] results = await Task.WhenAll(
                Task.Run(() => DeactivateAsync(factory, a.Id)),
                Task.Run(() => DeactivateAsync(factory, b.Id)));

            Assert.AreEqual(1, results.Count(r => r.Succeeded), $"rodada {round}: exatamente um dos dois consegue");
            Assert.AreEqual(1, await ActiveAdministratorsAsync(factory), $"rodada {round}: sobra um Administrador ativo");
        }
    }

    private static async Task DeactivateOthersAsync(IntegrationWebFactory factory, params int[] keep)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        List<AppUser> others = (await users.GetUsersInRoleAsync(RoleNames.Administrator)).Where(u => u.IsActive && !keep.Contains(u.Id)).ToList();
        foreach (AppUser other in others)
        {
            other.IsActive = false;
            await users.UpdateAsync(other);
        }
    }
}
