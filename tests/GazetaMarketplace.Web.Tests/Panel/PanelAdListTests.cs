using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GazetaMarketplace.Web.Tests.Review.ReviewSupport;

namespace GazetaMarketplace.Web.Tests.Panel;

/// <summary>
/// A lista de anúncios do painel (US-012, S01 a S08) pelas telas. O repositório é o de mentira (<see cref="StubPanelAdListRepository"/>): devolve páginas prontas e guarda a
/// consulta recebida, então aqui se prova o que o serviço <b>pede</b> (autoria, arquivados, termo normalizado, página) e o que a tela <b>mostra</b>; o filtro em si, no T-SQL de
/// verdade, é provado na integração (SQL Server).
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PanelAdListTests
#pragma warning restore CA1515
{
    private const string ListUrl = "/painel/anuncios";
    private const int Cars = 33;
    private const int Books = 86;

    private static StubPanelAdListRepository Stub(DraftSite site) => site.Harness.Factory.Services.GetRequiredService<StubPanelAdListRepository>();

    private static string Text(string html) => WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(html, @"<[^>]+>", " "), @"\s+", " ")).Trim();

    private static PanelAdListRow Row(int id, string title, int authorId = 2, string author = "Ana Souza", int? category = Cars, byte status = AdStatus.Draft, int day = 29, string reason = null) =>
        new(id, title, authorId, author, category, status, Day(day), reason);

    private static async Task<string> GetAsync(HttpClient client, string url) => await DraftSite.BodyAsync(await client.GetAsync(url));

    private static int RowCount(string html) => Regex.Matches(html, @"<tr data-ad-id=").Count;

    [TestMethod]
    public async Task US012S01_Redator_VeOsProprios_ComTituloCategoriaSituacaoEData_SemColunaAutor()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int ana = await site.UserIdAsync(Ana);
        Stub(site).Rows.AddRange([
            Row(1, "Moto para retirar peças", ana, status: AdStatus.Rejected, day: 30, reason: "Fotos escuras"),
            Row(2, "Honda Civic 2018", ana, status: AdStatus.InReview),
            Row(3, "Terreno 450 m²", ana, category: null),
            Row(4, "Casa com quintal", ana),
            Row(5, "Jaqueta jeans masculina", ana, category: Books, status: AdStatus.Published, day: 20)]);

        string html = await GetAsync(site.Writer, ListUrl);

        Assert.AreEqual(ana, Stub(site).LastQuery.AuthorId, "o serviço pediu só os anúncios do próprio Redator");
        StringAssert.Matches(html, new Regex(@"<h1[^>]*>Meus anúncios</h1>"));
        Assert.AreEqual(5, RowCount(html));
        Assert.AreEqual("5 anúncios", Text(Regex.Match(html, @"<p role=""status"" data-ads-total>[\s\S]*?</p>").Value));
        Assert.IsFalse(Regex.IsMatch(html, @"<th scope=""col"">Autor</th>"), "o Redator não vê a coluna do autor");
        foreach (string column in new[] { "Título", "Categoria", "Situação", "Alterado em" })
        {
            StringAssert.Contains(html, $"<th scope=\"col\">{column}</th>");
        }

        StringAssert.Matches(html, new Regex(@">Jaqueta jeans masculina</a>\s*</th>\s*<td[^>]*>Livros e revistas</td>\s*<td[^>]*>Publicado</td>\s*<td[^>]*><time[^>]*>20/09/2026</time>"));
        StringAssert.Matches(html, new Regex(@">Honda Civic 2018</a>\s*</th>\s*<td[^>]*>Carros, vans e utilitários</td>\s*<td[^>]*>Em revisão</td>"));
        Assert.IsFalse(html.Contains("Esta página é provisória", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task US012S02_Administrador_VeTodos_ComAutor_EAsAbasDaFila()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).InReviewCount = 3;
        Stub(site).Rows.AddRange([Row(1, "Honda Civic 2018", 2, "Ana Souza"), Row(2, "Casa com quintal", 3, "Bruno Lima")]);

        string html = await GetAsync(site.Admin, ListUrl);

        Assert.IsNull(Stub(site).LastQuery.AuthorId, "o Administrador não filtra por autor");
        StringAssert.Matches(html, new Regex(@"<h1[^>]*>Anúncios</h1>"));
        StringAssert.Contains(html, "<th scope=\"col\">Autor</th>");
        StringAssert.Matches(html, new Regex(@">Honda Civic 2018</a>\s*</th>\s*<td[^>]*>Ana Souza</td>"));
        StringAssert.Matches(html, new Regex(@">Casa com quintal</a>\s*</th>\s*<td[^>]*>Bruno Lima</td>"));
        StringAssert.Matches(html, new Regex(@"href=""/painel/anuncios/fila""[^>]*>Fila de revisão \(3\)</a>"));
        StringAssert.Matches(html, new Regex(@"<a class=""nav-link alvo-toque active"" href=""/painel/anuncios"" aria-current=""page"">Todos os anúncios</a>"));
    }

    [TestMethod]
    public async Task Redator_NaoVeAsAbasDoAdministrador()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1, "Honda Civic 2018"));

        string html = await GetAsync(site.Writer, ListUrl);

        Assert.IsFalse(html.Contains("Fila de revisão", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task US012S03_FiltrarPorSituacao_PedeASituacao_EOSelectMostraAEscolhida()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1, "Moto para retirar peças", status: AdStatus.Rejected, reason: "Fotos escuras"));

        string rejected = await GetAsync(site.Admin, ListUrl + "?situacao=rejeitado");
        PanelAdListQuery rejectedQuery = Stub(site).LastQuery;
        string archived = await GetAsync(site.Admin, ListUrl + "?situacao=arquivado");
        PanelAdListQuery archivedQuery = Stub(site).LastQuery;
        await GetAsync(site.Admin, ListUrl + "?situacao=invalida");
        PanelAdListQuery invalidQuery = Stub(site).LastQuery;
        await GetAsync(site.Admin, ListUrl);
        PanelAdListQuery plainQuery = Stub(site).LastQuery;

        Assert.AreEqual(AdStatus.Rejected, rejectedQuery.Status);
        Assert.IsTrue(rejectedQuery.ExcludeArchived);
        StringAssert.Contains(rejected, "<option value=\"rejeitado\" selected=\"selected\">Rejeitado</option>");
        StringAssert.Contains(Text(rejected), "1 anúncio");
        Assert.AreEqual(AdStatus.Archived, archivedQuery.Status);
        Assert.IsFalse(archivedQuery.ExcludeArchived, "só filtrando por Arquivado os arquivados aparecem");
        StringAssert.Contains(archived, "<option value=\"arquivado\" selected=\"selected\">Arquivado</option>");
        Assert.IsNull(invalidQuery.Status, "situação desconhecida não filtra");
        Assert.IsTrue(invalidQuery.ExcludeArchived);
        Assert.IsNull(plainQuery.Status);
        Assert.IsTrue(plainQuery.ExcludeArchived, "a lista padrão esconde os arquivados");
        string options = Text(Regex.Match(rejected, @"<select[\s\S]*?</select>").Value);
        Assert.AreEqual("Todas (exceto arquivados) Rascunho Em revisão Publicado Rejeitado Arquivado", options);
    }

    [TestMethod]
    public async Task US012S04_Buscar_NormalizaOTermo_LimitaA100_EDevolveOQueFoiDigitadoAoCampo()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1, "Honda Civic 2018"));

        string html = await GetAsync(site.Writer, ListUrl + "?q=%20%20CIVIC%20%20");
        string civic = Stub(site).LastQuery.Search;
        await GetAsync(site.Writer, ListUrl + "?q=Pr%C3%A9-venda%20%20Ca%C3%A7%C3%A3o");
        string accents = Stub(site).LastQuery.Search;
        await GetAsync(site.Writer, ListUrl + "?q=" + new string('a', 150));
        string longTerm = Stub(site).LastQuery.Search;
        await GetAsync(site.Writer, ListUrl + "?q=100%25_%5Bx");
        string wildcards = Stub(site).LastQuery.Search;

        Assert.AreEqual("civic", civic);
        StringAssert.Contains(html, "name=\"q\" value=\"CIVIC\"");
        Assert.AreEqual("pre-venda cacao", accents);
        Assert.AreEqual(100, longTerm.Length);
        Assert.AreEqual("100%_[x", wildcards, "curingas do SQL viram texto: o repositório usa CHARINDEX, sem LIKE");
        StringAssert.Contains(html, "type=\"search\"");
        StringAssert.Contains(html, "<label class=\"form-label\" for=\"busca\">Buscar por título</label>");
    }

    [TestMethod]
    public async Task US012S05_AbrirUmAnuncioDaLista_CadaSituacaoLevaAoDestinoCerto()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange([
            Row(11, "Rascunho A"), Row(12, "Rejeitado A", status: AdStatus.Rejected, reason: "x"), Row(13, "Revisão A", status: AdStatus.InReview),
            Row(14, "Publicado A", status: AdStatus.Published), Row(15, "Arquivado A", status: AdStatus.Archived)]);

        string admin = await GetAsync(site.Admin, ListUrl);
        string writer = await GetAsync(site.Writer, ListUrl);

        foreach ((string title, string adminHref, string writerHref) in new[]
        {
            ("Rascunho A", "/painel/anuncios/11/editar", "/painel/anuncios/11/editar"),
            ("Rejeitado A", "/painel/anuncios/12/editar", "/painel/anuncios/12/editar"),
            ("Revisão A", "/painel/anuncios/13/pre-visualizacao", "/painel/anuncios/13/editar"),
            ("Publicado A", "/painel/anuncios/14/editar", "/painel/anuncios/14/editar"),
            ("Arquivado A", "/painel/anuncios/15/editar", "/painel/anuncios/15/editar")
        })
        {
            StringAssert.Matches(admin, new Regex($@"<a [^>]*href=""{adminHref}""[^>]*>{title}</a>"), "Administrador: " + title);
            StringAssert.Matches(writer, new Regex($@"<a [^>]*href=""{writerHref}""[^>]*>{title}</a>"), "Redator: " + title);
        }
    }

    [TestMethod]
    public async Task US012S06_RedatorSemAnuncios_VeAMensagemEOBotaoNovoAnuncio()
    {
        using DraftSite site = await DraftSite.StartAsync();

        string writer = await GetAsync(site.Writer, ListUrl);
        string admin = await GetAsync(site.Admin, ListUrl);

        StringAssert.Contains(Text(writer), "Meus anúncios Você ainda não criou anúncios Novo anúncio");
        StringAssert.Matches(writer, new Regex(@"<a [^>]*href=""/painel/anuncios/novo""[^>]*>Novo anúncio</a>"));
        Assert.AreEqual(1, Regex.Matches(writer, @">Novo anúncio</a>").Count, "o botão aparece uma vez só");
        Assert.IsFalse(writer.Contains("<table", StringComparison.Ordinal));
        Assert.IsFalse(writer.Contains("data-ads-filters", StringComparison.Ordinal), "sem anúncios não há filtros");
        StringAssert.Contains(Text(admin), "Nenhum anúncio cadastrado ainda");
    }

    [TestMethod]
    public async Task SemResultado_BuscaOuFiltro_MostraAMensagemELimparFiltros_MantendoOFormulario()
    {
        using DraftSite site = await DraftSite.StartAsync();

        string html = await GetAsync(site.Writer, ListUrl + "?q=zzz&situacao=publicado");

        StringAssert.Contains(Text(html), "Nenhum anúncio encontrado");
        StringAssert.Matches(html, new Regex(@"<a [^>]*href=""/painel/anuncios""[^>]*>Limpar filtros</a>"));
        StringAssert.Contains(html, "name=\"q\" value=\"zzz\"");
        StringAssert.Contains(html, "data-ads-filters");
        Assert.IsFalse(html.Contains("<table", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task US012S07_ListaComMaisDe20_20PorPagina_TresPaginas_ComLinksQuePreservamOsFiltros()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 45).Select(i => Row(i, $"Anúncio {i:00}", status: AdStatus.Published)));

        string first = await GetAsync(site.Writer, ListUrl);
        string third = await GetAsync(site.Writer, ListUrl + "?pagina=3");
        string filtered = await GetAsync(site.Writer, ListUrl + "?q=an%C3%BAncio&situacao=publicado&pagina=2");

        Assert.AreEqual(20, RowCount(first));
        StringAssert.Contains(Text(first), "45 anúncios");
        StringAssert.Matches(first, new Regex(@"<nav aria-label=""Paginação"""));
        foreach (string number in new[] { "1", "2", "3" })
        {
            StringAssert.Matches(first, new Regex($@"<a [^>]*aria-label=""Página {number}[^""]*""[^>]*>{number}</a>"));
        }

        StringAssert.Matches(first, new Regex(@"<a [^>]*href=""/painel/anuncios""[^>]*aria-current=""page""[^>]*>1</a>"));
        StringAssert.Matches(first, new Regex(@"<a [^>]*href=""/painel/anuncios\?pagina=2""[^>]*>Próxima</a>"));
        StringAssert.Contains(first, "aria-disabled=\"true\">Anterior</span>");
        Assert.AreEqual(5, RowCount(third));
        StringAssert.Contains(third, "Anúncio 41");
        StringAssert.Matches(third, new Regex(@"<a [^>]*aria-current=""page""[^>]*>3</a>"));
        StringAssert.Contains(third, "aria-disabled=\"true\">Próxima</span>");
        Assert.AreEqual(20, Stub(site).Queries[0].PageSize);
        // Os links levam a busca e a situação junto (a página decodifica o &amp; do HTML)
        StringAssert.Matches(filtered, new Regex(@"<a [^>]*href=""/painel/anuncios\?q=an%C3%BAncio&situacao=publicado&pagina=3""[^>]*>Próxima</a>"));
        StringAssert.Matches(filtered, new Regex(@"<a [^>]*href=""/painel/anuncios\?q=an%C3%BAncio&situacao=publicado""[^>]*>Anterior</a>"));
    }

    [TestMethod]
    public async Task Paginacao_PaginaForaDoIntervaloOuInvalida_VoltaParaUmaPaginaValida()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 45).Select(i => Row(i, $"Anúncio {i:00}")));

        string beyond = await GetAsync(site.Writer, ListUrl + "?pagina=99");
        List<PanelAdListQuery> beyondQueries = [.. Stub(site).Queries];
        Stub(site).Queries.Clear();
        string zero = await GetAsync(site.Writer, ListUrl + "?pagina=0");
        string negative = await GetAsync(site.Writer, ListUrl + "?pagina=-5");
        string text = await GetAsync(site.Writer, ListUrl + "?pagina=abc");

        Assert.AreEqual(5, RowCount(beyond), "a página 99 mostra a última (a 3)");
        CollectionAssert.AreEqual(new[] { 99, 3 }, beyondQueries.Select(q => q.Page).ToArray());
        foreach (string page in new[] { zero, negative, text })
        {
            Assert.AreEqual(20, RowCount(page), "página inválida mostra a primeira");
        }

        Assert.IsTrue(Stub(site).Queries.All(q => q.Page == 1));
    }

    [TestMethod]
    public async Task Paginacao_PaginaGigante_FicaNoLimiteEMostraAUltimaPagina_SemErro503()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 45).Select(i => Row(i, $"Anúncio {i:00}")));

        HttpResponseMessage response = await site.Writer.GetAsync(ListUrl + "?pagina=2147483647");
        string html = await DraftSite.BodyAsync(response);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(5, RowCount(html), "a última página (a 3) tem 5 anúncios");
        Assert.AreEqual(PanelAdListFilters.MaxPage, Stub(site).Queries[0].Page, "o pedido ao banco usa o limite, não o número digitado");
        Assert.IsTrue(Stub(site).Queries.All(q => q.Page <= PanelAdListFilters.MaxPage));
    }

    [TestMethod]
    public async Task US012S08_FalhaAoCarregar_Da503ComAMensagemETentarNovamente_SemDetalheTecnico()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Failure = new InvalidOperationException("segredo-do-banco: Server=db.interno;Password=abc");

        HttpResponseMessage response = await site.Writer.GetAsync(ListUrl + "?situacao=rejeitado&q=civic");
        string html = await DraftSite.BodyAsync(response);

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        StringAssert.Contains(Text(html), "Não foi possível carregar os anúncios. Tente novamente.");
        StringAssert.Matches(html, new Regex(@"<a [^>]*href=""/painel/anuncios\?situacao=rejeitado&q=civic""[^>]*>Tentar novamente</a>"));
        StringAssert.Contains(html, "Código de referência");
        foreach (string leak in new[] { "segredo-do-banco", "db.interno", "InvalidOperationException", "   at " })
        {
            Assert.IsFalse(html.Contains(leak, StringComparison.Ordinal), "sem detalhe técnico: " + leak);
        }

        Stub(site).Failure = null;
        Assert.AreEqual(HttpStatusCode.OK, (await site.Writer.GetAsync(ListUrl)).StatusCode, "a nova tentativa funciona");
    }

    [TestMethod]
    public async Task Rejeitado_MostraOMotivoInteiroNaLinha_OsDemaisNao_ETudoSaiCodificado()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string longReason = string.Join(' ', Enumerable.Repeat("fotos escuras", 38))[..500];
        Stub(site).Rows.AddRange([
            Row(1, "<script>alert(1)</script> Moto", status: AdStatus.Rejected, reason: longReason),
            Row(2, "Rejeitado com HTML", status: AdStatus.Rejected, reason: "<b>negrito</b> & \"aspas\""),
            Row(3, "Publicado com motivo antigo", status: AdStatus.Published, reason: "motivo que não deve aparecer")]);

        string raw = await site.Writer.GetStringAsync(ListUrl);
        string html = WebUtility.HtmlDecode(raw);

        StringAssert.Contains(html, "Motivo da rejeição: " + longReason);
        Assert.AreEqual(2, Regex.Matches(html, @"data-ad-rejection").Count, "só as linhas Rejeitado mostram o motivo");
        Assert.IsFalse(html.Contains("motivo que não deve aparecer", StringComparison.Ordinal));
        Assert.IsFalse(raw.Contains("<script>alert(1)", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(raw.Contains("<b>negrito", StringComparison.Ordinal));
        StringAssert.Contains(raw, "&lt;script&gt;alert(1)&lt;/script&gt; Moto");
    }

    [TestMethod]
    public async Task ParametrosForaDaLista_AutorEOrdenacao_SaoIgnorados()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int ana = await site.UserIdAsync(Ana);
        Stub(site).Rows.Add(Row(1, "Honda Civic 2018", ana));

        await GetAsync(site.Writer, ListUrl);
        PanelAdListQuery plain = Stub(site).LastQuery;
        await GetAsync(site.Writer, ListUrl + "?autor=1&authorId=1&AuthorId=1&ordenar=Title&ordem=desc&sort=Title%3B%20DROP%20TABLE%20Ads&order=desc&pageSize=100&PageSize=1");
        PanelAdListQuery tampered = Stub(site).LastQuery;

        Assert.AreEqual(plain, tampered, "nada fora de q, situacao e pagina muda a consulta");
        Assert.AreEqual(ana, tampered.AuthorId, "o Redator continua só com os próprios anúncios");
        Assert.AreEqual(20, tampered.PageSize);
    }

    [TestMethod]
    public async Task AlteradoEm_DataNoFusoDeSaoPaulo_ComAHoraNoTitle()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1, "Honda Civic 2018", day: 30));

        string html = await GetAsync(site.Writer, ListUrl);

        StringAssert.Matches(html, new Regex(@"<time datetime=""2026-09-30T13:00:00Z"" title=""30/09/2026 10:00"">30/09/2026</time>"));
    }

    [TestMethod]
    public async Task SemLogin_VaiParaAEntrada_ERedatorEAdministradorTemOMesmoEndereco()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient anonymous = site.Harness.Anonymous();

        HttpResponseMessage response = await anonymous.GetAsync(ListUrl);

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        StringAssert.StartsWith(response.Destination(), "/painel/entrar");
    }
}
