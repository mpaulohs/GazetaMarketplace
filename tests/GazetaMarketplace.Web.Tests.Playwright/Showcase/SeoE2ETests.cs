using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Showcase;

/// <summary>
/// O SEO básico (NFR-21; tarefa 5.6) no site publicado: <c>/robots.txt</c>, <c>/sitemap.xml</c> (o anúncio publicado entra; arquivado pelo painel, sai na hora e a página dele vira 404 com <c>noindex</c>) e o
/// que o navegador lê no <c>&lt;head&gt;</c> do início, de uma categoria, de um anúncio (com a capa no Open Graph), da busca e dos favoritos. A conta do E2E (Administrador) publica e arquiva pelas telas.
/// Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class SeoE2ETests : SitePage
#pragma warning restore CA1515
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private static string BaseUrl => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/');

    private static string Url(string path) => BaseUrl + path;

    private async Task<string> MetaAsync(IPage page, string selector) =>
        await page.Locator(selector).GetAttributeAsync("content").ConfigureAwait(false);

    private async Task<string> SourceAsync(string path)
    {
        IAPIResponse response = await Context.APIRequest.GetAsync(Url(path)).ConfigureAwait(false);
        return await response.TextAsync().ConfigureAwait(false);
    }

    private async Task<string[]> SitemapLocsAsync()
    {
        IAPIResponse response = await Context.APIRequest.GetAsync(Url("/sitemap.xml")).ConfigureAwait(false);
        Assert.AreEqual(200, response.Status);
        StringAssert.StartsWith(response.Headers["content-type"], "application/xml");
        return [.. XDocument.Parse(await response.TextAsync().ConfigureAwait(false)).Root!.Elements(Ns + "url").Select(u => u.Element(Ns + "loc")!.Value)];
    }

    [TestMethod]
    public async Task Robots_LiberaOSite_FechaPainelEApi_ApontaOMapaDoSite()
    {
        IAPIResponse response = await Context.APIRequest.GetAsync(Url("/robots.txt")).ConfigureAwait(false);
        string text = await response.TextAsync().ConfigureAwait(false);

        Assert.AreEqual(200, response.Status);
        StringAssert.StartsWith(response.Headers["content-type"], "text/plain");
        StringAssert.Contains(text, "User-agent: *");
        StringAssert.Contains(text, "Disallow: /painel");
        StringAssert.Contains(text, "Disallow: /api/");
        StringAssert.Contains(text, $"Sitemap: {BaseUrl}/sitemap.xml");
        Assert.IsFalse(Regex.IsMatch(text, @"^Disallow:\s*/\s*$", RegexOptions.Multiline), "o site público não é bloqueado");
    }

    [TestMethod]
    public async Task Mapa_ListaInicioCategoriaEAnuncio_ArquivarTiraDoMapaNaHora_EAPaginaViraNoindex()
    {
        using FakeViaCep viaCep = new();
        await PublishingFlow.SignInAdminAsync(Page).ConfigureAwait(false);
        string title = await PublishingFlow.PublishAsync(Page, "Sitemap caderno", 1).ConfigureAwait(false);
        string path = await PublishingFlow.PublicPathAsync(Browser, title).ConfigureAwait(false);
        string id = Regex.Match(path, @"/anuncio/(\d+)/").Groups[1].Value;

        string[] before = await SitemapLocsAsync().ConfigureAwait(false);
        string categoryPath = Regex.Match(await SourceAsync(path).ConfigureAwait(false), @"href=""(/categoria/[^""]+)""[^>]*>Livros e revistas").Groups[1].Value;

        Assert.AreEqual(BaseUrl + "/", before[0], "o início vem primeiro, com o endereço do site");
        Assert.IsTrue(before.Contains(BaseUrl + path), "o anúncio publicado está no mapa: " + path);
        Assert.IsTrue(before.Contains(BaseUrl + categoryPath), "a categoria do anúncio está no mapa: " + categoryPath);
        Assert.IsTrue(before.All(l => l.StartsWith(BaseUrl + "/", StringComparison.Ordinal)));
        Assert.AreEqual(before.Length, before.Distinct().Count());

        // O Administrador arquiva pelo painel
        await Page.GotoAsync(Url($"/painel/anuncios/{id}/editar")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio arquivado" })).ToBeVisibleAsync().ConfigureAwait(false);

        string[] after = await SitemapLocsAsync().ConfigureAwait(false);
        Assert.IsFalse(after.Contains(BaseUrl + path), "arquivado sai do mapa na hora");
        Assert.IsTrue(after.Length < before.Length);

        IPage visitor = await (await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true }).ConfigureAwait(false)).NewPageAsync().ConfigureAwait(false);
        IResponse gone = (await visitor.GotoAsync(Url(path)).ConfigureAwait(false))!;
        Assert.AreEqual(404, gone.Status);
        Assert.AreEqual("noindex", await MetaAsync(visitor, "meta[name='robots']").ConfigureAwait(false));
        Assert.AreEqual(0, await visitor.Locator("link[rel='canonical'], meta[property^='og:'], meta[name='description']").CountAsync().ConfigureAwait(false));
        await visitor.Context.CloseAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Head_InicioCategoriaEAnuncio_TemTituloDescricaoCanonicoEOpenGraph_BuscaEFavoritosSaemComNoindex()
    {
        using FakeViaCep viaCep = new();
        await PublishingFlow.SignInAdminAsync(Page).ConfigureAwait(false);
        string title = await PublishingFlow.PublishAsync(Page, "Head caderno", 1).ConfigureAwait(false);
        string path = await PublishingFlow.PublicPathAsync(Browser, title).ConfigureAwait(false);
        IPage visitor = await (await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true }).ConfigureAwait(false)).NewPageAsync().ConfigureAwait(false);

        // Início
        await visitor.GotoAsync(Url("/")).ConfigureAwait(false);
        await Expect(visitor).ToHaveTitleAsync("Classificados de veículos, imóveis, serviços e vagas · GazetaMarketplace").ConfigureAwait(false);
        StringAssert.Contains(await MetaAsync(visitor, "meta[name='description']").ConfigureAwait(false), "veículos, imóveis, serviços");
        Assert.AreEqual(BaseUrl + "/", await visitor.Locator("link[rel='canonical']").GetAttributeAsync("href").ConfigureAwait(false));
        Assert.AreEqual(0, await visitor.Locator("meta[name='robots']").CountAsync().ConfigureAwait(false), "o início pode ser indexado");

        // Anúncio: título, descrição (até 160), canônico e a capa em miniatura no Open Graph
        await visitor.GotoAsync(Url(path)).ConfigureAwait(false);
        await Expect(visitor).ToHaveTitleAsync(title + " · GazetaMarketplace").ConfigureAwait(false);
        string description = (await MetaAsync(visitor, "meta[name='description']").ConfigureAwait(false))!;
        Assert.IsTrue(description.Length is > 0 and <= 160, description);
        Assert.AreEqual(BaseUrl + path, await visitor.Locator("link[rel='canonical']").GetAttributeAsync("href").ConfigureAwait(false));
        Assert.AreEqual(title, await MetaAsync(visitor, "meta[property='og:title']").ConfigureAwait(false));
        Assert.AreEqual(description, await MetaAsync(visitor, "meta[property='og:description']").ConfigureAwait(false));
        string image = (await MetaAsync(visitor, "meta[property='og:image']").ConfigureAwait(false))!;
        StringAssert.Matches(image, new Regex("^" + Regex.Escape(BaseUrl) + @"/fotos/\d+/\d+-480\.webp$"));
        IAPIResponse photo = await visitor.Context.APIRequest.GetAsync(image).ConfigureAwait(false);
        Assert.AreEqual(200, photo.Status, "a capa do Open Graph abre de verdade");
        Assert.AreEqual("image/webp", photo.Headers["content-type"]);

        // Categoria do anúncio
        await visitor.GotoAsync(Url(path)).ConfigureAwait(false);
        string categoryHref = (await visitor.GetByRole(AriaRole.Link, new() { Name = "Livros e revistas" }).First.GetAttributeAsync("href").ConfigureAwait(false))!;
        await visitor.GotoAsync(Url(categoryHref + "?utm_source=jornal")).ConfigureAwait(false);
        await Expect(visitor).ToHaveTitleAsync(new Regex(@"^Anúncios de Livros e revistas · .+ · GazetaMarketplace$")).ConfigureAwait(false);
        Assert.AreEqual(BaseUrl + categoryHref, await visitor.Locator("link[rel='canonical']").GetAttributeAsync("href").ConfigureAwait(false), "o canônico não leva o parâmetro de rastreio");
        StringAssert.Contains(await MetaAsync(visitor, "meta[name='description']").ConfigureAwait(false), "Livros e revistas");

        // Busca e favoritos: fora do índice, mas os links seguem
        foreach (string noIndex in new[] { "/busca?q=caderno", "/favoritos" })
        {
            await visitor.GotoAsync(Url(noIndex)).ConfigureAwait(false);
            Assert.AreEqual("noindex, follow", await MetaAsync(visitor, "meta[name='robots']").ConfigureAwait(false), noIndex);
            Assert.AreEqual(0, await visitor.Locator("link[rel='canonical']").CountAsync().ConfigureAwait(false), noIndex);
        }

        await visitor.Context.CloseAsync().ConfigureAwait(false);
    }
}
