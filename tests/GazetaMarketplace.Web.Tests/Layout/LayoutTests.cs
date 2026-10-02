using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Layout;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class LayoutTests
#pragma warning restore CA1515
{
    private static readonly string[] _paginas = ["/teste/publica", "/teste/painel", "/teste/estados", "/"];

    [TestMethod]
    [DataRow("/teste/publica")]
    [DataRow("/teste/painel")]
    [DataRow("/")]
    public async Task Html_TemLangPtBr_SkipLink_E_MainComFoco(string caminho)
    {
        string html = await BaixarAsync(caminho);

        StringAssert.Matches(html, new Regex(@"<html[^>]*\blang=""pt-BR"""));
        StringAssert.Matches(html, new Regex(@"<a[^>]*href=""#conteudo""[^>]*>\s*Ir para o conteúdo\s*</a>"));
        StringAssert.Matches(html, new Regex(@"<main[^>]*\bid=""conteudo""[^>]*\btabindex=""-1"""));
    }

    [TestMethod]
    public async Task LayoutPainel_ExpoeOTokenAntiforgery_ELayoutPublicoNaoEmiteToken()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage painel = await cliente.GetAsync("/teste/painel");
        string htmlPainel = await painel.Content.ReadAsStringAsync();
        StringAssert.Matches(htmlPainel, new Regex(@"<meta[^>]*name=""request-verification-token""[^>]*content=""[^""]+"""));

        // O site público não escreve nada no servidor: sem token, sem cookie (a página continua cacheável)
        HttpResponseMessage publica = await cliente.GetAsync("/teste/publica");
        string htmlPublico = await publica.Content.ReadAsStringAsync();
        Assert.IsFalse(htmlPublico.Contains("request-verification-token", System.StringComparison.Ordinal));
        Assert.IsFalse(publica.Headers.Contains("Set-Cookie"));
    }

    [TestMethod]
    public async Task Html_PrimeiroLinkDaPagina_EOSkipLink()
    {
        string html = await BaixarAsync("/teste/publica");

        Match primeiro = Regex.Match(html, @"<a\b[^>]*>");
        StringAssert.Contains(primeiro.Value, "href=\"#conteudo\"");
    }

    [TestMethod]
    public async Task LayoutPublico_TemBuscaEFavoritosComCaminhosFixos()
    {
        string html = await BaixarAsync("/teste/publica");

        StringAssert.Matches(html, new Regex(@"<form[^>]*\bmethod=""get""[^>]*\baction=""/busca""|<form[^>]*\baction=""/busca""[^>]*\bmethod=""get"""));
        StringAssert.Matches(html, new Regex(@"<a[^>]*href=""/favoritos""[^>]*>[\s\S]*?Favoritos"));
        StringAssert.Matches(html, new Regex(@"<footer[\s\S]*Área da equipe[\s\S]*</footer>"));
    }

    [TestMethod]
    public async Task PaginaNaoCarregaRecursosExternos()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        foreach (string caminho in _paginas)
        {
            string html = await (await cliente.GetAsync(caminho)).Content.ReadAsStringAsync();

            Assert.AreEqual(0, Regex.Matches(html, @"\b(src|href|action|data)\s*=\s*""(https?:)?//", RegexOptions.IgnoreCase).Count, caminho + ": URL absoluta no HTML");
            Assert.AreEqual(0, Regex.Matches(html, @"@import|url\(\s*['""]?(https?:)?//", RegexOptions.IgnoreCase).Count, caminho);

            // Todo CSS e JS referenciado é servido pelo próprio site e não puxa nada de fora
            foreach (Match recurso in Regex.Matches(html, @"(?:src|href)=""(/[^""#]+\.(?:css|js))(?:\?[^""]*)?"""))
            {
                HttpResponseMessage resposta = await cliente.GetAsync(recurso.Groups[1].Value + "?" + "v=1");
                Assert.IsTrue(resposta.IsSuccessStatusCode, recurso.Groups[1].Value + " não foi servido");
                string conteudo = await resposta.Content.ReadAsStringAsync();
                Assert.AreEqual(0, Regex.Matches(conteudo, @"@import\s+(url\()?['""]?(https?:)?//|url\(\s*['""]?https?://").Count, recurso.Groups[1].Value);
            }
        }
    }

    [TestMethod]
    public async Task NenhumScriptOuEventoInline()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        foreach (string caminho in _paginas)
        {
            string html = await (await cliente.GetAsync(caminho)).Content.ReadAsStringAsync();

            foreach (Match script in Regex.Matches(html, @"<script\b[^>]*>", RegexOptions.IgnoreCase))
            {
                StringAssert.Matches(script.Value, new Regex(@"\bsrc="""), caminho + ": script sem src (inline): " + script.Value);
            }

            Assert.AreEqual(0, Regex.Matches(html, @"<[a-z][^>]*\son[a-z]+\s*=", RegexOptions.IgnoreCase).Count, caminho + ": evento inline");
            Assert.AreEqual(0, Regex.Matches(html, @"<[a-z][^>]*\sstyle\s*=", RegexOptions.IgnoreCase).Count, caminho + ": atributo style (a CSP bloqueia)");
            Assert.AreEqual(0, Regex.Matches(html, @"<style\b", RegexOptions.IgnoreCase).Count, caminho + ": <style> inline");
            Assert.AreEqual(0, Regex.Matches(html, @"javascript:", RegexOptions.IgnoreCase).Count, caminho);
        }
    }

    [TestMethod]
    public async Task Jquery_NaoEhCarregadoPeloLayout()
    {
        string html = await BaixarAsync("/teste/publica");

        Assert.IsFalse(html.Contains("jquery", System.StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task SemJavaScript_MenuDoPainelFicaVisivel()
    {
        string html = await BaixarAsync("/teste/painel");

        StringAssert.Matches(html, new Regex(@"<noscript>\s*<link[^>]*href=""/css/sem-js\.css"));
    }

    private static async Task<string> BaixarAsync(string caminho)
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(FabricaWeb.CabecalhoPapel, "Administrador");
        HttpResponseMessage resposta = await cliente.GetAsync(caminho);
        Assert.IsTrue(resposta.IsSuccessStatusCode, caminho + " respondeu " + (int)resposta.StatusCode);
        return await resposta.Content.ReadAsStringAsync();
    }
}
