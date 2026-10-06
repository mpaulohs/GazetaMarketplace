using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class RateLimiterTests
#pragma warning restore CA1515
{
    private static Task<HttpResponseMessage> Send(HttpClient client, string path, string remoteIp = null, string forwarded = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, path);
        if (remoteIp is not null)
        {
            request.Headers.Add(WebFactory.RemoteIpHeader, remoteIp);
        }

        if (forwarded is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwarded);
        }

        return client.SendAsync(request);
    }

    private static async Task<string> IpSeenByApplication(HttpClient client, string remoteIp, string forwarded)
    {
        HttpResponseMessage response = await Send(client, "/api/v1/teste/ip", remoteIp, forwarded);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("ip").GetString();
    }

    [TestMethod]
    public async Task SextaTentativaDeLogin_Devolve429()
    {
        using WebFactory factory = new(configuration: new System.Collections.Generic.Dictionary<string, string> { ["RateLimiting:AuthPermits"] = "5" });
        using HttpClient client = factory.CreateClient();

        for (int i = 1; i <= 5; i++)
        {
            Assert.AreEqual(HttpStatusCode.OK, (await Send(client, "/api/v1/teste/auth", "198.51.100.1")).StatusCode, "tentativa " + i);
        }

        HttpResponseMessage sixth = await Send(client, "/api/v1/teste/auth", "198.51.100.1");

        Assert.AreEqual(HttpStatusCode.TooManyRequests, sixth.StatusCode);
        Assert.AreEqual("application/problem+json", sixth.Content.Headers.ContentType.MediaType);
        Assert.IsTrue(sixth.Headers.Contains("Retry-After"));
        using JsonDocument json = JsonDocument.Parse(await sixth.Content.ReadAsStringAsync());
        Assert.AreEqual("RATE_LIMITED", json.RootElement.GetProperty("code").GetString());
        Assert.AreEqual(429, json.RootElement.GetProperty("status").GetInt32());
        Assert.AreEqual(sixth.Headers.GetValues("X-Correlation-ID").Single(), json.RootElement.GetProperty("traceId").GetString());

        // o limite é por IP: outro visitante não é afetado
        Assert.AreEqual(HttpStatusCode.OK, (await Send(client, "/api/v1/teste/auth", "198.51.100.2")).StatusCode);
    }

    [TestMethod]
    public async Task LimiteGlobal_Devolve429AposCemPedidos()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        for (int i = 1; i <= 100; i++)
        {
            Assert.AreEqual(HttpStatusCode.OK, (await Send(client, "/api/v1/teste/log", "198.51.100.1")).StatusCode, "pedido " + i);
        }

        Assert.AreEqual(HttpStatusCode.TooManyRequests, (await Send(client, "/api/v1/teste/log", "198.51.100.1")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Send(client, "/api/v1/teste/log", "198.51.100.2")).StatusCode);
    }

    [TestMethod]
    [DataRow("250", 250)]
    [DataRow("3", 3)]
    [DataRow("0", 100)]
    [DataRow("-5", 100)]
    [DataRow("muitos", 100)]
    public async Task LimiteGlobal_PodeSerConfigurado_ValoresInvalidosVoltamParaCem(string configured, int effective)
    {
        using WebFactory factory = new(configuration: new Dictionary<string, string> { ["RateLimiting:GlobalPerMinute"] = configured });
        using HttpClient client = factory.CreateClient();

        for (int i = 1; i <= effective; i++)
        {
            Assert.AreEqual(HttpStatusCode.OK, (await Send(client, "/api/v1/teste/log", "198.51.100.7")).StatusCode, "pedido " + i);
        }

        Assert.AreEqual(HttpStatusCode.TooManyRequests, (await Send(client, "/api/v1/teste/log", "198.51.100.7")).StatusCode);
    }

    [TestMethod]
    [DataRow("4", 4)]
    [DataRow("0", 300)]
    [DataRow("-1", 300)]
    [DataRow("muitas", 300)]
    public async Task LimiteDeFotos_PodeSerConfigurado_ValoresInvalidosVoltamPara300(string configured, int effective)
    {
        using WebFactory factory = new(configuration: new Dictionary<string, string> { ["RateLimiting:PhotosPerMinute"] = configured }, withDatabase: true);
        using HttpClient client = factory.CreateClient();

        // a foto não existe: 404 conta como pedido atendido; o que importa é quando o limite passa a devolver 429
        for (int i = 1; i <= effective; i++)
        {
            Assert.AreEqual(HttpStatusCode.NotFound, (await Send(client, "/fotos/1/1-480.webp", "198.51.100.9")).StatusCode, "pedido " + i);
        }

        Assert.AreEqual(HttpStatusCode.TooManyRequests, (await Send(client, "/fotos/1/1-480.webp", "198.51.100.9")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Send(client, "/api/v1/teste/log", "198.51.100.9")).StatusCode, "o limite global não foi gasto");
    }

    [TestMethod]
    public async Task LimiteDeLoginERecuperacao_NaoMudaComOLimiteGlobalConfigurado()
    {
        using WebFactory factory = new(configuration: new Dictionary<string, string> { ["RateLimiting:GlobalPerMinute"] = "1000", ["RateLimiting:AuthPermits"] = "5" });
        using HttpClient client = factory.CreateClient();

        for (int i = 1; i <= 5; i++)
        {
            Assert.AreEqual(HttpStatusCode.OK, (await Send(client, "/api/v1/teste/auth", "198.51.100.8")).StatusCode, "tentativa " + i);
        }

        Assert.AreEqual(HttpStatusCode.TooManyRequests, (await Send(client, "/api/v1/teste/auth", "198.51.100.8")).StatusCode);
    }

    [TestMethod]
    public async Task ArquivosEstaticos_NaoConsomemOLimiteGlobal()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        for (int i = 0; i < 150; i++)
        {
            HttpResponseMessage staticFile = await Send(client, "/css/site.css", "198.51.100.1");
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, staticFile.StatusCode);
        }

        Assert.AreEqual(HttpStatusCode.OK, (await Send(client, "/api/v1/teste/log", "198.51.100.1")).StatusCode);
    }

    [TestMethod]
    public async Task IpDoCliente_VemDoCabecalhoEncaminhado_DeProxyConfiavel()
    {
        using WebFactory factory = new(configuration: new Dictionary<string, string> { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.1" });
        using HttpClient client = factory.CreateClient();

        string ip = await IpSeenByApplication(client, remoteIp: "10.0.0.1", forwarded: "203.0.113.9");

        Assert.AreEqual("203.0.113.9", ip);
    }

    [TestMethod]
    public async Task CabecalhoEncaminhado_DeOrigemNaoConfiavel_E_Ignorado()
    {
        using WebFactory factory = new(configuration: new Dictionary<string, string> { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.1" });
        using HttpClient client = factory.CreateClient();

        string ip = await IpSeenByApplication(client, remoteIp: "198.51.100.7", forwarded: "203.0.113.9");

        Assert.AreEqual("198.51.100.7", ip);
    }

    [TestMethod]
    public async Task SemProxiesConfigurados_CabecalhoEncaminhado_E_Ignorado()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        string ip = await IpSeenByApplication(client, remoteIp: "10.0.0.1", forwarded: "203.0.113.9");

        Assert.AreEqual("10.0.0.1", ip);
    }
}
