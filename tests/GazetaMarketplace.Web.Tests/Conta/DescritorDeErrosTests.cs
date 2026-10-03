using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Equipe;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Seguranca;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Conta;

/// <summary>Mensagens do Identity em português (reaproveitadas pela tela de usuários, US-014-S06), tokens de 1 hora e o claim da senha provisória.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DescritorDeErrosTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void DescritorDoSite_EODaEquipe_ComCodigosOriginais()
    {
        using FabricaWeb fabrica = new(comBanco: true);
        IdentityErrorDescriber describer = fabrica.Services.GetRequiredService<IdentityErrorDescriber>();

        Assert.IsInstanceOfType<DescritorDeErrosDaEquipe>(describer);
        IdentityError curta = describer.PasswordTooShort(8);
        Assert.AreEqual("PasswordTooShort", curta.Code);
        Assert.AreEqual("A senha precisa ter 8 caracteres ou mais.", curta.Description);
        Assert.AreEqual("PasswordRequiresUpper", describer.PasswordRequiresUpper().Code);
        Assert.AreEqual("Já existe uma conta com este e-mail.", describer.DuplicateEmail("a@b.com").Description);
        Assert.AreEqual("Já existe uma conta com este e-mail.", describer.DuplicateUserName("a@b.com").Description);
        Assert.AreEqual("E-mail em formato inválido.", describer.InvalidEmail("x").Description);
    }

    [TestMethod]
    public void NenhumaMensagemFicaEmIngles()
    {
        IdentityErrorDescriber describer = new DescritorDeErrosDaEquipe();
        IdentityError[] erros =
        [
            describer.DefaultError(), describer.PasswordMismatch(), describer.PasswordTooShort(8), describer.PasswordRequiresUpper(),
            describer.PasswordRequiresLower(), describer.PasswordRequiresDigit(), describer.PasswordRequiresNonAlphanumeric(),
            describer.PasswordRequiresUniqueChars(3), describer.InvalidEmail("x"), describer.InvalidUserName("x"),
            describer.DuplicateEmail("x"), describer.DuplicateUserName("x"), describer.InvalidToken()
        ];

        foreach (IdentityError erro in erros)
        {
            Assert.IsFalse(erro.Description.Contains("Password", StringComparison.Ordinal) || erro.Description.Contains("must", StringComparison.OrdinalIgnoreCase), erro.Code + ": " + erro.Description);
        }
    }

    [TestMethod]
    public void TokenDoIdentity_ValeUmaHora()
    {
        using FabricaWeb fabrica = new(comBanco: true);

        Assert.AreEqual(TimeSpan.FromHours(1), fabrica.Services.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value.TokenLifespan);
    }

    [TestMethod]
    public async Task TokenDeRedefinicao_PodeSerGerado_ESoValeUmaVez()
    {
        using FabricaWeb fabrica = new(comBanco: true);
        await fabrica.CriarUsuarioAsync("ana@exemplo.com.br", "Ana", "Senha@Forte1", Papeis.Redator);
        using IServiceScope escopo = fabrica.Services.CreateScope();
        UserManager<UsuarioIdentity> usuarios = escopo.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        UsuarioIdentity ana = await usuarios.FindByEmailAsync("ana@exemplo.com.br");

        string token = await usuarios.GeneratePasswordResetTokenAsync(ana);
        Assert.IsTrue((await usuarios.ResetPasswordAsync(ana, token, "Outra@Senha2")).Succeeded);
        Assert.IsFalse((await usuarios.ResetPasswordAsync(ana, token, "Terceira@Senha3")).Succeeded, "o carimbo mudou: o mesmo token não vale de novo");
    }

    [TestMethod]
    public async Task Cookie_LevaOClaimDaSenhaProvisoria()
    {
        using FabricaWeb fabrica = new(comBanco: true);
        await fabrica.CriarUsuarioAsync("nova@exemplo.com.br", "Nova", "Provisoria@1", Papeis.Redator, trocarSenha: true);
        await fabrica.CriarUsuarioAsync("velha@exemplo.com.br", "Velha", "Senha@Forte1", Papeis.Redator);
        using IServiceScope escopo = fabrica.Services.CreateScope();
        UserManager<UsuarioIdentity> usuarios = escopo.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        IUserClaimsPrincipalFactory<UsuarioIdentity> fabricaDeClaims = escopo.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<UsuarioIdentity>>();

        ClaimsPrincipal nova = await fabricaDeClaims.CreateAsync(await usuarios.FindByEmailAsync("nova@exemplo.com.br"));
        ClaimsPrincipal velha = await fabricaDeClaims.CreateAsync(await usuarios.FindByEmailAsync("velha@exemplo.com.br"));

        Assert.AreEqual("1", nova.FindFirstValue(ClaimsDaEquipe.DeveTrocarSenha));
        Assert.AreEqual("0", velha.FindFirstValue(ClaimsDaEquipe.DeveTrocarSenha));
        Assert.AreEqual("Nova", nova.FindFirstValue(ClaimsDaEquipe.NomeCompleto));
    }
}
