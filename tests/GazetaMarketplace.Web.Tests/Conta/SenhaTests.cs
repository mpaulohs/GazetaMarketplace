using System;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Tests.Suporte;
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
    public async Task Politica_RejeitaSenhasFracas(string senha, string erroEsperado)
    {
        using FabricaWeb fabrica = new(comBanco: true);
        using IServiceScope escopo = fabrica.Services.CreateScope();
        UserManager<UsuarioIdentity> usuarios = escopo.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();

        IdentityResult resultado = await usuarios.CreateAsync(
            new UsuarioIdentity { UserName = "fraca@exemplo.com.br", Email = "fraca@exemplo.com.br", FullName = "Fraca" }, senha);

        Assert.IsFalse(resultado.Succeeded, senha);
        CollectionAssert.Contains(resultado.Errors.Select(e => e.Code).ToArray(), erroEsperado);
    }

    [TestMethod]
    public async Task SenhaForte_E_Aceita_ESoFicaOHash()
    {
        using FabricaWeb fabrica = new(comBanco: true);
        using IServiceScope escopo = fabrica.Services.CreateScope();
        UserManager<UsuarioIdentity> usuarios = escopo.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        UsuarioIdentity usuario = new() { UserName = "forte@exemplo.com.br", Email = "forte@exemplo.com.br", FullName = "Forte" };

        IdentityResult resultado = await usuarios.CreateAsync(usuario, "Senha@Forte1");

        Assert.IsTrue(resultado.Succeeded, string.Join(";", resultado.Errors.Select(e => e.Code)));
        Assert.IsFalse(usuario.PasswordHash.Contains("Senha@Forte1", StringComparison.Ordinal), "nunca em texto");
        StringAssert.StartsWith(usuario.PasswordHash, "AQAAAA", "hash do Identity (PBKDF2 com sal), irreversível");
    }

    [TestMethod]
    public async Task EmailRepetido_NaoCriaSegundaConta()
    {
        using FabricaWeb fabrica = new(comBanco: true);
        await fabrica.CriarUsuarioAsync("ana@exemplo.com.br", "Ana", "Senha@Forte1", "Redator");
        using IServiceScope escopo = fabrica.Services.CreateScope();
        UserManager<UsuarioIdentity> usuarios = escopo.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();

        IdentityResult resultado = await usuarios.CreateAsync(
            new UsuarioIdentity { UserName = "ANA@exemplo.com.br", Email = "ANA@exemplo.com.br", FullName = "Outra" }, "Senha@Forte1");

        Assert.IsFalse(resultado.Succeeded);
    }
}
