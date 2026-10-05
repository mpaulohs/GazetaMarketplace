using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Seo;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Seo;

/// <summary>
/// O mapa do site (<c>/sitemap.xml</c>, NFR-21; tarefa 5.6): o início, as categorias com anúncio publicado (e os ancestrais delas) e todos os anúncios publicados, com os endereços de <c>Site:BaseUrl</c>.
/// Nestes testes o repositório é o de mentira (devolve o que o teste pôs, sem filtro): a regra "só publicados" é da consulta e é provada no SQL Server (integração) e no navegador (E2E).
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SitemapTests
#pragma warning restore CA1515
{
    private const string BaseUrl = "https://gazeta.exemplo.com.br";
    private const int Cars = 33;
    private const int Motorcycles = 36;

    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private static Dictionary<string, string> Configuration => new() { ["Site:BaseUrl"] = BaseUrl };

    private static StubSitemapReadRepository Stub(DraftSite site) => site.Harness.Factory.Services.GetRequiredService<StubSitemapReadRepository>();

    private static async Task<(HttpResponseMessage Response, XDocument Xml)> GetAsync(DraftSite site, string host = null)
    {
        using HttpClient visitor = site.Harness.Anonymous();
        using HttpRequestMessage request = new(HttpMethod.Get, "/sitemap.xml");
        if (host is not null)
        {
            request.Headers.Host = host;
        }

        HttpResponseMessage response = await visitor.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();
        return (response, response.StatusCode == HttpStatusCode.OK ? XDocument.Parse(body) : null);
    }

    private static string[] Locs(XDocument xml) => [.. xml.Root.Elements(Ns + "url").Select(u => u.Element(Ns + "loc")!.Value)];

    private static async Task<CategoryTreeSnapshot> TreeAsync(DraftSite site) => await site.Harness.Factory.Services.GetRequiredService<ICategoryTree>().GetAsync(default);

    [TestMethod]
    public async Task SoOInicio_QuandoNaoHaAnuncioPublicado()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);

        (HttpResponseMessage response, XDocument xml) = await GetAsync(site);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        CollectionAssert.AreEqual(new[] { BaseUrl + "/" }, Locs(xml), "sem anúncios: só o início, nenhuma categoria");
    }

    [TestMethod]
    public async Task ListaInicio_CategoriasComAnuncio_ETodosOsAnunciosPublicados_NaOrdemDaConsulta()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        CategoryTreeSnapshot tree = await TreeAsync(site);
        Stub(site).CategoryIds.Add(Cars);
        Stub(site).Ads.AddRange([
            new SitemapAd(12, "Honda Civic 2018", new DateTime(2026, 9, 12, 15, 0, 0, DateTimeKind.Utc)),
            new SitemapAd(7, "Moto CG 160", new DateTime(2026, 9, 10, 8, 30, 0, DateTimeKind.Utc))]);

        (_, XDocument xml) = await GetAsync(site);

        string[] locs = Locs(xml);
        Assert.AreEqual(BaseUrl + "/", locs[0]);
        StringAssert.EndsWith(locs[^2], "/anuncio/12/honda-civic-2018");
        StringAssert.EndsWith(locs[^1], "/anuncio/7/moto-cg-160");
        string carsSlug = tree.Find(Cars).Slug;
        Assert.IsTrue(locs.Contains($"{BaseUrl}/categoria/{carsSlug}"), "a categoria com anúncio entra");
        Assert.IsTrue(locs.All(l => l.StartsWith(BaseUrl + "/", StringComparison.Ordinal)), "todos os endereços usam o endereço do site");
        Assert.IsFalse(locs.Contains($"{BaseUrl}/categoria/{tree.Find(Motorcycles).Slug}"), "categoria sem anúncio fica de fora");
        string[] lastmods = [.. xml.Root.Elements(Ns + "url").Where(u => u.Element(Ns + "loc")!.Value.Contains("/anuncio/", StringComparison.Ordinal)).Select(u => u.Element(Ns + "lastmod")!.Value)];
        CollectionAssert.AreEqual(new[] { "2026-09-12", "2026-09-10" }, lastmods, "a data de publicação, só o dia");
    }

    [TestMethod]
    public async Task OAncestralDaCategoriaComAnuncio_EntraNoMapa_PoisAPaginaDeleMostraOsAnunciosDaDescendente()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        CategoryTreeSnapshot tree = await TreeAsync(site);
        Stub(site).CategoryIds.Add(Cars);

        (_, XDocument xml) = await GetAsync(site);

        string[] locs = Locs(xml);
        foreach (CategoryNode ancestor in tree.AncestorsOf(Cars))
        {
            Assert.IsTrue(locs.Contains($"{BaseUrl}/categoria/{ancestor.Slug}"), "o ancestral " + ancestor.Name);
        }

        Assert.IsTrue(tree.AncestorsOf(Cars).Count > 0, "Carros tem categoria principal acima");
        Assert.AreEqual(1 + 1 + tree.AncestorsOf(Cars).Count, locs.Length, "início, a categoria e os ancestrais, nada além");
    }

    [TestMethod]
    public async Task OEnderecoVemDeSiteBaseUrl_NuncaDoCabecalhoHost()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        Stub(site).Ads.Add(new SitemapAd(1, "Anúncio", null));

        (_, XDocument xml) = await GetAsync(site, host: "atacante.example");

        Assert.IsTrue(Locs(xml).All(l => l.StartsWith(BaseUrl + "/", StringComparison.Ordinal)), string.Join(", ", Locs(xml)));
        Assert.IsFalse(xml.ToString().Contains("atacante", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task AnuncioSemDataDePublicacao_FicaSemLastmod()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        Stub(site).Ads.Add(new SitemapAd(1, "Anúncio", null));

        (_, XDocument xml) = await GetAsync(site);

        Assert.IsNull(xml.Root.Elements(Ns + "url").Last().Element(Ns + "lastmod"));
    }

    [TestMethod]
    public async Task RespondaXml_SemCache_ECadaPedidoConsultaDeNovo_AnuncioRetiradoSaiNaHora()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        Stub(site).Ads.AddRange([new SitemapAd(1, "Primeiro", null), new SitemapAd(2, "Segundo", null)]);

        (HttpResponseMessage first, XDocument before) = await GetAsync(site);
        Stub(site).Ads.RemoveAll(a => a.Id == 2);
        (_, XDocument after) = await GetAsync(site);

        StringAssert.StartsWith(first.Content.Headers.ContentType!.ToString(), "application/xml");
        Assert.IsTrue(first.Headers.CacheControl!.NoStore, "o mapa não é guardado: arquivar tira o anúncio na hora");
        Assert.AreEqual(3, Locs(before).Length);
        Assert.AreEqual(2, Locs(after).Length);
        Assert.IsFalse(Locs(after).Any(l => l.Contains("/anuncio/2/", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task PedeAoRepositorioOQueSobraDoLimiteDe50MilEnderecos()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        Stub(site).CategoryIds.Add(Cars);
        CategoryTreeSnapshot tree = await TreeAsync(site);
        int pages = 1 + 1 + tree.AncestorsOf(Cars).Count;

        await GetAsync(site);

        Assert.AreEqual(SitemapDocument.MaxUrls - pages, Stub(site).TakeRequests.Single(), "os anúncios ocupam o que sobra depois do início e das categorias");
    }

    [TestMethod]
    public async Task FalhaNaConsulta_Da503SemDetalhe()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        Stub(site).Failure = new InvalidOperationException("segredo-do-banco");
        using HttpClient visitor = site.Harness.Anonymous();

        HttpResponseMessage response = await visitor.GetAsync("/sitemap.xml");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.IsFalse((await response.Content.ReadAsStringAsync()).Contains("segredo-do-banco", StringComparison.Ordinal));
    }

    // ---------- O documento ----------

    [TestMethod]
    public void Documento_EscapaOsCaracteresDoXml_EDeclaraONamespaceDoProtocolo()
    {
        string xml = SitemapDocument.Build(BaseUrl + "/", [new SitemapEntry("/anuncio/1/a&b<c>", new DateTime(2026, 1, 5))]);

        XDocument parsed = XDocument.Parse(xml);
        Assert.AreEqual(Ns + "urlset", parsed.Root!.Name);
        Assert.AreEqual(BaseUrl + "/anuncio/1/a&b<c>", parsed.Root.Element(Ns + "url")!.Element(Ns + "loc")!.Value, "o XML lido de volta traz o endereço original");
        StringAssert.Contains(xml, "a&amp;b&lt;c&gt;");
        StringAssert.Contains(xml, "<lastmod>2026-01-05</lastmod>");
        StringAssert.StartsWith(xml, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    }

    [TestMethod]
    public void Documento_NaoPassaDe50MilEnderecos()
    {
        IEnumerable<SitemapEntry> entries = Enumerable.Range(1, SitemapDocument.MaxUrls + 25).Select(i => new SitemapEntry("/anuncio/" + i, null));

        XDocument parsed = XDocument.Parse(SitemapDocument.Build(BaseUrl, entries));

        Assert.AreEqual(50_000, parsed.Root!.Elements(Ns + "url").Count(), "o limite do protocolo sitemaps.org");
    }
}
