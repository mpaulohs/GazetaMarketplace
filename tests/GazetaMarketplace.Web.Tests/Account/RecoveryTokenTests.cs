using System;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Account;

/// <summary>O token do link de redefinição (NFR-09): 1 hora, uso único e amarrado a uma pessoa.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class RecoveryTokenTests
#pragma warning restore CA1515
{
    private const string Purpose = UserManager<AppUser>.ResetPasswordTokenPurpose;

    private static async Task<(WebFactory Factory, AppUser Ana, AppUser Bia)> NewFactoryAsync()
    {
        WebFactory factory = new(withDatabase: true);
        AppUser ana = await factory.CreateUserAsync("ana@exemplo.com.br", "Ana", "Senha@Forte1", RoleNames.Writer);
        AppUser bia = await factory.CreateUserAsync("bia@exemplo.com.br", "Bia", "Senha@Forte1", RoleNames.Writer);
        return (factory, ana, bia);
    }

    private static async Task<RecoveryLinkState> InspectAsync(WebFactory factory, string token, string email)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        return scope.ServiceProvider.GetRequiredService<RecoveryTokenProvider>().Inspect(Purpose, token, await users.FindByEmailAsync(email));
    }

    private static async Task<string> GenerateAsync(WebFactory factory, string email)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        return await users.GeneratePasswordResetTokenAsync(await users.FindByEmailAsync(email));
    }

    [TestMethod]
    public async Task Token_ExpiraEmUmaHora()
    {
        (WebFactory factory, _, _) = await NewFactoryAsync();
        using (factory)
        {
            string token = await GenerateAsync(factory, "ana@exemplo.com.br");

            Assert.AreEqual(RecoveryLinkState.Valid, await InspectAsync(factory, token, "ana@exemplo.com.br"));
            factory.Clock.Now = factory.Clock.Now.AddMinutes(59).AddSeconds(59);
            Assert.AreEqual(RecoveryLinkState.Valid, await InspectAsync(factory, token, "ana@exemplo.com.br"), "ainda dentro da hora");
            factory.Clock.Now = factory.Clock.Now.AddSeconds(2);
            Assert.AreEqual(RecoveryLinkState.Expired, await InspectAsync(factory, token, "ana@exemplo.com.br"), "passou de 1 hora");
        }
    }

    [TestMethod]
    public async Task Token_QueVirouUsado_NaoValeDeNovo()
    {
        (WebFactory factory, _, _) = await NewFactoryAsync();
        using (factory)
        {
            string token = await GenerateAsync(factory, "ana@exemplo.com.br");
            using (IServiceScope scope = factory.Services.CreateScope())
            {
                UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                IdentityResult change = await users.ResetPasswordAsync(await users.FindByEmailAsync("ana@exemplo.com.br"), token, "Nova@Senha2");
                Assert.IsTrue(change.Succeeded);
            }

            Assert.AreEqual(RecoveryLinkState.Used, await InspectAsync(factory, token, "ana@exemplo.com.br"), "o carimbo de segurança mudou");

            using (IServiceScope scope = factory.Services.CreateScope())
            {
                UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                IdentityResult again = await users.ResetPasswordAsync(await users.FindByEmailAsync("ana@exemplo.com.br"), token, "Outra@Senha3");
                Assert.IsFalse(again.Succeeded, "o mesmo token não troca a senha duas vezes");
            }
        }
    }

    [TestMethod]
    public async Task Token_DeUmaPessoa_NaoValeParaOutra()
    {
        (WebFactory factory, _, _) = await NewFactoryAsync();
        using (factory)
        {
            string token = await GenerateAsync(factory, "ana@exemplo.com.br");

            Assert.AreEqual(RecoveryLinkState.Invalid, await InspectAsync(factory, token, "bia@exemplo.com.br"));
        }
    }

    [TestMethod]
    public async Task Token_DeOutroPropositoDoIdentity_NaoServeParaRedefinirSenha()
    {
        (WebFactory factory, AppUser ana, _) = await NewFactoryAsync();
        using (factory)
        {
            using IServiceScope scope = factory.Services.CreateScope();
            UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            string other = await users.GenerateUserTokenAsync(ana, RecoveryTokenProvider.Name, "EmailConfirmation");

            Assert.AreEqual(RecoveryLinkState.Invalid, scope.ServiceProvider.GetRequiredService<RecoveryTokenProvider>().Inspect(Purpose, other, ana));
        }
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("não é base64")]
    [DataRow("QUJDREVGRw==")]
    public async Task TokenQueNaoEDoSite_EInvalido(string token)
    {
        (WebFactory factory, AppUser ana, _) = await NewFactoryAsync();
        using (factory)
        {
            using IServiceScope scope = factory.Services.CreateScope();

            Assert.AreEqual(RecoveryLinkState.Invalid, scope.ServiceProvider.GetRequiredService<RecoveryTokenProvider>().Inspect(Purpose, token, ana));
        }
    }

    [TestMethod]
    public async Task OSiteUsaOTokenProprioParaRedefinirSenha()
    {
        (WebFactory factory, _, _) = await NewFactoryAsync();
        using (factory)
        {
            using IServiceScope scope = factory.Services.CreateScope();
            IdentityOptions options = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityOptions>>().Value;

            Assert.AreEqual(RecoveryTokenProvider.Name, options.Tokens.PasswordResetTokenProvider);
            Assert.IsTrue(options.Tokens.ProviderMap.ContainsKey(RecoveryTokenProvider.Name));
        }
    }
}
