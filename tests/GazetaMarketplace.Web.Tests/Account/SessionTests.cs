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

namespace GazetaMarketplace.Web.Tests.Account;

/// <summary>NFR-08: cookie da sessão da equipe e expiração por inatividade.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SessionTests
#pragma warning restore CA1515
{
    private const string Email = "ana@exemplo.com.br";
    private const string Password = "Senha@Forte1";

    private static async Task<WebFactory> NewFactoryAsync(Dictionary<string, string> configuration = null)
    {
        WebFactory factory = new(configuration: configuration, withDatabase: true);
        await factory.CreateUserAsync(Email, "Ana Souza", Password, RoleNames.Writer);
        return factory;
    }

    private static async Task<bool> IsSignedInAsync(HttpClient client) =>
        (await client.GetAsync("/painel/anuncios")).StatusCode == HttpStatusCode.OK;

    [TestMethod]
    public async Task Cookie_Tem_HttpOnly_Secure_SameSite_E_30Min()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        HttpResponseMessage entry = await client.SignInAsync(Email, Password);
        string cookie = entry.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("Gazeta.Team=", StringComparison.Ordinal));

        StringAssert.Contains(cookie, "httponly");
        StringAssert.Contains(cookie, "secure");
        StringAssert.Contains(cookie, "samesite=lax");
        StringAssert.Contains(cookie, "path=/");
        Assert.IsFalse(cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase), "cookie de sessão: não sobrevive ao fechar o navegador");

        // 30 minutos deslizantes: cada uso renova; 31 minutos parado expira
        factory.Clock.Now += TimeSpan.FromMinutes(29);
        Assert.IsTrue(await IsSignedInAsync(client), "aos 29 min ainda vale (e renova)");
        factory.Clock.Now += TimeSpan.FromMinutes(29);
        Assert.IsTrue(await IsSignedInAsync(client), "29 min depois do último uso ainda vale");
        factory.Clock.Now += TimeSpan.FromMinutes(31);
        Assert.IsFalse(await IsSignedInAsync(client), "31 min parada expira");
    }

    [TestMethod]
    public async Task SessaoMinutos_ConfiguraAExpiracao()
    {
        using WebFactory factory = await NewFactoryAsync(new Dictionary<string, string> { ["Authentication:SessionMinutes"] = "2" });
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Email, Password);

        factory.Clock.Now += TimeSpan.FromMinutes(1);
        Assert.IsTrue(await IsSignedInAsync(client));
        factory.Clock.Now += TimeSpan.FromMinutes(3);
        Assert.IsFalse(await IsSignedInAsync(client), "com 2 minutos configurados, 3 parada expira");
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("121")]
    public async Task SessaoMinutosForaDoIntervalo_ImpedeAPartida(string value)
    {
        using WebFactory factory = new(configuration: new Dictionary<string, string> { ["Authentication:SessionMinutes"] = value }, withDatabase: true);

        OptionsValidationException error = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        StringAssert.Contains(error.Message, "SessionMinutes");
    }

    [TestMethod]
    public async Task UsuarioDesativado_PerdeAcessoAposRevalidacao()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Email, Password);
        Assert.IsTrue(await IsSignedInAsync(client));

        // Desativa sem mexer no carimbo de segurança: quem desativa pode esquecer de trocá-lo
        await factory.UpdateUserAsync(Email, u => u.IsActive = false);

        factory.Clock.Now += TimeSpan.FromMinutes(4);
        Assert.IsTrue(await IsSignedInAsync(client), "antes da revalidação de 5 min, a sessão ainda vale");
        factory.Clock.Now += TimeSpan.FromMinutes(2);
        Assert.IsFalse(await IsSignedInAsync(client), "passada a revalidação, a conta desativada perde o acesso");
    }

    [TestMethod]
    public async Task SenhaTrocada_EncerraAsOutrasSessoes_NaRevalidacao()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Email, Password);

        await factory.UpdateUserAsync(Email, u => u.SecurityStamp = Guid.NewGuid().ToString());
        factory.Clock.Now += TimeSpan.FromMinutes(6);

        Assert.IsFalse(await IsSignedInAsync(client));
    }
}
