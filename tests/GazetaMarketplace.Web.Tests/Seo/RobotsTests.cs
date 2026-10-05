using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Seo;
using GazetaMarketplace.Web.Tests.Ads;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Seo;

/// <summary>O <c>/robots.txt</c> (NFR-21; tarefa 5.6): libera o site público, fecha o painel e a API e aponta o mapa do site, com o endereço de <c>Site:BaseUrl</c>.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class RobotsTests
#pragma warning restore CA1515
{
    private const string BaseUrl = "https://gazeta.exemplo.com.br";

    private static async Task<(HttpResponseMessage Response, string[] Lines)> GetAsync(DraftSite site, string host = null)
    {
        using HttpClient visitor = site.Harness.Anonymous();
        using HttpRequestMessage request = new(HttpMethod.Get, "/robots.txt");
        if (host is not null)
        {
            request.Headers.Host = host;
        }

        HttpResponseMessage response = await visitor.SendAsync(request);
        return (response, [.. (await response.Content.ReadAsStringAsync()).Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0)]);
    }

    [TestMethod]
    public async Task LiberaOSitePublico_FechaPainelEApi_ApontaOMapaDoSite()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: new() { ["Site:BaseUrl"] = BaseUrl });

        (HttpResponseMessage response, string[] lines) = await GetAsync(site);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.StartsWith(response.Content.Headers.ContentType!.ToString(), "text/plain");
        CollectionAssert.AreEqual(
            new[] { "User-agent: *", "Disallow: /painel", "Disallow: /api/", "Disallow: /favoritos/lista", $"Sitemap: {BaseUrl}/sitemap.xml" },
            lines);
    }

    [TestMethod]
    public async Task NaoBloqueiaOSitePublico_NemABuscaENemOsFavoritos_ParaORoboLerONoindexDelas()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: new() { ["Site:BaseUrl"] = BaseUrl });

        (_, string[] lines) = await GetAsync(site);

        string[] disallowed = [.. lines.Where(l => l.StartsWith("Disallow:", System.StringComparison.Ordinal)).Select(l => l["Disallow:".Length..].Trim())];
        CollectionAssert.DoesNotContain(disallowed, "/", "o site público não é bloqueado");
        CollectionAssert.DoesNotContain(disallowed, "/busca");
        CollectionAssert.DoesNotContain(disallowed, "/favoritos");
        CollectionAssert.DoesNotContain(disallowed, "/anuncio");
        CollectionAssert.DoesNotContain(disallowed, "/categoria");
        CollectionAssert.DoesNotContain(disallowed, "/fotos");
    }

    [TestMethod]
    public async Task OMapaVemDeSiteBaseUrl_NuncaDoCabecalhoHost()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: new() { ["Site:BaseUrl"] = BaseUrl });

        (_, string[] lines) = await GetAsync(site, host: "atacante.example");

        Assert.AreEqual($"Sitemap: {BaseUrl}/sitemap.xml", lines.Single(l => l.StartsWith("Sitemap:", System.StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Texto_TerminaComQuebraDeLinha_ENaoRepeteABarraDoEndereco()
    {
        string text = RobotsText.Build(BaseUrl + "/");

        Assert.IsTrue(text.EndsWith('\n'));
        StringAssert.Contains(text, $"Sitemap: {BaseUrl}/sitemap.xml\n");
        Assert.IsFalse(text.Contains("//sitemap", System.StringComparison.Ordinal));
    }
}
