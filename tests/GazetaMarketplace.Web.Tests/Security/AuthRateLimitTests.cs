using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

/// <summary>
/// RC-10, NFR-06 e SC-01/SC-02: entrar conta só <b>falhas</b> por IP; "esqueci minha senha" e "redefinir senha" têm um balde de 15 minutos cada.
/// Com proxies conhecidos o limite é 5 (rules/security.md); enquanto a lista não vem do provedor (SEC-01) é 20, para uma pessoa errando a senha não travar a redação.
/// </summary>
[TestClass]
public sealed class AuthRateLimitTests
{
    private const string Email = "ana@exemplo.com.br";
    private const string Wrong = "Senha@Errada9";
    private const string Right = "Senha@Forte1";

    // "0" = valor ausente: vale o padrão de produção; o host de teste usa limites altos (1000) e 5 para as falhas de entrada
    private static readonly Dictionary<string, string> ProductionDefault = new() { ["RateLimiting:AuthPermits"] = "0", ["RateLimiting:LoginFailuresPerOrigin"] = "0" };

    private static readonly Dictionary<string, string> WithKnownProxy = new()
    {
        ["RateLimiting:AuthPermits"] = "0",
        ["RateLimiting:LoginFailuresPerOrigin"] = "0",
        ["ForwardedHeaders:KnownProxies:0"] = "203.0.113.250"
    };

    private static async Task<WebFactory> NewFactoryAsync(Dictionary<string, string> configuration = null)
    {
        WebFactory factory = new(configuration: configuration ?? ProductionDefault, withDatabase: true);
        await factory.CreateUserAsync(Email, "Ana Souza", Right, RoleNames.Writer);
        return factory;
    }

    private static Task<HttpResponseMessage> ForgotAsync(HttpClient client, string ip)
        => client.PostFormWithIpAsync("/painel/esqueci-minha-senha", "/painel/esqueci-minha-senha", new Dictionary<string, string> { ["Email"] = Email }, ip);

    private static Task<HttpResponseMessage> ResetAsync(HttpClient client, string ip)
        => client.PostFormWithIpAsync("/painel/esqueci-minha-senha", "/painel/redefinir-senha", new Dictionary<string, string> { ["UserId"] = "1", ["Token"] = "x", ["NewPassword"] = "Senha@Forte2", ["ConfirmPassword"] = "Senha@Forte2" }, ip);

    private static async Task Exhaust(Func<string, Task<HttpResponseMessage>> action, string ip, int permits)
    {
        for (int i = 1; i <= permits; i++)
        {
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, (await action(ip)).StatusCode, "pedido " + i);
        }

        Assert.AreEqual(HttpStatusCode.TooManyRequests, (await action(ip)).StatusCode, "o pedido seguinte é recusado");
    }

    [TestMethod]
    public async Task SemProxiesConhecidos_OLimiteTemporarioE20_ParaAsTresAcoes() // SC-01
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        await Exhaust(ip => ForgotAsync(client, ip), "198.51.100.3", 20);
        await Exhaust(ip => ResetAsync(client, ip), "198.51.100.4", 20);

        for (int i = 1; i <= 20; i++)
        {
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, (await client.SignInAsync(Email, Wrong, ip: "198.51.100.5")).StatusCode, "falha " + i);
        }

        HttpResponseMessage blocked = await client.SignInAsync(Email, Wrong, ip: "198.51.100.5");
        Assert.AreEqual(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.IsTrue(blocked.Headers.Contains("Retry-After"));
    }

    [TestMethod]
    public async Task ComProxiesConhecidos_OLimiteE5_ParaAsTresAcoes_EPorIp() // SC-01
    {
        using WebFactory factory = await NewFactoryAsync(WithKnownProxy);
        using HttpClient client = TeamClient.Create(factory);

        await Exhaust(ip => ForgotAsync(client, ip), "198.51.100.3", 5);
        await Exhaust(ip => ResetAsync(client, ip), "198.51.100.4", 5);

        for (int i = 1; i <= 5; i++)
        {
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, (await client.SignInAsync(Email, Wrong, ip: "198.51.100.5")).StatusCode, "falha " + i);
        }

        Assert.AreEqual(HttpStatusCode.TooManyRequests, (await client.SignInAsync(Email, Wrong, ip: "198.51.100.5")).StatusCode);
        Assert.AreNotEqual(HttpStatusCode.TooManyRequests, (await client.SignInAsync(Email, Wrong, ip: "198.51.100.6")).StatusCode, "o limite é por IP");
    }

    [TestMethod]
    public async Task EntradasComSucesso_NuncaContam_AMesmaRedacaoEntraDeManha() // SC-02 (a 6.ª pessoa do mesmo IP recebia 429 com a senha certa)
    {
        using WebFactory factory = await NewFactoryAsync(WithKnownProxy);

        for (int i = 1; i <= 12; i++)
        {
            using HttpClient person = TeamClient.Create(factory);
            HttpResponseMessage entry = await person.SignInAsync(Email, Right, ip: "198.51.100.7");
            Assert.AreEqual(HttpStatusCode.Redirect, entry.StatusCode, "entrada " + i);
            Assert.AreNotEqual("/painel/entrar", entry.Destination());
        }
    }

    [TestMethod]
    public async Task OsTresBaldes_SaoSeparados_QuemErrouASenhaAindaPedeARedefinicao() // SC-02
    {
        using WebFactory factory = await NewFactoryAsync(WithKnownProxy);
        using HttpClient client = TeamClient.Create(factory);

        for (int i = 1; i <= 6; i++)
        {
            await client.SignInAsync(Email, Wrong, ip: "198.51.100.8");
        }

        Assert.AreEqual(HttpStatusCode.TooManyRequests, (await client.SignInAsync(Email, Right, ip: "198.51.100.8")).StatusCode, "entrar está bloqueado para esse IP");
        Assert.AreEqual(HttpStatusCode.OK, (await ForgotAsync(client, "198.51.100.8")).StatusCode, "mas o pedido de redefinição segue livre");

        await Exhaust(ip => ForgotAsync(client, ip), "198.51.100.9", 5);
        Assert.AreNotEqual(HttpStatusCode.TooManyRequests, (await ResetAsync(client, "198.51.100.9")).StatusCode, "esgotar o 'esqueci' não gasta o balde do 'redefinir'");
        Assert.AreNotEqual(HttpStatusCode.TooManyRequests, (await client.SignInAsync(Email, Wrong, ip: "198.51.100.9")).StatusCode, "nem o de entrar");
    }

    [TestMethod]
    public async Task AbrirAsTelas_NuncaConta()
    {
        using WebFactory factory = await NewFactoryAsync(WithKnownProxy);
        using HttpClient client = TeamClient.Create(factory);

        for (int i = 1; i <= 10; i++)
        {
            Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/painel/entrar")).StatusCode, "abrir a tela não conta");
            Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/painel/esqueci-minha-senha")).StatusCode, "abrir a tela não conta");
        }
    }

    [TestMethod]
    public async Task TheLimit_CanBeRaisedByConfiguration_ForTheE2ESite()
    {
        using WebFactory factory = await NewFactoryAsync(new Dictionary<string, string> { ["RateLimiting:AuthPermits"] = "50", ["RateLimiting:LoginFailuresPerOrigin"] = "50" });
        using HttpClient client = TeamClient.Create(factory);

        for (int i = 1; i <= 12; i++)
        {
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, (await ForgotAsync(client, "198.51.100.6")).StatusCode, "pedido " + i);
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, (await client.SignInAsync(Email, Wrong, ip: "198.51.100.6")).StatusCode, "falha " + i);
        }
    }
}
