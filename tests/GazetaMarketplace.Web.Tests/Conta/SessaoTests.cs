using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Tests.Support;
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

    private static async Task<WebFactory> NovaFabricaAsync(Dictionary<string, string> configuration = null)
    {
        WebFactory factory = new(configuration: configuration, withDatabase: true);
        await factory.CreateUserAsync(Email, "Ana Souza", Senha, RoleNames.Writer);
        return factory;
    }

    private static async Task<bool> EstaLogadaAsync(HttpClient client) =>
        (await client.GetAsync("/painel/anuncios")).StatusCode == HttpStatusCode.OK;

    [TestMethod]
    public async Task Cookie_Tem_HttpOnly_Secure_SameSite_E_30Min()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        HttpResponseMessage entry = await client.EntrarAsync(Email, Senha);
        string cookie = entry.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("Gazeta.Equipe=", StringComparison.Ordinal));

        StringAssert.Contains(cookie, "httponly");
        StringAssert.Contains(cookie, "secure");
        StringAssert.Contains(cookie, "samesite=lax");
        StringAssert.Contains(cookie, "path=/");
        Assert.IsFalse(cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase), "cookie de sessão: não sobrevive ao fechar o navegador");

        // 30 minutos deslizantes: cada uso renova; 31 minutos parado expira
        factory.Clock.Now += TimeSpan.FromMinutes(29);
        Assert.IsTrue(await EstaLogadaAsync(client), "aos 29 min ainda vale (e renova)");
        factory.Clock.Now += TimeSpan.FromMinutes(29);
        Assert.IsTrue(await EstaLogadaAsync(client), "29 min depois do último uso ainda vale");
        factory.Clock.Now += TimeSpan.FromMinutes(31);
        Assert.IsFalse(await EstaLogadaAsync(client), "31 min parada expira");
    }

    [TestMethod]
    public async Task SessaoMinutos_ConfiguraAExpiracao()
    {
        using WebFactory factory = await NovaFabricaAsync(new Dictionary<string, string> { ["Autenticacao:SessaoMinutos"] = "2" });
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Email, Senha);

        factory.Clock.Now += TimeSpan.FromMinutes(1);
        Assert.IsTrue(await EstaLogadaAsync(client));
        factory.Clock.Now += TimeSpan.FromMinutes(3);
        Assert.IsFalse(await EstaLogadaAsync(client), "com 2 minutos configurados, 3 parada expira");
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("121")]
    public async Task SessaoMinutosForaDoIntervalo_ImpedeAPartida(string value)
    {
        using WebFactory factory = new(configuration: new Dictionary<string, string> { ["Autenticacao:SessaoMinutos"] = value }, withDatabase: true);

        OptionsValidationException error = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        StringAssert.Contains(error.Message, "SessaoMinutos");
    }

    [TestMethod]
    public async Task UsuarioDesativado_PerdeAcessoAposRevalidacao()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Email, Senha);
        Assert.IsTrue(await EstaLogadaAsync(client));

        // Desativa sem mexer no carimbo de segurança: quem desativa pode esquecer de trocá-lo
        await factory.UpdateUserAsync(Email, u => u.IsActive = false);

        factory.Clock.Now += TimeSpan.FromMinutes(4);
        Assert.IsTrue(await EstaLogadaAsync(client), "antes da revalidação de 5 min, a sessão ainda vale");
        factory.Clock.Now += TimeSpan.FromMinutes(2);
        Assert.IsFalse(await EstaLogadaAsync(client), "passada a revalidação, a conta desativada perde o acesso");
    }

    [TestMethod]
    public async Task SenhaTrocada_EncerraAsOutrasSessoes_NaRevalidacao()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Email, Senha);

        await factory.UpdateUserAsync(Email, u => u.SecurityStamp = Guid.NewGuid().ToString());
        factory.Clock.Now += TimeSpan.FromMinutes(6);

        Assert.IsFalse(await EstaLogadaAsync(client));
    }
}
