using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// O fluxo da tela de usuários (US-014) pelo site inteiro contra o SQL Server real: Identity, transação serializável com a estratégia de
/// retentativa, auditoria e cookies. É o que os testes de unidade só conseguem simular em SQLite.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class UserFlowTests
#pragma warning restore CA1515
{
    private const string AdminEmail = "marcos@exemplo.com.br";
    private const string AnaEmail = "ana.souza@exemplo.com.br";
    private const string Password = "Senha@Forte1";
    private const string Provisional = "Provis0ria!";

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US014_CriarMudarPapelDesativarReativarERedefinir_NoSqlServerReal()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());
        await factory.CreateUserAsync(AdminEmail, "Marcos Silva", Password, RoleNames.Administrator);
        using HttpClient admin = factory.CreateBrowser();
        Assert.AreEqual("/painel/anuncios/fila", (await admin.SignInAsync(AdminEmail, Password)).Destination());

        // S01: cria Ana; ela entra com a provisória e é levada a definir a nova senha
        HttpResponseMessage created = await admin.PostFormAsync("/painel/usuarios/novo", "/painel/usuarios/novo", new Dictionary<string, string>
        {
            ["FullName"] = "Ana Souza",
            ["Email"] = AnaEmail,
            ["Role"] = RoleNames.Writer,
            ["ProvisionalPassword"] = Provisional
        });
        Assert.AreEqual("/painel/usuarios", created.Destination());
        using HttpClient ana = factory.CreateBrowser();
        Assert.AreEqual("/painel/definir-senha", (await ana.SignInAsync(AnaEmail, Provisional)).Destination());

        int anaId = await IdOfAsync(factory, AnaEmail);

        // S05 e S06: e-mail repetido e senha fraca não criam nada
        string duplicate = await (await admin.PostFormAsync("/painel/usuarios/novo", "/painel/usuarios/novo", new Dictionary<string, string>
        {
            ["FullName"] = "Outra",
            ["Email"] = AnaEmail.ToUpperInvariant(),
            ["Role"] = RoleNames.Writer,
            ["ProvisionalPassword"] = Provisional
        })).TextAsync();
        StringAssert.Contains(duplicate, "Já existe um usuário com este e-mail");

        // S02: muda o papel
        await admin.PostFormAsync($"/painel/usuarios/{anaId}/editar", $"/painel/usuarios/{anaId}/editar", new Dictionary<string, string> { ["Role"] = RoleNames.Administrator });
        Assert.AreEqual(RoleNames.Administrator, (await RoleOfAsync(factory, anaId)));

        // S09: com dois Administradores ativos, volta a Redator; Marcos, agora único, não pode se rebaixar
        await admin.PostFormAsync($"/painel/usuarios/{anaId}/editar", $"/painel/usuarios/{anaId}/editar", new Dictionary<string, string> { ["Role"] = RoleNames.Writer });
        int marcosId = await IdOfAsync(factory, AdminEmail);
        string lastAdmin = await (await admin.PostFormAsync($"/painel/usuarios/{marcosId}/editar", $"/painel/usuarios/{marcosId}/editar", new Dictionary<string, string> { ["Role"] = RoleNames.Writer })).TextAsync();
        StringAssert.Contains(lastAdmin, "Deve existir ao menos um administrador ativo");

        // S03 e S04: desativa e reativa
        await admin.PostFormAsync($"/painel/usuarios/{anaId}/desativar", $"/painel/usuarios/{anaId}/desativar");
        using HttpClient blocked = factory.CreateBrowser();
        StringAssert.Contains(await (await blocked.SignInAsync(AnaEmail, Provisional)).TextAsync(), "E-mail ou senha inválidos, ou conta desativada");
        await admin.PostFormAsync("/painel/usuarios", $"/painel/usuarios/{anaId}/reativar");
        using HttpClient again = factory.CreateBrowser();
        Assert.AreEqual("/painel/definir-senha", (await again.SignInAsync(AnaEmail, Provisional)).Destination());

        // S10: redefine; a provisória antiga deixa de valer
        const string Second = "Segund@Provis0ria";
        await admin.PostFormAsync($"/painel/usuarios/{anaId}/redefinir-senha", $"/painel/usuarios/{anaId}/redefinir-senha", new Dictionary<string, string> { ["ProvisionalPassword"] = Second });
        using HttpClient old = factory.CreateBrowser();
        StringAssert.Contains(await (await old.SignInAsync(AnaEmail, Provisional)).TextAsync(), "E-mail ou senha inválidos");
        using HttpClient fresh = factory.CreateBrowser();
        Assert.AreEqual("/painel/definir-senha", (await fresh.SignInAsync(AnaEmail, Second)).Destination());

        // RC-16: tudo ficou na auditoria, com o Administrador como ator
        using IServiceScope scope = factory.Services.CreateScope();
        List<AuditEntry> entries = await scope.ServiceProvider.GetRequiredService<AppDbContext>().AuditEntries.AsNoTracking()
            .Where(e => e.TargetType == "User").OrderBy(e => e.Id).ToListAsync();
        CollectionAssert.AreEqual(
            new[] { "user.create", "user.change_role", "user.change_role", "user.change_role", "user.deactivate", "user.reactivate", "user.reset_password" },
            entries.Select(e => e.Action).ToArray());
        CollectionAssert.AreEqual(
            new[] { AuditResult.Success, AuditResult.Success, AuditResult.Success, AuditResult.Denied, AuditResult.Success, AuditResult.Success, AuditResult.Success },
            entries.Select(e => e.Result).ToArray());
        Assert.IsTrue(entries.All(e => e.ActorId == marcosId));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Redator_NaoAcessaATelaDeUsuarios_NoSqlServerReal()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());
        await factory.CreateUserAsync(AnaEmail, "Ana Souza", Password, RoleNames.Writer);
        using HttpClient writer = factory.CreateBrowser();
        await writer.SignInAsync(AnaEmail, Password);

        HttpResponseMessage response = await writer.GetAsync("/painel/usuarios");

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        StringAssert.StartsWith(response.Destination(), "/painel/acesso-negado");
    }

    private static async Task<int> IdOfAsync(IntegrationWebFactory factory, string email)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.AsNoTracking().SingleAsync(u => u.Email == email)).Id;
    }

    private static async Task<string> RoleOfAsync(IntegrationWebFactory factory, int id)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.UserRoles.Where(ur => ur.UserId == id).Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name).SingleAsync();
    }
}
