using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Ads;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Search;

/// <summary><c>GET /api/v1/public/cities?uf=</c> (S10): as cidades de uma UF para o filtro da busca, sem login e com cache que pode ser compartilhado. A rota de cidades da equipe continua exigindo login.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PublicCitiesTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task SemLogin_ListaAsCidadesDaUf_EmOrdemAlfabetica_ComCodigoENome()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();

        HttpResponseMessage response = await visitor.GetAsync("/api/v1/public/cities?uf=SP");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        CollectionAssert.AreEqual(new[] { "Campinas", "São Paulo" }, body.RootElement.EnumerateArray().Select(c => c.GetProperty("name").GetString()).ToArray());
        CollectionAssert.AreEquivalent(new[] { "ibgeCode", "name" }, body.RootElement[0].EnumerateObject().Select(p => p.Name).ToArray());
    }

    [TestMethod]
    public async Task CacheDeDezMinutos_PodeSerCompartilhado()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();

        HttpResponseMessage response = await visitor.GetAsync("/api/v1/public/cities?uf=SP");

        string cache = response.Headers.CacheControl!.ToString();
        StringAssert.Contains(cache, "public");
        StringAssert.Contains(cache, "max-age=600");
    }

    [TestMethod]
    [DataRow("XX")]
    [DataRow("")]
    [DataRow("São Paulo")]
    public async Task UfInvalidaOuAusente_Da400(string uf)
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();

        HttpResponseMessage response = await visitor.GetAsync("/api/v1/public/cities?uf=" + System.Uri.EscapeDataString(uf));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task UfSemCargaDoIbge_DevolveListaVazia()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();

        HttpResponseMessage response = await visitor.GetAsync("/api/v1/public/cities?uf=AC");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("[]", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task RotaDaEquipe_ContinuaExigindoLogin()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/v1/cities?uf=SP")).StatusCode);
    }
}
