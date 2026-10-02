using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Seguranca;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CabecalhosTests
#pragma warning restore CA1515
{
    [TestMethod]
    [DataRow("/")]
    [DataRow("/api/v1/teste/log")]
    [DataRow("/api/v1/teste/erro/inesperado")]
    [DataRow("/api/v1/teste/erro/notfound")]
    [DataRow("/nao-existe")]
    public async Task TodaResposta_TemOsCabecalhosObrigatorios(string caminho)
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync(caminho);

        Assert.AreEqual("nosniff", resposta.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.AreEqual("DENY", resposta.Headers.GetValues("X-Frame-Options").Single());
        Assert.AreEqual("strict-origin-when-cross-origin", resposta.Headers.GetValues("Referrer-Policy").Single());
        StringAssert.Contains(resposta.Headers.GetValues("Permissions-Policy").Single(), "camera=()");
        StringAssert.Contains(resposta.Headers.GetValues("Permissions-Policy").Single(), "geolocation=()");
        Assert.IsTrue(resposta.Headers.Contains("Content-Security-Policy"));
    }

    [TestMethod]
    public async Task Hsts_SoEmProducao()
    {
        using FabricaWeb teste = new("Testing");
        using HttpClient clienteTeste = teste.CreateClient();
        HttpResponseMessage emTeste = await clienteTeste.GetAsync("/api/v1/teste/log");
        Assert.IsFalse(emTeste.Headers.Contains("Strict-Transport-Security"));

        using FabricaWeb producao = new("Production", FabricaWeb.ConfiguracaoDeProducao());
        using HttpClient clienteProducao = producao.CreateClient();
        HttpResponseMessage emProducao = await clienteProducao.GetAsync("/api/v1/teste/log");
        string hsts = emProducao.Headers.GetValues("Strict-Transport-Security").Single();
        StringAssert.Contains(hsts, "max-age=31536000");
        StringAssert.Contains(hsts, "includeSubDomains");
    }

    [TestMethod]
    public async Task Http_RedirecionaParaHttps_ComAPortaDaConfiguracao()
    {
        using FabricaWeb fabrica = new(configuracao: new System.Collections.Generic.Dictionary<string, string> { ["HttpsRedirection:HttpsPort"] = "443" });
        using HttpClient cliente = ClienteHttp(fabrica);

        HttpResponseMessage resposta = await cliente.GetAsync("/api/v1/teste/log");

        Assert.AreEqual(System.Net.HttpStatusCode.PermanentRedirect, resposta.StatusCode);
        Assert.AreEqual("https://localhost/api/v1/teste/log", resposta.Headers.Location.ToString());
    }

    [TestMethod]
    public async Task Http_EmProducao_RedirecionaParaHttpsNaPorta443_SemConfigurar()
    {
        using FabricaWeb producao = new("Production", FabricaWeb.ConfiguracaoDeProducao());
        using HttpClient cliente = ClienteHttp(producao);

        HttpResponseMessage resposta = await cliente.GetAsync("/api/v1/teste/log");

        Assert.AreEqual(System.Net.HttpStatusCode.PermanentRedirect, resposta.StatusCode);
        Assert.AreEqual("https://localhost/api/v1/teste/log", resposta.Headers.Location.ToString());
    }

    [TestMethod]
    public async Task Http_EmDesenvolvimento_SemPortaConfigurada_NaoRedirecionaParaUmaPortaInexistente()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = ClienteHttp(fabrica);

        HttpResponseMessage resposta = await cliente.GetAsync("/api/v1/teste/log");

        Assert.AreEqual(System.Net.HttpStatusCode.OK, resposta.StatusCode);
    }

    private static HttpClient ClienteHttp(FabricaWeb fabrica) =>
        fabrica.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new System.Uri("http://localhost"),
            AllowAutoRedirect = false
        });
}
