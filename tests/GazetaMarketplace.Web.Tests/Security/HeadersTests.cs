using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class HeadersTests
#pragma warning restore CA1515
{
    [TestMethod]
    [DataRow("/")]
    [DataRow("/api/v1/teste/log")]
    [DataRow("/api/v1/teste/erro/inesperado")]
    [DataRow("/api/v1/teste/erro/notfound")]
    [DataRow("/nao-existe")]
    public async Task TodaResposta_TemOsCabecalhosObrigatorios(string path)
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(path);

        Assert.AreEqual("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.AreEqual("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.AreEqual("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
        StringAssert.Contains(response.Headers.GetValues("Permissions-Policy").Single(), "camera=()");
        StringAssert.Contains(response.Headers.GetValues("Permissions-Policy").Single(), "geolocation=()");
        Assert.IsTrue(response.Headers.Contains("Content-Security-Policy"));
    }

    [TestMethod]
    public async Task Hsts_SoEmProducao()
    {
        using WebFactory test = new("Testing");
        using HttpClient testClient = test.CreateClient();
        HttpResponseMessage inTest = await testClient.GetAsync("/api/v1/teste/log");
        Assert.IsFalse(inTest.Headers.Contains("Strict-Transport-Security"));

        using WebFactory production = new("Production", WebFactory.ProductionConfiguration());
        using HttpClient productionClient = production.CreateClient();
        HttpResponseMessage inProduction = await productionClient.GetAsync("/api/v1/teste/log");
        string hsts = inProduction.Headers.GetValues("Strict-Transport-Security").Single();
        StringAssert.Contains(hsts, "max-age=31536000");
        StringAssert.Contains(hsts, "includeSubDomains");
    }

    [TestMethod]
    public async Task Http_RedirecionaParaHttps_ComAPortaDaConfiguracao()
    {
        using WebFactory factory = new(configuration: new System.Collections.Generic.Dictionary<string, string> { ["HttpsRedirection:HttpsPort"] = "443" });
        using HttpClient client = HttpOnlyClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/v1/teste/log");

        Assert.AreEqual(System.Net.HttpStatusCode.PermanentRedirect, response.StatusCode);
        Assert.AreEqual("https://localhost/api/v1/teste/log", response.Headers.Location.ToString());
    }

    [TestMethod]
    public async Task Http_EmProducao_RedirecionaParaHttpsNaPorta443_SemConfigurar()
    {
        using WebFactory production = new("Production", WebFactory.ProductionConfiguration());
        using HttpClient client = HttpOnlyClient(production);

        HttpResponseMessage response = await client.GetAsync("/api/v1/teste/log");

        Assert.AreEqual(System.Net.HttpStatusCode.PermanentRedirect, response.StatusCode);
        Assert.AreEqual("https://localhost/api/v1/teste/log", response.Headers.Location.ToString());
    }

    [TestMethod]
    public async Task Http_EmDesenvolvimento_SemPortaConfigurada_NaoRedirecionaParaUmaPortaInexistente()
    {
        using WebFactory factory = new();
        using HttpClient client = HttpOnlyClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/v1/teste/log");

        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    private static HttpClient HttpOnlyClient(WebFactory factory) =>
        factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new System.Uri("http://localhost"),
            AllowAutoRedirect = false
        });
}
