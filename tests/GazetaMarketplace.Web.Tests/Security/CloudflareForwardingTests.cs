using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cloudflare.ForwardedHeaders;
using GazetaMarketplace.Web.Security;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Security;

/// <summary>
/// Site atrás do Cloudflare (SEC-01): o IP do visitante vem do cabeçalho <c>CF-Connecting-IP</c>, mas só quando o pedido chega de um
/// endereço do Cloudflare. O host de teste não tem rede: a lista de faixas é a que o teste devolve no lugar do cloudflare.com.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CloudflareForwardingTests
#pragma warning restore CA1515
{
    private const string CloudflareEdge = "173.245.48.5"; // dentro de 173.245.48.0/20 (lista da fábrica e a embutida no pacote)
    private const string Visitor = "203.0.113.9";

    private static Dictionary<string, string> CloudflareOn() => new() { ["ForwardedHeaders:Cloudflare"] = "true" };

    /// <summary>Troca o download do cloudflare.com por uma resposta fixa (ou por uma falha, que faz o pacote usar a lista embutida).</summary>
    private static Action<IServiceCollection> FakeCloudflare(string ipv4 = "173.245.48.0/20\n", string ipv6 = "2400:cb00::/32\n", bool fail = false) =>
        services => services.AddHttpClient(CloudflareForwardedHeadersDefaults.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new StubHandler(ipv4, ipv6, fail));

    private static async Task<string> IpSeenByApplication(HttpClient client, string remoteIp, string cfConnectingIp = null, string forwardedFor = null)
    {
        HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/teste/ip");
        request.Headers.Add(WebFactory.RemoteIpHeader, remoteIp);
        if (cfConnectingIp is not null)
        {
            request.Headers.Add("CF-Connecting-IP", cfConnectingIp);
        }

        if (forwardedFor is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }

        HttpResponseMessage response = await client.SendAsync(request);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("ip").GetString();
    }

    [TestMethod]
    public async Task PedidoDoCloudflare_IpDoVisitanteVemDoCfConnectingIp()
    {
        using WebFactory factory = new(configuration: CloudflareOn(), services: FakeCloudflare());
        using HttpClient client = factory.CreateClient();

        Assert.AreEqual(Visitor, await IpSeenByApplication(client, CloudflareEdge, cfConnectingIp: Visitor));
    }

    [TestMethod]
    public async Task PedidoDoCloudflareEmIpv6_IpDoVisitanteVemDoCfConnectingIp()
    {
        using WebFactory factory = new(configuration: CloudflareOn(), services: FakeCloudflare());
        using HttpClient client = factory.CreateClient();

        Assert.AreEqual("2001:db8::7", await IpSeenByApplication(client, "2400:cb00::1", cfConnectingIp: "2001:db8::7"));
    }

    [TestMethod]
    public async Task PedidoQueNaoVemDoCloudflare_CfConnectingIpForjadoE_Ignorado()
    {
        using WebFactory factory = new(configuration: CloudflareOn(), services: FakeCloudflare());
        using HttpClient client = factory.CreateClient();

        Assert.AreEqual("198.51.100.7", await IpSeenByApplication(client, "198.51.100.7", cfConnectingIp: Visitor));
    }

    [TestMethod]
    public async Task PedidoDoCloudflareSemCfConnectingIp_XForwardedForNaoMudaOIp()
    {
        using WebFactory factory = new(configuration: CloudflareOn(), services: FakeCloudflare());
        using HttpClient client = factory.CreateClient();

        Assert.AreEqual(CloudflareEdge, await IpSeenByApplication(client, CloudflareEdge, forwardedFor: Visitor));
    }

    [TestMethod]
    public async Task SemModoCloudflare_CfConnectingIpDeEnderecoDoCloudflareE_Ignorado()
    {
        using WebFactory factory = new(services: FakeCloudflare());
        using HttpClient client = factory.CreateClient();

        Assert.AreEqual(CloudflareEdge, await IpSeenByApplication(client, CloudflareEdge, cfConnectingIp: Visitor));
    }

    [TestMethod]
    public async Task SemRede_UsaAListaEmbutidaNoPacote()
    {
        using WebFactory factory = new(configuration: CloudflareOn(), services: FakeCloudflare(fail: true));
        using HttpClient client = factory.CreateClient();

        Assert.AreEqual(Visitor, await IpSeenByApplication(client, CloudflareEdge, cfConnectingIp: Visitor));
        Assert.AreEqual("198.51.100.7", await IpSeenByApplication(client, "198.51.100.7", cfConnectingIp: Visitor));
    }

    [TestMethod]
    public async Task ListaDoCloudflare_SoConfiaNasFaixasBaixadas()
    {
        using WebFactory factory = new(configuration: CloudflareOn(), services: FakeCloudflare(ipv4: "198.41.128.0/17\n", ipv6: "2606:4700::/32\n"));
        using HttpClient client = factory.CreateClient();

        Assert.AreEqual(CloudflareEdge, await IpSeenByApplication(client, CloudflareEdge, cfConnectingIp: Visitor));
        Assert.AreEqual(Visitor, await IpSeenByApplication(client, "198.41.129.1", cfConnectingIp: Visitor));
    }

    [TestMethod]
    public void ModoCloudflare_ContaComoIpConhecido_LimiteDeEntradaVolta_Para5()
    {
        IConfiguration comCloudflare = new ConfigurationBuilder().AddInMemoryCollection(CloudflareOn()).Build();
        IConfiguration semNada = new ConfigurationBuilder().Build();

        Assert.AreEqual(AuthLimits.Default, AuthLimits.ForOrigin(comCloudflare));
        Assert.AreEqual(AuthLimits.WhileProxyUnknown, AuthLimits.ForOrigin(semNada));
    }

    [TestMethod]
    public void Production_ComModoCloudflare_NaoAvisaDeProxyDesconhecido_ERegistraAsFaixas()
    {
        Dictionary<string, string> configuration = WebFactory.ProductionConfiguration();
        configuration["ForwardedHeaders:Cloudflare"] = "true";
        using WebFactory production = new("Production", configuration, FakeCloudflare());
        using HttpClient client = production.CreateClient();

        Assert.IsFalse(production.Logs.Events.Any(e => CollectorSink.Text(e).Contains("KnownProxies", StringComparison.Ordinal)));
        LogEvent[] started = production.Logs.Events.Where(e => CollectorSink.Text(e).Contains("Cloudflare", StringComparison.Ordinal)).ToArray();
        Assert.HasCount(1, started);
        Assert.AreEqual(LogEventLevel.Information, started[0].Level);
        StringAssert.Contains(CollectorSink.Text(started[0]), "2 faixas");
    }

    private sealed class StubHandler(string ipv4, string ipv6, bool fail) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (fail)
            {
                throw new HttpRequestException("sem rede (teste)");
            }

            string body = request.RequestUri.AbsolutePath.EndsWith("ips-v6", StringComparison.Ordinal) ? ipv6 : ipv4;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }
}
