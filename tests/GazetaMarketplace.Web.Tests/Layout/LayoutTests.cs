using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Layout;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class LayoutTests
#pragma warning restore CA1515
{
    private static readonly string[] _pages = ["/teste/publica", "/teste/painel", "/teste/estados", "/"];

    [TestMethod]
    [DataRow("/teste/publica")]
    [DataRow("/teste/painel")]
    [DataRow("/")]
    public async Task Html_TemLangPtBr_SkipLink_E_MainComFoco(string path)
    {
        string html = await DownloadAsync(path);

        StringAssert.Matches(html, new Regex(@"<html[^>]*\blang=""pt-BR"""));
        StringAssert.Matches(html, new Regex(@"<a[^>]*href=""#conteudo""[^>]*>\s*Ir para o conteúdo\s*</a>"));
        StringAssert.Matches(html, new Regex(@"<main[^>]*\bid=""conteudo""[^>]*\btabindex=""-1"""));
    }

    [TestMethod]
    public async Task LayoutPainel_ExpoeOTokenAntiforgery_ELayoutPublicoNaoEmiteToken()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage panel = await client.GetAsync("/teste/painel");
        string panelHtml = await panel.Content.ReadAsStringAsync();
        StringAssert.Matches(panelHtml, new Regex(@"<meta[^>]*name=""request-verification-token""[^>]*content=""[^""]+"""));

        // O site público não escreve nada no servidor: sem token, sem cookie (a página continua cacheável)
        HttpResponseMessage publicPage = await client.GetAsync("/teste/publica");
        string publicHtml = await publicPage.Content.ReadAsStringAsync();
        Assert.IsFalse(publicHtml.Contains("request-verification-token", System.StringComparison.Ordinal));
        Assert.IsFalse(publicPage.Headers.Contains("Set-Cookie"));
    }

    [TestMethod]
    public async Task Html_PrimeiroLinkDaPagina_EOSkipLink()
    {
        string html = await DownloadAsync("/teste/publica");

        Match first = Regex.Match(html, @"<a\b[^>]*>");
        StringAssert.Contains(first.Value, "href=\"#conteudo\"");
    }

    [TestMethod]
    public async Task LayoutPublico_TemBuscaEFavoritosComCaminhosFixos()
    {
        string html = await DownloadAsync("/teste/publica");

        StringAssert.Matches(html, new Regex(@"<form[^>]*\bmethod=""get""[^>]*\baction=""/busca""|<form[^>]*\baction=""/busca""[^>]*\bmethod=""get"""));
        StringAssert.Matches(html, new Regex(@"<a[^>]*href=""/favoritos""[^>]*>[\s\S]*?Favoritos"));
        StringAssert.Matches(html, new Regex(@"<footer[\s\S]*Área da equipe[\s\S]*</footer>"));
    }

    [TestMethod]
    public async Task PaginaNaoCarregaRecursosExternos()
    {
        using WebFactory factory = new(withDatabase: true);
        using HttpClient client = factory.CreateClient();

        foreach (string path in _pages)
        {
            string html = await (await client.GetAsync(path)).Content.ReadAsStringAsync();

            Assert.AreEqual(0, Regex.Matches(html, @"\b(src|href|action|data)\s*=\s*""(https?:)?//", RegexOptions.IgnoreCase).Count, path + ": URL absoluta no HTML");
            Assert.AreEqual(0, Regex.Matches(html, @"@import|url\(\s*['""]?(https?:)?//", RegexOptions.IgnoreCase).Count, path);

            // Todo CSS e JS referenciado é servido pelo próprio site e não puxa nada de fora
            foreach (Match resource in Regex.Matches(html, @"(?:src|href)=""(/[^""#]+\.(?:css|js))(?:\?[^""]*)?"""))
            {
                HttpResponseMessage response = await client.GetAsync(resource.Groups[1].Value + "?" + "v=1");
                Assert.IsTrue(response.IsSuccessStatusCode, resource.Groups[1].Value + " não foi servido");
                string content = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(0, Regex.Matches(content, @"@import\s+(url\()?['""]?(https?:)?//|url\(\s*['""]?https?://").Count, resource.Groups[1].Value);
            }
        }
    }

    [TestMethod]
    public async Task NenhumScriptOuEventoInline()
    {
        using WebFactory factory = new(withDatabase: true);
        using HttpClient client = factory.CreateClient();

        foreach (string path in _pages)
        {
            string html = await (await client.GetAsync(path)).Content.ReadAsStringAsync();

            foreach (Match script in Regex.Matches(html, @"<script\b[^>]*>", RegexOptions.IgnoreCase))
            {
                StringAssert.Matches(script.Value, new Regex(@"\bsrc="""), path + ": script sem src (inline): " + script.Value);
            }

            Assert.AreEqual(0, Regex.Matches(html, @"<[a-z][^>]*\son[a-z]+\s*=", RegexOptions.IgnoreCase).Count, path + ": evento inline");
            Assert.AreEqual(0, Regex.Matches(html, @"<[a-z][^>]*\sstyle\s*=", RegexOptions.IgnoreCase).Count, path + ": atributo style (a CSP bloqueia)");
            Assert.AreEqual(0, Regex.Matches(html, @"<style\b", RegexOptions.IgnoreCase).Count, path + ": <style> inline");
            Assert.AreEqual(0, Regex.Matches(html, @"javascript:", RegexOptions.IgnoreCase).Count, path);
        }
    }

    [TestMethod]
    public async Task Jquery_NaoEhCarregadoPeloLayout()
    {
        string html = await DownloadAsync("/teste/publica");

        Assert.IsFalse(html.Contains("jquery", System.StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task SemJavaScript_MenuDoPainelFicaVisivel()
    {
        string html = await DownloadAsync("/teste/painel");

        StringAssert.Matches(html, new Regex(@"<noscript>\s*<link[^>]*href=""/css/sem-js\.css"));
    }

    private static async Task<string> DownloadAsync(string path)
    {
        using WebFactory factory = new(withDatabase: true);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(WebFactory.RoleHeader, "Administrador");
        HttpResponseMessage response = await client.GetAsync(path);
        Assert.IsTrue(response.IsSuccessStatusCode, path + " respondeu " + (int)response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }
}
