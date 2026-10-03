using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Equipe;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Conta;

/// <summary>NFR-08: cookie da sessão da equipe e expiração por inatividade.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SessaoTests
#pragma warning restore CA1515
{
    private const string Email = "ana@exemplo.com.br";
    private const string Senha = "Senha@Forte1";

    private static async Task<FabricaWeb> NovaFabricaAsync(Dictionary<string, string> configuracao = null)
    {
        FabricaWeb fabrica = new(configuracao: configuracao, comBanco: true);
        await fabrica.CriarUsuarioAsync(Email, "Ana Souza", Senha, Papeis.Redator);
        return fabrica;
    }

    private static async Task<bool> EstaLogadaAsync(HttpClient cliente) =>
        (await cliente.GetAsync("/painel/anuncios")).StatusCode == HttpStatusCode.OK;

    [TestMethod]
    public async Task Cookie_Tem_HttpOnly_Secure_SameSite_E_30Min()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);

        HttpResponseMessage entrada = await cliente.EntrarAsync(Email, Senha);
        string cookie = entrada.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("Gazeta.Equipe=", StringComparison.Ordinal));

        StringAssert.Contains(cookie, "httponly");
        StringAssert.Contains(cookie, "secure");
        StringAssert.Contains(cookie, "samesite=lax");
        StringAssert.Contains(cookie, "path=/");
        Assert.IsFalse(cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase), "cookie de sessão: não sobrevive ao fechar o navegador");

        // 30 minutos deslizantes: cada uso renova; 31 minutos parado expira
        fabrica.Relogio.Agora += TimeSpan.FromMinutes(29);
        Assert.IsTrue(await EstaLogadaAsync(cliente), "aos 29 min ainda vale (e renova)");
        fabrica.Relogio.Agora += TimeSpan.FromMinutes(29);
        Assert.IsTrue(await EstaLogadaAsync(cliente), "29 min depois do último uso ainda vale");
        fabrica.Relogio.Agora += TimeSpan.FromMinutes(31);
        Assert.IsFalse(await EstaLogadaAsync(cliente), "31 min parada expira");
    }

    [TestMethod]
    public async Task SessaoMinutos_ConfiguraAExpiracao()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync(new Dictionary<string, string> { ["Autenticacao:SessaoMinutos"] = "2" });
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);
        await cliente.EntrarAsync(Email, Senha);

        fabrica.Relogio.Agora += TimeSpan.FromMinutes(1);
        Assert.IsTrue(await EstaLogadaAsync(cliente));
        fabrica.Relogio.Agora += TimeSpan.FromMinutes(3);
        Assert.IsFalse(await EstaLogadaAsync(cliente), "com 2 minutos configurados, 3 parada expira");
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("121")]
    public async Task SessaoMinutosForaDoIntervalo_ImpedeAPartida(string valor)
    {
        using FabricaWeb fabrica = new(configuracao: new Dictionary<string, string> { ["Autenticacao:SessaoMinutos"] = valor }, comBanco: true);

        OptionsValidationException erro = Assert.Throws<OptionsValidationException>(() => fabrica.CreateClient());
        StringAssert.Contains(erro.Message, "SessaoMinutos");
    }

    [TestMethod]
    public async Task UsuarioDesativado_PerdeAcessoAposRevalidacao()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);
        await cliente.EntrarAsync(Email, Senha);
        Assert.IsTrue(await EstaLogadaAsync(cliente));

        // Desativa sem mexer no carimbo de segurança: quem desativa pode esquecer de trocá-lo
        await fabrica.AlterarUsuarioAsync(Email, u => u.IsActive = false);

        fabrica.Relogio.Agora += TimeSpan.FromMinutes(4);
        Assert.IsTrue(await EstaLogadaAsync(cliente), "antes da revalidação de 5 min, a sessão ainda vale");
        fabrica.Relogio.Agora += TimeSpan.FromMinutes(2);
        Assert.IsFalse(await EstaLogadaAsync(cliente), "passada a revalidação, a conta desativada perde o acesso");
    }

    [TestMethod]
    public async Task SenhaTrocada_EncerraAsOutrasSessoes_NaRevalidacao()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);
        await cliente.EntrarAsync(Email, Senha);

        await fabrica.AlterarUsuarioAsync(Email, u => u.SecurityStamp = Guid.NewGuid().ToString());
        fabrica.Relogio.Agora += TimeSpan.FromMinutes(6);

        Assert.IsFalse(await EstaLogadaAsync(cliente));
    }
}
