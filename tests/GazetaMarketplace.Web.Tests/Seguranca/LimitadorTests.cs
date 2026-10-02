using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Seguranca;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class LimitadorTests
#pragma warning restore CA1515
{
    private static Task<HttpResponseMessage> Pedir(HttpClient cliente, string caminho, string ipRemoto = null, string encaminhado = null)
    {
        HttpRequestMessage pedido = new(HttpMethod.Get, caminho);
        if (ipRemoto is not null)
        {
            pedido.Headers.Add(FabricaWeb.CabecalhoIpRemoto, ipRemoto);
        }

        if (encaminhado is not null)
        {
            pedido.Headers.Add("X-Forwarded-For", encaminhado);
        }

        return cliente.SendAsync(pedido);
    }

    private static async Task<string> IpVistoPelaAplicacao(HttpClient cliente, string ipRemoto, string encaminhado)
    {
        HttpResponseMessage resposta = await Pedir(cliente, "/api/v1/teste/ip", ipRemoto, encaminhado);
        using JsonDocument json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("ip").GetString();
    }

    [TestMethod]
    public async Task SextaTentativaDeLogin_Devolve429()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        for (int i = 1; i <= 5; i++)
        {
            Assert.AreEqual(HttpStatusCode.OK, (await Pedir(cliente, "/api/v1/teste/auth", "198.51.100.1")).StatusCode, "tentativa " + i);
        }

        HttpResponseMessage sexta = await Pedir(cliente, "/api/v1/teste/auth", "198.51.100.1");

        Assert.AreEqual(HttpStatusCode.TooManyRequests, sexta.StatusCode);
        Assert.AreEqual("application/problem+json", sexta.Content.Headers.ContentType.MediaType);
        Assert.IsTrue(sexta.Headers.Contains("Retry-After"));
        using JsonDocument json = JsonDocument.Parse(await sexta.Content.ReadAsStringAsync());
        Assert.AreEqual("RATE_LIMITED", json.RootElement.GetProperty("code").GetString());
        Assert.AreEqual(429, json.RootElement.GetProperty("status").GetInt32());
        Assert.AreEqual(sexta.Headers.GetValues("X-Correlation-ID").Single(), json.RootElement.GetProperty("traceId").GetString());

        // o limite é por IP: outro visitante não é afetado
        Assert.AreEqual(HttpStatusCode.OK, (await Pedir(cliente, "/api/v1/teste/auth", "198.51.100.2")).StatusCode);
    }

    [TestMethod]
    public async Task LimiteGlobal_Devolve429AposCemPedidos()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        for (int i = 1; i <= 100; i++)
        {
            Assert.AreEqual(HttpStatusCode.OK, (await Pedir(cliente, "/api/v1/teste/log", "198.51.100.1")).StatusCode, "pedido " + i);
        }

        Assert.AreEqual(HttpStatusCode.TooManyRequests, (await Pedir(cliente, "/api/v1/teste/log", "198.51.100.1")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Pedir(cliente, "/api/v1/teste/log", "198.51.100.2")).StatusCode);
    }

    [TestMethod]
    public async Task ArquivosEstaticos_NaoConsomemOLimiteGlobal()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        for (int i = 0; i < 150; i++)
        {
            HttpResponseMessage estatico = await Pedir(cliente, "/css/site.css", "198.51.100.1");
            Assert.AreNotEqual(HttpStatusCode.TooManyRequests, estatico.StatusCode);
        }

        Assert.AreEqual(HttpStatusCode.OK, (await Pedir(cliente, "/api/v1/teste/log", "198.51.100.1")).StatusCode);
    }

    [TestMethod]
    public async Task IpDoCliente_VemDoCabecalhoEncaminhado_DeProxyConfiavel()
    {
        using FabricaWeb fabrica = new(configuracao: new Dictionary<string, string> { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.1" });
        using HttpClient cliente = fabrica.CreateClient();

        string ip = await IpVistoPelaAplicacao(cliente, ipRemoto: "10.0.0.1", encaminhado: "203.0.113.9");

        Assert.AreEqual("203.0.113.9", ip);
    }

    [TestMethod]
    public async Task CabecalhoEncaminhado_DeOrigemNaoConfiavel_E_Ignorado()
    {
        using FabricaWeb fabrica = new(configuracao: new Dictionary<string, string> { ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.1" });
        using HttpClient cliente = fabrica.CreateClient();

        string ip = await IpVistoPelaAplicacao(cliente, ipRemoto: "198.51.100.7", encaminhado: "203.0.113.9");

        Assert.AreEqual("198.51.100.7", ip);
    }

    [TestMethod]
    public async Task SemProxiesConfigurados_CabecalhoEncaminhado_E_Ignorado()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        string ip = await IpVistoPelaAplicacao(cliente, ipRemoto: "10.0.0.1", encaminhado: "203.0.113.9");

        Assert.AreEqual("10.0.0.1", ip);
    }
}
