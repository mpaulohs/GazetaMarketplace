using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Web.Security;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Account;

/// <summary>Mensagens do Identity em português (reaproveitadas pela tela de usuários, US-014-S06), tokens de 1 hora e o claim da senha provisória.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ErrorDescriberTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void DescritorDoSite_EODaEquipe_ComCodigosOriginais()
    {
        using WebFactory factory = new(withDatabase: true);
        IdentityErrorDescriber describer = factory.Services.GetRequiredService<IdentityErrorDescriber>();

        Assert.IsInstanceOfType<TeamIdentityErrorDescriber>(describer);
        IdentityError shortError = describer.PasswordTooShort(8);
        Assert.AreEqual("PasswordTooShort", shortError.Code);
        Assert.AreEqual("A senha precisa ter 8 caracteres ou mais.", shortError.Description);
        Assert.AreEqual("PasswordRequiresUpper", describer.PasswordRequiresUpper().Code);
        Assert.AreEqual("Já existe um usuário com este e-mail", describer.DuplicateEmail("a@b.com").Description);
        Assert.AreEqual("Já existe um usuário com este e-mail", describer.DuplicateUserName("a@b.com").Description);
        Assert.AreEqual("Informe um e-mail válido", describer.InvalidEmail("x").Description);
    }

    [TestMethod]
    public void NenhumaMensagemFicaEmIngles()
    {
        IdentityErrorDescriber describer = new TeamIdentityErrorDescriber();
        IdentityError[] errors =
        [
            describer.DefaultError(), describer.PasswordMismatch(), describer.PasswordTooShort(8), describer.PasswordRequiresUpper(),
            describer.PasswordRequiresLower(), describer.PasswordRequiresDigit(), describer.PasswordRequiresNonAlphanumeric(),
            describer.PasswordRequiresUniqueChars(3), describer.InvalidEmail("x"), describer.InvalidUserName("x"),
            describer.DuplicateEmail("x"), describer.DuplicateUserName("x"), describer.InvalidToken()
        ];

        foreach (IdentityError error in errors)
        {
            Assert.IsFalse(error.Description.Contains("Password", StringComparison.Ordinal) || error.Description.Contains("must", StringComparison.OrdinalIgnoreCase), error.Code + ": " + error.Description);
        }
    }

    [TestMethod]
    public void TokenDoIdentity_ValeUmaHora()
    {
        using WebFactory factory = new(withDatabase: true);

        Assert.AreEqual(TimeSpan.FromHours(1), factory.Services.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value.TokenLifespan);
    }

    [TestMethod]
    public async Task TokenDeRedefinicao_PodeSerGerado_ESoValeUmaVez()
    {
        using WebFactory factory = new(withDatabase: true);
        await factory.CreateUserAsync("ana@exemplo.com.br", "Ana", "Senha@Forte1", RoleNames.Writer);
        using IServiceScope scope = factory.Services.CreateScope();
        UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        AppUser ana = await users.FindByEmailAsync("ana@exemplo.com.br");

        string token = await users.GeneratePasswordResetTokenAsync(ana);
        Assert.IsTrue((await users.ResetPasswordAsync(ana, token, "Outra@Senha2")).Succeeded);
        Assert.IsFalse((await users.ResetPasswordAsync(ana, token, "Terceira@Senha3")).Succeeded, "o carimbo mudou: o mesmo token não vale de novo");
    }

    [TestMethod]
    public async Task Cookie_LevaOClaimDaSenhaProvisoria()
    {
        using WebFactory factory = new(withDatabase: true);
        await factory.CreateUserAsync("nova@exemplo.com.br", "Nova", "Provisoria@1", RoleNames.Writer, mustChangePassword: true);
        await factory.CreateUserAsync("velha@exemplo.com.br", "Velha", "Senha@Forte1", RoleNames.Writer);
        using IServiceScope scope = factory.Services.CreateScope();
        UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        IUserClaimsPrincipalFactory<AppUser> claimsFactory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<AppUser>>();

        ClaimsPrincipal newPassword = await claimsFactory.CreateAsync(await users.FindByEmailAsync("nova@exemplo.com.br"));
        ClaimsPrincipal old = await claimsFactory.CreateAsync(await users.FindByEmailAsync("velha@exemplo.com.br"));

        Assert.AreEqual("1", newPassword.FindFirstValue(TeamClaims.MustChangePassword));
        Assert.AreEqual("0", old.FindFirstValue(TeamClaims.MustChangePassword));
        Assert.AreEqual("Nova", newPassword.FindFirstValue(TeamClaims.FullName));
    }
}
