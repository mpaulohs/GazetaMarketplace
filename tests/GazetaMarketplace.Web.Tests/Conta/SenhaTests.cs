using System;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Conta;

/// <summary>NFR-07: política de senha (8 ou mais, maiúscula, minúscula, número e símbolo) e hash irreversível.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SenhaTests
#pragma warning restore CA1515
{
    [TestMethod]
    [DataRow("Ab1!", "PasswordTooShort")]
    [DataRow("senha@forte1", "PasswordRequiresUpper")]
    [DataRow("SENHA@FORTE1", "PasswordRequiresLower")]
    [DataRow("Senha@Forte", "PasswordRequiresDigit")]
    [DataRow("SenhaForte12", "PasswordRequiresNonAlphanumeric")]
    public async Task Politica_RejeitaSenhasFracas(string password, string erroEsperado)
    {
        using WebFactory factory = new(withDatabase: true);
        using IServiceScope scope = factory.Services.CreateScope();
        UserManager<UsuarioIdentity> users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();

        IdentityResult result = await users.CreateAsync(
            new UsuarioIdentity { UserName = "fraca@exemplo.com.br", Email = "fraca@exemplo.com.br", FullName = "Fraca" }, password);

        Assert.IsFalse(result.Succeeded, password);
        CollectionAssert.Contains(result.Errors.Select(e => e.Code).ToArray(), erroEsperado);
    }

    [TestMethod]
    public async Task SenhaForte_E_Aceita_ESoFicaOHash()
    {
        using WebFactory factory = new(withDatabase: true);
        using IServiceScope scope = factory.Services.CreateScope();
        UserManager<UsuarioIdentity> users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        UsuarioIdentity user = new() { UserName = "forte@exemplo.com.br", Email = "forte@exemplo.com.br", FullName = "Forte" };

        IdentityResult result = await users.CreateAsync(user, "Senha@Forte1");

        Assert.IsTrue(result.Succeeded, string.Join(";", result.Errors.Select(e => e.Code)));
        Assert.IsFalse(user.PasswordHash.Contains("Senha@Forte1", StringComparison.Ordinal), "nunca em texto");
        StringAssert.StartsWith(user.PasswordHash, "AQAAAA", "hash do Identity (PBKDF2 com sal), irreversível");
    }

    [TestMethod]
    public async Task EmailRepetido_NaoCriaSegundaConta()
    {
        using WebFactory factory = new(withDatabase: true);
        await factory.CreateUserAsync("ana@exemplo.com.br", "Ana", "Senha@Forte1", "Redator");
        using IServiceScope scope = factory.Services.CreateScope();
        UserManager<UsuarioIdentity> users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();

        IdentityResult result = await users.CreateAsync(
            new UsuarioIdentity { UserName = "ANA@exemplo.com.br", Email = "ANA@exemplo.com.br", FullName = "Outra" }, "Senha@Forte1");

        Assert.IsFalse(result.Succeeded);
    }
}
