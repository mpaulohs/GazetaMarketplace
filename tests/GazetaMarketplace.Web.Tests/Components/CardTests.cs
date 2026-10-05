using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Components;

/// <summary>O card do anúncio (3.8) nas três variantes, o corpo textual e a página de componentes do painel (só Development, só Administrador).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CardTests
#pragma warning restore CA1515
{
    private const string ComponentsPage = "/painel/componentes";

    private static async Task<string> CardAsync(string query)
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/teste/card?" + query);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static string Attribute(string html, string name) => Regex.Match(html, name + @"=""([^""]*)""").Groups[1].Value;

    private static string LinkName(string html) => WebUtility.HtmlDecode(Regex.Match(html, @"<a [^>]*aria-label=""([^""]*)""").Groups[1].Value);

    private static string Visible(string html) => WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(html, @"<[^>]+>", " "), @"\s+", " ")).Trim();

    [TestMethod]
    public async Task Padrao_TemCapaComDimensoes_ProporcaoReservada_ELazyForaDaPrimeiraLinha()
    {
        string html = await CardAsync("title=Honda%20Civic%202018&group=Cars&price=6200000&cover=true&city=Campinas&uf=SP");

        Assert.AreEqual(1, Regex.Matches(html, @"<article\b").Count);
        StringAssert.Contains(html, "class=\"card ad-card h-100\"");
        Assert.AreEqual("/fotos/7/9-480.webp", Attribute(html, "src"), "a miniatura de 480 px");
        Assert.AreEqual("480", Attribute(html, "width"));
        Assert.AreEqual("360", Attribute(html, "height"), "4:3 reservado pelos atributos (e pelo CSS)");
        Assert.AreEqual(string.Empty, Attribute(html, "alt"), "a imagem é decorativa: o nome do link já diz tudo");
        Assert.AreEqual("lazy", Attribute(html, "loading"));
        StringAssert.Contains(html, "R$ 62.000");
        Assert.IsFalse(html.Contains("data-ad-placeholder", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task PrimeiraLinha_CarregaAImagemJa()
    {
        string html = await CardAsync("title=Honda&group=Cars&price=6200000&cover=true&eager=true");

        Assert.AreEqual("eager", Attribute(html, "loading"));
    }

    [TestMethod]
    public async Task UmSoLinkCobreOCard_ComNomeAcessivelTituloValorCidade()
    {
        string html = await CardAsync("title=Honda%20Civic%202018&group=Cars&price=6200000&cover=true&city=Campinas&uf=SP");

        Assert.AreEqual(1, Regex.Matches(html, @"<a\b").Count, "um único link por card");
        StringAssert.Matches(html, new Regex(@"<a class=""stretched-link ad-card__link"" href=""/anuncio/7"" aria-label=""[^""]+"">Honda Civic 2018</a>"));
        Assert.AreEqual("Honda Civic 2018, R$ 62.000, Campinas/SP", LinkName(html));
        StringAssert.Contains(html, "<h3 ");
    }

    [TestMethod]
    public async Task Servicos_MostraTipoNoLugarDoPreco()
    {
        string html = await CardAsync("title=Diarista&group=Services&service=Servi%C3%A7os%20dom%C3%A9sticos&cover=true&city=S%C3%A3o%20Paulo&uf=SP");

        StringAssert.Contains(html, "data-ad-value=\"tipo\"");
        StringAssert.Contains(Visible(html), "Serviços domésticos");
        Assert.IsFalse(html.Contains("R$", StringComparison.Ordinal), "Serviços não têm preço");
        Assert.AreEqual("Diarista, Tipo: Serviços domésticos, São Paulo/SP", LinkName(html));
        StringAssert.Contains(html, "<img ", "Serviços mostram a capa normal");
    }

    [TestMethod]
    public async Task Servicos_MesmoComPrecoNaEntrada_NaoMostraPreco()
    {
        string html = await CardAsync("title=Diarista&group=Services&service=Limpeza&price=500000");

        Assert.IsFalse(html.Contains("R$", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Vagas_MostraBlocoNeutro_ESalario()
    {
        string html = await CardAsync("title=Pizzaiolo&group=Jobs&price=280000&area=Alimenta%C3%A7%C3%A3o&city=Campinas&uf=SP&cover=true");

        Assert.IsFalse(html.Contains("<img", StringComparison.Ordinal), "sem <img> em Vagas, mesmo que venha uma capa");
        StringAssert.Contains(html, "data-ad-placeholder");
        StringAssert.Contains(html, "media-reservada");
        StringAssert.Contains(Visible(html), "Vaga de emprego Alimentação");
        StringAssert.Contains(html, "data-ad-value=\"salario\"");
        StringAssert.Contains(Visible(html), "Salário R$ 2.800");
        Assert.AreEqual("Pizzaiolo, Salário R$ 2.800, Campinas/SP", LinkName(html));
        string withoutHeart = Regex.Replace(html, @"<button[^>]*data-favorite-toggle[\s\S]*?</button>", string.Empty);
        Assert.AreEqual(1, Regex.Matches(withoutHeart, @"aria-hidden=""true""").Count, "o bloco neutro é decorativo (o ícone do coração é outro assunto)");
    }

    [TestMethod]
    public async Task Vagas_SemArea_MostraSoOTituloDoBloco()
    {
        string html = await CardAsync("title=Pizzaiolo&group=Jobs&price=280000");

        StringAssert.Contains(Visible(html), "Vaga de emprego");
        StringAssert.Matches(html, new Regex(@"<div[^>]*data-ad-placeholder[^>]*>\s*<span class=""fw-bold"">Vaga de emprego</span>\s*</div>"), "só o título do bloco, sem linha de área");
    }

    [TestMethod]
    public async Task SemCapa_MostraFotoIndisponivel()
    {
        string html = await CardAsync("title=Violão&group=GeneralProducts&price=249990&city=Goi%C3%A2nia&uf=GO");

        Assert.IsFalse(html.Contains("<img", StringComparison.Ordinal));
        StringAssert.Contains(Visible(html), "Foto indisponível");
        StringAssert.Contains(Visible(html), "R$ 2.499,90");
    }

    [TestMethod]
    public async Task SemCidade_SemPreco_ONomeAcessivelNaoTemVirgulaSobrando()
    {
        string noCity = await CardAsync("title=Jaqueta&group=GeneralProducts&price=18000");
        string noPrice = await CardAsync("title=Jaqueta&group=GeneralProducts&city=Goi%C3%A2nia&uf=GO");
        string neither = await CardAsync("title=Jaqueta&group=GeneralProducts");
        string noType = await CardAsync("title=Diarista&group=Services&city=Goi%C3%A2nia&uf=GO");

        Assert.AreEqual("Jaqueta, R$ 180", LinkName(noCity));
        Assert.AreEqual("Jaqueta, Goiânia/GO", LinkName(noPrice));
        Assert.AreEqual("Jaqueta", LinkName(neither));
        Assert.AreEqual("Diarista, Goiânia/GO", LinkName(noType));
        Assert.IsFalse(noCity.Contains("text-body-secondary mb-0", StringComparison.Ordinal), "sem cidade, sem linha de local");
        Assert.IsFalse(noPrice.Contains("data-ad-value", StringComparison.Ordinal), "sem preço, sem linha de valor (nunca \"R$ 0\")");
    }

    [TestMethod]
    public async Task CentavosSoQuandoNaoSaoZero()
    {
        string cents = await CardAsync("title=Violão&price=249990");
        string whole = await CardAsync("title=Jaqueta&price=18000");

        StringAssert.Contains(cents, "R$ 2.499,90");
        StringAssert.Contains(whole, "R$ 180<");
        Assert.IsFalse(whole.Contains("R$ 180,00", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task TextoDoUsuarioSaiCodificado()
    {
        string html = await CardAsync("title=%3Cscript%3Ealert(1)%3C%2Fscript%3E%20%22x%22&group=GeneralProducts&price=100&city=%3Cb%3EX%3C%2Fb%3E&uf=SP&cover=true");

        Assert.IsFalse(html.Contains("<script", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("<b>X", StringComparison.Ordinal));
        StringAssert.Contains(html, "&lt;script&gt;");
        Assert.AreEqual("<script>alert(1)</script> \"x\", R$ 1, <b>X</b>/SP", LinkName(html), "o atributo também é codificado e volta ao texto original");
    }

    [TestMethod]
    public async Task Card_SemEstiloEmLinhaNemManipuladorEmLinha()
    {
        string html = await CardAsync("title=Honda&group=Cars&price=100&cover=true&city=A&uf=SP");

        Assert.IsFalse(Regex.IsMatch(html, @"\sstyle="), "a CSP bloqueia atributo style");
        Assert.IsFalse(Regex.IsMatch(html, @"\son[a-z]+="), "sem onerror/onclick em linha: o módulo ad-card.js cuida da foto que falha");
    }

    [TestMethod]
    public async Task Corpo_TituloNoNivelPedido_TextoCodificado_QuebrasPeloCss()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        string asPage = await client.GetStringAsync("/teste/corpo?title=T%C3%ADtulo&level=1");
        string nested = await client.GetStringAsync("/teste/corpo?title=T%C3%ADtulo&level=3&description=%3Cimg%20src%3Dx%3E");

        StringAssert.Contains(asPage, "<h1 ");
        StringAssert.Contains(nested, "<h3 ");
        Assert.IsFalse(nested.Contains("<h1", StringComparison.Ordinal), "dentro de outra página o título desce de nível");
        Assert.IsFalse(nested.Contains("<img", StringComparison.Ordinal));
        StringAssert.Contains(nested, "&lt;img src=x&gt;");
        StringAssert.Contains(asPage, "Campinas/SP");
        StringAssert.Matches(asPage, new Regex(@"<dt[^>]*>Ano</dt>\s*<dd[^>]*>2018</dd>"));
    }

    [TestMethod]
    public void Estilos_ComTokens_SemImportant_ELinkadosNosDoisLayouts()
    {
        string css = File.ReadAllText(RepositoryRoot.Wwwroot("css", "components", "card.css"));
        string site = File.ReadAllText(RepositoryRoot.FullPath("src", "GazetaMarketplace.Web", "Views", "Shared", "_Layout.cshtml"));
        string panel = File.ReadAllText(RepositoryRoot.FullPath("src", "GazetaMarketplace.Web", "Views", "Shared", "_PanelLayout.cshtml"));

        StringAssert.Contains(css, "var(--app-media-ratio)");
        StringAssert.Contains(css, "var(--app-focus-outline)");
        Assert.IsFalse(css.Contains("!important", StringComparison.Ordinal));
        Assert.IsFalse(Regex.IsMatch(css, @"#[0-9a-fA-F]{3,8}\b"), "cores só pelos tokens");
        StringAssert.Contains(site, "css/components/card.css");
        StringAssert.Contains(panel, "css/components/card.css");
    }

    [TestMethod]
    public void ModuloDaFotoQueFalha_MontaOBlocoSemInnerHtml_ECarregaEmToda_Pagina()
    {
        string module = File.ReadAllText(RepositoryRoot.Wwwroot("js", "modules", "ad-card.js"));
        string layout = File.ReadAllText(RepositoryRoot.Wwwroot("js", "pages", "layout.js"));

        StringAssert.Contains(module, "createElement");
        StringAssert.Contains(module, "textContent");
        StringAssert.Contains(module, "Foto indisponível");
        StringAssert.Contains(module, "naturalWidth === 0", "a imagem que falhou antes de o módulo rodar também é trocada");
        StringAssert.Contains(layout, "ad-card.js");
        StringAssert.Contains(layout, "watchAdCardImages()");
    }

    [TestMethod]
    public async Task PaginaDeComponentes_Administrador_VeOsQuatroCardsEOCorpo()
    {
        using PanelFixture panel = await StartAsync("Development");

        HttpResponseMessage response = await panel.Admin.GetAsync(ComponentsPage);
        string html = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(4, Regex.Matches(html, @"<article class=""card ad-card").Count);
        Assert.AreEqual(1, Regex.Matches(html, @"<h1\b").Count, "um só h1; o título do corpo desce para h3");
        StringAssert.Contains(html, "data-ad-body");
        Assert.AreEqual(5, Regex.Matches(html, @"data-ad-value=""(preco|tipo|salario)""").Count, "um valor em cada um dos 4 cards + o do corpo");
        Assert.AreEqual(1, Regex.Matches(html, @"data-ad-value=""salario""").Count);
        Assert.AreEqual(1, Regex.Matches(html, @"data-ad-value=""tipo""").Count);
        Assert.AreEqual(2, Regex.Matches(html, @"data-ad-placeholder").Count, "Vagas e o card sem foto");
        Assert.AreEqual(2, Regex.Matches(html, @"<img ").Count, "Carro e Serviço têm capa");
    }

    [TestMethod]
    public async Task PaginaDeComponentes_EmProduction_E404ParaTodos_InclusiveOAdministrador()
    {
        using PanelFixture panel = await StartAsync("Production");
        using HttpClient anonymous = TeamClient.Create(panel.Factory);

        Assert.AreEqual(HttpStatusCode.NotFound, (await panel.Admin.GetAsync(ComponentsPage)).StatusCode, "o Administrador também recebe 404");
        Assert.AreEqual(HttpStatusCode.NotFound, (await anonymous.GetAsync(ComponentsPage)).StatusCode, "sem login: 404, e não o redirecionamento à entrada");
    }

    [TestMethod]
    public async Task PaginaDeComponentes_EmDevelopment_RedatorENaoLogadoNaoEntram()
    {
        using PanelFixture panel = await StartAsync("Development");
        using HttpClient writer = await PanelFixture.SignedInAsync(panel.Factory, PanelFixture.WriterEmail);
        using HttpClient anonymous = TeamClient.Create(panel.Factory);

        HttpResponseMessage asWriter = await writer.GetAsync(ComponentsPage);
        HttpResponseMessage asAnonymous = await anonymous.GetAsync(ComponentsPage);

        Assert.AreEqual(HttpStatusCode.Redirect, asWriter.StatusCode);
        StringAssert.Contains(asWriter.Headers.Location!.OriginalString, "acesso-negado");
        Assert.AreEqual(HttpStatusCode.Redirect, asAnonymous.StatusCode);
        StringAssert.Contains(asAnonymous.Headers.Location!.OriginalString, "/painel/entrar");
    }

    [TestMethod]
    public void PaginaDeComponentes_NaoAparecePeloMenuNemTemRotaPublica()
    {
        string menu = File.ReadAllText(RepositoryRoot.FullPath("src", "GazetaMarketplace.Web", "Navigation", "PanelMenu.cs"));
        string routes = File.ReadAllText(RepositoryRoot.FullPath("src", "GazetaMarketplace.Web", "Navigation", "Routes.cs"));

        Assert.IsFalse(menu.Contains("omponentes", StringComparison.Ordinal));
        Assert.IsFalse(routes.Contains("omponentes", StringComparison.Ordinal));
    }

    private static async Task<PanelFixture> StartAsync(string environment) =>
        await PanelFixture.StartAsync(environment: environment);
}
