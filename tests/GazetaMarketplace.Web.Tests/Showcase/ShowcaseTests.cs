using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Showcase;

/// <summary>
/// A página inicial e as páginas de categoria (US-001, S01 a S08; tarefa 5.1). A consulta real (só publicados, ordem, capa, descendentes) é provada no SQL Server, na integração;
/// aqui o repositório é o de mentira, que devolve o que o teste põe e registra o que o serviço pediu.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ShowcaseTests
#pragma warning restore CA1515
{
    private const int Automobiles = 2;
    private const int Motorcycles = 36;
    private const int Cars = 33;
    private const int Parts = 3;
    private const int CarParts = 38;

    private static string Text(string html) => WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(html, @"<[^>]+>", " "), @"\s+", " ")).Trim();

    private static StubShowcaseRepository Stub(DraftSite site) => site.Harness.Factory.Services.GetRequiredService<StubShowcaseRepository>();

    private static async Task<CategoryTreeSnapshot> TreeAsync(DraftSite site) => await site.Harness.Factory.Services.GetRequiredService<ICategoryTree>().GetAsync(CancellationToken.None);

    private static async Task<string> SlugAsync(DraftSite site, int id) => (await TreeAsync(site)).Find(id).Slug;

    private static async Task<(HttpStatusCode Status, string Html)> GetAsync(DraftSite site, string path)
    {
        using HttpClient visitor = site.Harness.Anonymous();
        HttpResponseMessage response = await visitor.GetAsync(path);
        return (response.StatusCode, await DraftSite.BodyAsync(response));
    }

    private static ShowcaseRow Row(int id, string title = null, int? categoryId = Cars, long? price = 6_200_000, string city = "Campinas", string uf = "SP", bool cover = true, string attributes = "{}") =>
        new(id, title ?? $"Anúncio {id:00}", categoryId, price, attributes, city, uf, cover ? id * 10 : null, cover ? 1600 : null, cover ? 1200 : null);

    private static int CardCount(string html) => Regex.Matches(html, @"<article class=""card ad-card[^""]*"" data-ad-card>").Count;

    private static string[] TileNames(string html) =>
        [.. Regex.Matches(html, @"<a [^>]*data-category-tile[^>]*>[\s\S]*?</a>").Select(m => Text(m.Value))];

    [TestMethod]
    public async Task US001S01_PaginaInicialMostraCategoriasEAnunciosRecentes()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 15).Select(i => Row(i, categoryId: i % 2 == 0 ? Cars : 96, price: i == 3 ? null : 6_200_000)));
        CategoryTreeSnapshot tree = await TreeAsync(site);

        (HttpStatusCode status, string html) = await GetAsync(site, "/");

        Assert.AreEqual(HttpStatusCode.OK, status);
        CollectionAssert.AreEqual(tree.Roots.Select(r => r.Name).ToArray(), TileNames(html), "as categorias principais, cada uma com seu nome, na ordem da árvore");
        foreach (CategoryNode root in tree.Roots)
        {
            StringAssert.Contains(html, $"href=\"/categoria/{root.Slug}\"");
        }

        CollectionAssert.AreEqual(new[] { ShowcaseFilters.RecentCount }, Stub(site).RecentRequests.ToArray(), "o serviço pede 12");
        Assert.AreEqual(12, CardCount(html), "12 anúncios, mesmo com 15 publicados");
        StringAssert.Contains(html, "Anúncios mais recentes");
        StringAssert.Matches(html, new Regex(@"<form[^>]*role=""search""[^>]*action=""/busca"""), "a caixa de busca no topo");
        StringAssert.Contains(html, "src=\"/fotos/2/20-480.webp\"", "a capa do card é a miniatura de 480 px");
        StringAssert.Contains(Text(html), "R$ 62.000");
        StringAssert.Contains(Text(html), "Campinas/SP");
        Assert.IsFalse(Text(html).Contains("Em breve teremos novos anúncios", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task US001S01_Cards_TemTituloComLinkParaODetalhe_VarianteDeServicosEDeVagas()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(
        [
            Row(1, "Honda Civic 2018"),
            Row(2, "Diarista com experiência", categoryId: 66, price: null, attributes: """{"serviceTypeId":1}"""),
            Row(3, "Pizzaiolo com experiência", categoryId: 96, price: 280_000, cover: false, attributes: """{"jobAreaIds":[2,1]}""")
        ]);

        (_, string html) = await GetAsync(site, "/");
        string visible = Text(html);

        StringAssert.Matches(html, new Regex(@"<a class=""stretched-link ad-card__link"" href=""/anuncio/1/honda-civic-2018"""));
        StringAssert.Matches(html, new Regex(@"href=""/anuncio/3/pizzaiolo-com-experiencia"""));
        StringAssert.Contains(html, "data-ad-value=\"tipo\"", "Serviços mostram o tipo no lugar do preço");
        StringAssert.Contains(visible, FieldLists.ServiceType.Find(1).Label);
        StringAssert.Contains(html, "data-ad-value=\"salario\"", "Vagas mostram o salário");
        StringAssert.Contains(visible, "Salário R$ 2.800");
        StringAssert.Contains(visible, FieldLists.JobArea.Find(2).Label, "a primeira área marcada no bloco no lugar da foto");
        Assert.AreEqual(2, Regex.Matches(html, @"<img[^>]*data-ad-card-image").Count, "a vaga não tem foto");
    }

    [TestMethod]
    public async Task US001S01_AsPrimeirasImagensCarregamJa_AsDemaisSoQuandoChegamPertoDaTela()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 12).Select(i => Row(i)));

        (_, string html) = await GetAsync(site, "/");
        string[] loading = [.. Regex.Matches(html, @"<img[^>]*data-ad-card-image[^>]*loading=""(\w+)""|<img[^>]*loading=""(\w+)""[^>]*data-ad-card-image").Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)];

        CollectionAssert.AreEqual(new[] { "eager", "eager", "eager", "eager", "lazy", "lazy", "lazy", "lazy", "lazy", "lazy", "lazy", "lazy" }, loading, "só a primeira linha (4 colunas) é urgente");
    }

    [TestMethod]
    public async Task US001S02_EntrarEmUmaCategoriaPrincipal_MostraSubcategoriasEPedeOsAnunciosDeTodasAsDescendentes()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 3).Select(i => Row(i, categoryId: i == 2 ? Motorcycles : Cars)));
        CategoryTreeSnapshot tree = await TreeAsync(site);

        (HttpStatusCode status, string html) = await GetAsync(site, "/categoria/" + await SlugAsync(site, Automobiles));

        Assert.AreEqual(HttpStatusCode.OK, status);
        StringAssert.Contains(Text(html), "Automóveis, Peças e Acessórios");
        string subcategories = Regex.Match(html, @"<nav aria-label=""Subcategorias""[\s\S]*?</nav>").Value;
        CollectionAssert.AreEqual(
            new[] { "Carros, vans e utilitários", "Motos", "Ônibus", "Caminhões", "Barcos e aeronaves", "Autopeças" },
            Regex.Matches(subcategories, @"<a [^>]*>([^<]+)</a>").Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).ToArray(),
            "as subcategorias na ordem da árvore");
        (int[] ids, int page, int size) = Stub(site).CategoryRequests.Single();
        CollectionAssert.AreEquivalent(new[] { Automobiles }.Concat(tree.DescendantsOf(Automobiles).Select(c => c.Id)).ToArray(), ids, "a categoria e todas as descendentes, em todos os níveis");
        CollectionAssert.Contains(ids, CarParts, "inclui as netas (Peças para carros)");
        Assert.AreEqual((1, 24), (page, size));
        Assert.AreEqual(3, CardCount(html));
    }

    [TestMethod]
    public async Task US001S03_EntrarEmUmaSubcategoria_SoAsDela_CaminhoDeNavegacao_EVoltar()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1, "Moto CG 160", Motorcycles));
        string parentUrl = "/categoria/" + await SlugAsync(site, Automobiles);

        (_, string parent) = await GetAsync(site, parentUrl);
        string motosHref = Regex.Match(parent, @"<a [^>]*href=""([^""]+)""[^>]*>Motos</a>").Groups[1].Value;
        Stub(site).CategoryRequests.Clear();
        (HttpStatusCode status, string motos) = await GetAsync(site, motosHref);

        Assert.AreEqual(HttpStatusCode.OK, status);
        CollectionAssert.AreEqual(new[] { Motorcycles }, Stub(site).CategoryRequests.Single().CategoryIds, "só a subcategoria, sem a mãe nem as irmãs");
        string breadcrumb = Regex.Match(motos, @"<nav aria-label=""Caminho de navegação""[\s\S]*?</nav>").Value;
        CollectionAssert.AreEqual(new[] { "Início", "Automóveis, Peças e Acessórios", "Motos" }, Regex.Matches(breadcrumb, @"<li class=""breadcrumb-item[^>]*>([\s\S]*?)</li>").Select(m => Text(m.Groups[1].Value)).ToArray());
        StringAssert.Contains(breadcrumb, "href=\"/\"");
        StringAssert.Contains(breadcrumb, $"href=\"{parentUrl}\"");
        StringAssert.Matches(breadcrumb, new Regex(@"<li class=""breadcrumb-item active"" aria-current=""page"">Motos</li>"), "o passo atual não é link");
        Assert.IsFalse(motos.Contains("aria-label=\"Subcategorias\"", StringComparison.Ordinal), "Motos não tem subcategorias: a linha some");

        string backHref = Regex.Match(breadcrumb, @"<a href=""([^""]+)"">Automóveis, Peças e Acessórios</a>").Groups[1].Value;
        Stub(site).CategoryRequests.Clear();
        (HttpStatusCode backStatus, string back) = await GetAsync(site, backHref);

        Assert.AreEqual(HttpStatusCode.OK, backStatus);
        Assert.IsTrue(Stub(site).CategoryRequests.Single().CategoryIds.Length > 1, "voltando à mãe, os anúncios de todas as subcategorias");
        StringAssert.Contains(Text(back), "Subcategorias");
    }

    [TestMethod]
    public async Task US001S03_CaminhoDeTresNiveis_VaiDaCategoriaPrincipalAteAPropria()
    {
        using DraftSite site = await DraftSite.StartAsync();

        (_, string html) = await GetAsync(site, "/categoria/" + await SlugAsync(site, CarParts));

        string breadcrumb = Regex.Match(html, @"<nav aria-label=""Caminho de navegação""[\s\S]*?</nav>").Value;
        CollectionAssert.AreEqual(
            new[] { "Início", "Automóveis, Peças e Acessórios", "Autopeças", "Peças para carros, vans e utilitários" },
            Regex.Matches(breadcrumb, @"<li class=""breadcrumb-item[^>]*>([\s\S]*?)</li>").Select(m => Text(m.Groups[1].Value)).ToArray());
        Assert.AreEqual(Parts, (await TreeAsync(site)).Find(CarParts).ParentId);
    }

    [TestMethod]
    public async Task US001S04_CategoriaSemAnunciosPublicados_DizEOfereceAsDemaisCategoriasPrincipais()
    {
        using DraftSite site = await DraftSite.StartAsync();
        CategoryTreeSnapshot tree = await TreeAsync(site);

        (HttpStatusCode status, string html) = await GetAsync(site, "/categoria/" + await SlugAsync(site, 66));

        Assert.AreEqual(HttpStatusCode.OK, status, "a categoria existe: não é 404");
        StringAssert.Contains(Text(html), "Ainda não há anúncios nesta categoria");
        Assert.AreEqual(0, CardCount(html));
        CollectionAssert.AreEqual(tree.Roots.Select(r => r.Name).ToArray(), TileNames(html), "links para as categorias principais");
        Assert.IsFalse(html.Contains("data-pagination", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task US001S05_SiteSemNenhumAnuncioPublicado_MostraCategoriasEAMensagemNoLugarDaLista()
    {
        using DraftSite site = await DraftSite.StartAsync();
        CategoryTreeSnapshot tree = await TreeAsync(site);

        (HttpStatusCode status, string html) = await GetAsync(site, "/");

        Assert.AreEqual(HttpStatusCode.OK, status);
        CollectionAssert.AreEqual(tree.Roots.Select(r => r.Name).ToArray(), TileNames(html));
        StringAssert.Contains(Text(html), "Em breve teremos novos anúncios");
        Assert.AreEqual(0, CardCount(html));
        Assert.IsFalse(html.Contains("data-ad-grid", StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow("/")]
    [DataRow("/categoria/{slug}")]
    public async Task US001S06_FalhaAoCarregar_Da503ComAMensagem_TentarNovamente_ECodigo_SemDetalheTecnico(string path)
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Failure = new InvalidOperationException("segredo-do-banco: Server=db.interno;Password=abc");
        string url = path.Replace("{slug}", await SlugAsync(site, Automobiles), StringComparison.Ordinal);

        (HttpStatusCode status, string html) = await GetAsync(site, url + (url == "/" ? string.Empty : "?pagina=2"));

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, status);
        StringAssert.Contains(Text(html), "Não foi possível carregar a página. Tente novamente.");
        StringAssert.Matches(html, new Regex(@"<a [^>]*href=""" + Regex.Escape(url == "/" ? "/" : url + "?pagina=2") + @"""[^>]*>Tentar novamente</a>"));
        StringAssert.Contains(html, "Código de referência");
        foreach (string leak in new[] { "segredo-do-banco", "db.interno", "InvalidOperationException", "   at " })
        {
            Assert.IsFalse(html.Contains(leak, StringComparison.Ordinal), "sem detalhe técnico: " + leak);
        }

        Assert.IsTrue(site.Harness.Factory.Logs.Events.Any(e => e.Exception?.Message.Contains("segredo-do-banco", StringComparison.Ordinal) == true), "a causa fica no log");
        Stub(site).Failure = null;
        Assert.AreEqual(HttpStatusCode.OK, (await GetAsync(site, url)).Status, "voltou ao normal, o botão funciona");
    }

    [TestMethod]
    public async Task US001S07_EnderecoDeCategoriaQueNaoExiste_Da404ComAMensagemEOsCaminhosDeVolta()
    {
        using DraftSite site = await DraftSite.StartAsync();
        CategoryTreeSnapshot tree = await TreeAsync(site);

        (HttpStatusCode status, string html) = await GetAsync(site, "/categoria/barcos-e-aeronaves-antigo");

        Assert.AreEqual(HttpStatusCode.NotFound, status);
        StringAssert.Contains(Text(html), "Categoria não encontrada");
        StringAssert.Matches(html, new Regex(@"<a [^>]*href=""/""[^>]*>Ir para a página inicial</a>"));
        CollectionAssert.AreEqual(tree.Roots.Select(r => r.Name).ToArray(), TileNames(html), "links para as categorias principais");
        Assert.IsEmpty(Stub(site).CategoryRequests, "slug que não existe não consulta anúncios");
    }

    [TestMethod]
    public async Task US001S07_CategoriaExcluida_PassaADar404_SemRedirecionar()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string slug = await SlugAsync(site, 40);
        Assert.AreEqual(HttpStatusCode.OK, (await GetAsync(site, "/categoria/" + slug)).Status);

        await site.Harness.WithDbAsync(async db =>
        {
            db.Categories.Remove(db.Categories.Single(c => c.Id == 40));
            await db.SaveChangesAsync();
            return 0;
        });
        site.Harness.Factory.Services.GetRequiredService<ICategoryTree>().Invalidate();

        (HttpStatusCode status, string html) = await GetAsync(site, "/categoria/" + slug);

        Assert.AreEqual(HttpStatusCode.NotFound, status);
        StringAssert.Contains(Text(html), "Categoria não encontrada");
    }

    [TestMethod]
    [DataRow("/categoria/ABC")]
    [DataRow("/categoria/a%20b")]
    [DataRow("/categoria/%3Cscript%3Ealert(1)%3C%2Fscript%3E")]
    [DataRow("/categoria/..%2F..%2Fpainel")]
    public async Task US001S07_SlugEstranho_Da404_SemErroEmNadaDoTextoDigitado(string path)
    {
        using DraftSite site = await DraftSite.StartAsync();

        (HttpStatusCode status, string html) = await GetAsync(site, path);

        Assert.AreEqual(HttpStatusCode.NotFound, status);
        Assert.IsFalse(html.Contains("<script>alert", StringComparison.Ordinal), "o texto digitado não volta na página");
    }

    [TestMethod]
    public async Task US001S07_SlugGigante_Da404_SemConsultarNada()
    {
        using DraftSite site = await DraftSite.StartAsync();

        (HttpStatusCode status, _) = await GetAsync(site, "/categoria/" + new string('a', 5_000));

        Assert.AreEqual(HttpStatusCode.NotFound, status);
        Assert.IsEmpty(Stub(site).CategoryRequests);
    }

    [TestMethod]
    public async Task US001S08_PaginaInicialEmTelaEstreita_GradeDeDuasColunas_SemLarguraFixa()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 12).Select(i => Row(i)));

        (_, string html) = await GetAsync(site, "/");

        StringAssert.Contains(html, "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"");
        Assert.AreEqual(2, Regex.Matches(html, @"class=""row row-cols-2 row-cols-md-3 row-cols-lg-4 g-3 list-unstyled mb-0""").Count, "categorias e anúncios: 2 colunas no celular, 3 e 4 nas telas maiores");
        Assert.IsFalse(Regex.IsMatch(html, @"style=""[^""]*width\s*:\s*\d+px"), "nenhuma largura fixa em pixels");
        Assert.IsFalse(html.Contains("<table", StringComparison.Ordinal));
        int search = html.IndexOf("role=\"search\"", StringComparison.Ordinal);
        int categories = html.IndexOf("data-category-grid", StringComparison.Ordinal);
        int ads = html.IndexOf("data-ad-grid", StringComparison.Ordinal);
        Assert.IsTrue(search >= 0 && search < categories && categories < ads, "a busca, as categorias e a lista, nessa ordem, só rolando para baixo");
    }

    [TestMethod]
    public async Task Categoria_24PorPagina_ComPaginacaoQueMantemOEndereco()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 55).Select(i => Row(i)));
        string url = "/categoria/" + await SlugAsync(site, Cars);

        (_, string first) = await GetAsync(site, url);
        (_, string third) = await GetAsync(site, url + "?pagina=3");

        Assert.AreEqual(24, CardCount(first));
        Assert.AreEqual(7, CardCount(third), "55 anúncios: 24 + 24 + 7");
        StringAssert.Contains(first, $"href=\"{url}?pagina=2\"");
        StringAssert.Contains(first, "rel=\"next\"");
        StringAssert.Contains(third, "aria-current=\"page\"");
        Assert.AreEqual((3, 24), (Stub(site).CategoryRequests[^1].Page, Stub(site).CategoryRequests[^1].PageSize));
    }

    [TestMethod]
    public async Task Categoria_PaginaForaDoIntervaloOuInvalida_VoltaParaUmaPaginaValida_SemErro()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 55).Select(i => Row(i)));
        string url = "/categoria/" + await SlugAsync(site, Cars);

        (HttpStatusCode hugeStatus, string huge) = await GetAsync(site, url + "?pagina=2147483647");
        List<(int[] CategoryIds, int Page, int PageSize)> hugeRequests = [.. Stub(site).CategoryRequests];
        Stub(site).CategoryRequests.Clear();
        (_, string beyond) = await GetAsync(site, url + "?pagina=99");
        Stub(site).CategoryRequests.Clear();
        string[] invalid = [(await GetAsync(site, url + "?pagina=0")).Html, (await GetAsync(site, url + "?pagina=-5")).Html, (await GetAsync(site, url + "?pagina=abc")).Html];

        Assert.AreEqual(HttpStatusCode.OK, hugeStatus);
        Assert.AreEqual(ShowcaseFilters.MaxPage, hugeRequests[0].Page, "o pedido ao banco usa o limite, não o número digitado");
        Assert.AreEqual(7, CardCount(huge), "e mostra a última página (a 3)");
        Assert.AreEqual(7, CardCount(beyond));
        foreach (string page in invalid)
        {
            Assert.AreEqual(24, CardCount(page), "página inválida mostra a primeira");
        }

        Assert.IsTrue(Stub(site).CategoryRequests.All(r => r.Page == 1));
    }

    [TestMethod]
    public async Task TextoDoAnuncioEDaCategoria_NuncaViraHtml()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1, "<img src=x onerror=alert(1)>", city: "<b>Cidade</b>"));

        // O HTML como o navegador o recebe (o ajudante dos outros testes decodifica as entidades e esconderia o problema)
        using HttpClient visitor = site.Harness.Anonymous();
        string html = await visitor.GetStringAsync("/");

        Assert.IsFalse(html.Contains("<img src=x", StringComparison.Ordinal), "o título não vira marcação");
        Assert.IsFalse(html.Contains("<b>Cidade</b>", StringComparison.Ordinal), "a cidade não vira marcação");
        StringAssert.Contains(html, "&lt;img src=x onerror=alert(1)&gt;");
    }

    [TestMethod]
    public async Task AsPaginasSaoPublicas_SemLogin_ENaoMostramNadaDoPainel()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1));

        (HttpStatusCode home, string homeHtml) = await GetAsync(site, "/");
        (HttpStatusCode category, string categoryHtml) = await GetAsync(site, "/categoria/" + await SlugAsync(site, Cars));

        Assert.AreEqual(HttpStatusCode.OK, home);
        Assert.AreEqual(HttpStatusCode.OK, category);
        foreach (string html in new[] { homeHtml, categoryHtml })
        {
            StringAssert.Contains(html, "Área da equipe", "o link discreto no rodapé");
            Assert.IsFalse(html.Contains("/painel/anuncios", StringComparison.Ordinal), "nenhum caminho do painel nos anúncios");
        }
    }
}
