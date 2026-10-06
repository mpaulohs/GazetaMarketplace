using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Core.Search;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Web.Models;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Search;

/// <summary>
/// A página de busca (US-002, S01 a S12; tarefa 5.4). A consulta real (só publicados, filtros, ordem, paginação, desempenho) é provada no SQL Server, na integração; aqui o repositório é
/// o de mentira, que devolve o que o teste põe e registra o que o serviço pediu, então estes testes provam o que a <b>tela</b> faz com o endereço: campos preenchidos, mensagens, links e estados.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SearchTests
#pragma warning restore CA1515
{
    private const int Cars = 33;
    private const int Land = 30;
    private const int General = 86;

    private static string Text(string html) => WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(html, @"<(script|style)[\s\S]*?</\1>", " "), @"<[^>]+>", " ")) is { } t ? Regex.Replace(t, @"\s+", " ").Trim() : string.Empty;

    private static StubSearchReadRepository Stub(DraftSite site) => site.Harness.Factory.Services.GetRequiredService<StubSearchReadRepository>();

    private static async Task<string> SlugAsync(DraftSite site, int id) =>
        (await site.Harness.Factory.Services.GetRequiredService<ICategoryTree>().GetAsync(CancellationToken.None)).Find(id).Slug;

    // O HTML do servidor codifica acentos e símbolos (&#xE7;, &amp;): decodificado, os padrões de tag podem usar o texto como o visitante lê
    private static async Task<(HttpStatusCode Status, string Raw)> GetAsync(DraftSite site, string path)
    {
        using HttpClient visitor = site.Harness.Anonymous();
        HttpResponseMessage response = await visitor.GetAsync(path);
        return (response.StatusCode, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }

    private static async Task<string> GetEncodedAsync(DraftSite site, string path)
    {
        using HttpClient visitor = site.Harness.Anonymous();
        return await (await visitor.GetAsync(path)).Content.ReadAsStringAsync();
    }

    private static ShowcaseRow Row(int id, string title = null, int? categoryId = Cars, long? price = 6_200_000, string city = "Campinas", string uf = "SP") =>
        new(id, title ?? $"Anúncio {id:00}", categoryId, price, "{}", city, uf, id * 10, 1600, 1200);

    private static int CardCount(string html) => Regex.Matches(html, @"<article class=""card ad-card[^""]*"" data-ad-card>").Count;

    // O formulário de filtros (o primeiro com data-search-form)
    private static string FiltersForm(string html) => Regex.Match(html, @"<form[^>]*data-search-form[\s\S]*?</form>").Value;

    private static string Field(string html, string name) => Regex.Match(FiltersForm(html), $@"<(input|select)[^>]*name=""{name}""[^>]*>").Value;

    private static string[] Hrefs(string html, string container) =>
        [.. Regex.Matches(Regex.Match(html, container).Value, @"href=""([^""]+)""").Select(m => WebUtility.HtmlDecode(m.Groups[1].Value))];

    [TestMethod]
    public async Task US002S01_BuscarPorTexto_PedeAsPalavrasNormalizadas_MostraOTotalEOsCards()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1, "Honda Civic 2018"));

        (HttpStatusCode status, string html) = await GetAsync(site, "/busca?q=CIVIC");

        Assert.AreEqual(HttpStatusCode.OK, status);
        CollectionAssert.AreEqual(new[] { "civic" }, Stub(site).Last.Words.ToArray());
        StringAssert.Contains(Text(html), "1 anúncio encontrado");
        Assert.AreEqual(1, CardCount(html));
        StringAssert.Contains(Text(html), "Honda Civic 2018");
        StringAssert.Matches(html, new Regex(@"<input[^>]*id=""busca-q""[^>]*value=""CIVIC"""), "a caixa de busca do topo mostra o que foi buscado");
        StringAssert.Contains(html, "<meta name=\"robots\" content=\"noindex, follow\" />");
    }

    [TestMethod]
    public async Task US002S01_TotalNoPlural_ComMilhar()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 3).Select(i => Row(i)));
        Stub(site).Total = 1234;

        (_, string html) = await GetAsync(site, "/busca?q=casa");

        StringAssert.Contains(Text(html), "1.234 anúncios encontrados");
    }

    [TestMethod]
    public async Task US002S02_CombinarCategoriaLocalizacaoEPreco_PedeTudoJuntoEOsCamposVoltamPreenchidos()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string cars = await SlugAsync(site, Cars);
        Stub(site).Rows.Add(Row(1));

        (_, string html) = await GetAsync(site, $"/busca?categoria={cars}&uf=SP&cidade=Campinas&precoMax=50000");

        SearchCriteria criteria = Stub(site).Last;
        Assert.IsTrue(criteria.CategoryIds.Contains(Cars));
        Assert.AreEqual("SP", criteria.Uf);
        Assert.AreEqual("Campinas", criteria.City);
        Assert.AreEqual(5_000_000L, criteria.PriceMaxCents);
        StringAssert.Matches(html, new Regex($@"<option value=""{cars}""[^>]*selected"));
        StringAssert.Matches(html, new Regex(@"<option value=""SP""[^>]*selected"));
        StringAssert.Matches(html, new Regex(@"<option value=""Campinas""[^>]*selected"));
        StringAssert.Contains(Field(html, "precoMax"), "value=\"50000\"");
        StringAssert.Contains(Text(html), "Campinas/SP");
        StringAssert.Contains(Text(html), "R$ 62.000", "o card mostra o preço e a cidade/UF");
    }

    [TestMethod]
    public async Task US002S03_FiltrarPorCaracteristicasDeVeiculo_SoComCategoriaDeVeiculos()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string cars = await SlugAsync(site, Cars);
        string general = await SlugAsync(site, General);
        Stub(site).Rows.Add(Row(1));

        (_, string withCars) = await GetAsync(site, $"/busca?categoria={cars}&marca=1&anoDe=2015&anoAte=2020&kmMax=100000");
        (_, string withGeneral) = await GetAsync(site, $"/busca?categoria={general}");
        (_, string withoutCategory) = await GetAsync(site, "/busca");

        SearchCriteria criteria = Stub(site).Requests[0];
        Assert.AreEqual((1, 2015, 2020, 100_000), (criteria.BrandId, criteria.YearFrom, criteria.YearTo, criteria.KmMax));
        foreach (string block in new[] { "brand", "model", "year", "km" })
        {
            Assert.IsFalse(Regex.IsMatch(withCars, $@"data-filter=""{block}""[^>]*hidden"), $"o bloco {block} aparece na categoria de veículos");
            Assert.IsTrue(Regex.IsMatch(withGeneral, $@"data-filter=""{block}""[^>]*hidden=""hidden"""), $"o bloco {block} não aparece em Produtos em geral");
            Assert.IsTrue(Regex.IsMatch(withoutCategory, $@"data-filter=""{block}""[^>]*hidden=""hidden"""), $"o bloco {block} não aparece sem categoria");
        }

        StringAssert.Matches(withCars, new Regex(@"<option value=""1""[^>]*selected[^>]*>Honda</option>"), "a marca escolhida volta marcada");
        StringAssert.Contains(Field(withCars, "anoDe"), "value=\"2015\"");
        StringAssert.Contains(Field(withCars, "kmMax"), "value=\"100000\"");
        StringAssert.Contains(Field(withGeneral, "marca"), "disabled", "campo que não vale nesta categoria não é enviado");
        StringAssert.Contains(Field(withGeneral, "kmMax"), "disabled");
    }

    [TestMethod]
    public async Task US002S04_FiltrarTerrenosPorArea_MostraAreaMinimaEMaxima()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string land = await SlugAsync(site, Land);
        Stub(site).Rows.Add(Row(1, categoryId: Land));

        (_, string html) = await GetAsync(site, $"/busca?categoria={land}&areaMin=300&areaMax=600");

        Assert.AreEqual((300m, 600m), (Stub(site).Last.AreaMin, Stub(site).Last.AreaMax));
        Assert.IsFalse(Regex.IsMatch(html, @"data-filter=""area""[^>]*hidden"));
        StringAssert.Contains(Field(html, "areaMin"), "value=\"300\"");
        StringAssert.Contains(Field(html, "areaMax"), "value=\"600\"");
        Assert.IsTrue(Regex.IsMatch(html, @"data-filter=""brand""[^>]*hidden=""hidden"""), "marca não aparece em terrenos");
    }

    [TestMethod]
    public async Task US002S05_OrdenarOsResultados_AOrdemEscolhidaContinuaNaProximaPagina()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 30).Select(i => Row(i)));

        (_, string html) = await GetAsync(site, "/busca?q=casa&ordem=menor-preco");

        Assert.AreEqual(SearchOrder.PriceAscending, Stub(site).Last.Order);
        StringAssert.Matches(html, new Regex(@"<option value=""menor-preco""[^>]*selected[^>]*>Menor preço</option>"));
        string[] pageLinks = Hrefs(html, @"<nav aria-label=""Paginação""[\s\S]*?</nav>");
        Assert.IsTrue(pageLinks.Length > 0);
        Assert.IsTrue(pageLinks.All(h => h.Contains("ordem=menor-preco", StringComparison.Ordinal) && h.Contains("q=casa", StringComparison.Ordinal)), "todo link de página leva a ordem e o texto: " + string.Join(" ", pageLinks));
    }

    [TestMethod]
    public async Task US002S05_FormularioDeOrdenar_LevaOsFiltrosEOsTresValores_EComJavaScriptEnviaSozinho()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string cars = await SlugAsync(site, Cars);
        Stub(site).Rows.Add(Row(1));

        (_, string html) = await GetAsync(site, $"/busca?categoria={cars}&uf=SP&ordem=maior-preco&q=civic");

        string form = Regex.Match(html, @"<form[^>]*data-order-form[\s\S]*?</form>").Value;
        StringAssert.Contains(form, $"name=\"categoria\" value=\"{cars}\"");
        StringAssert.Contains(form, "name=\"uf\" value=\"SP\"");
        StringAssert.Contains(form, "name=\"q\" value=\"civic\"");
        Assert.IsFalse(form.Contains("name=\"ordem\" value=", StringComparison.Ordinal), "a ordem vem do select, não de um campo oculto");
        CollectionAssert.AreEqual(new[] { "recentes", "menor-preco", "maior-preco" }, Regex.Matches(form, @"<option value=""([^""]+)""").Select(m => m.Groups[1].Value).ToArray());
        StringAssert.Contains(form, "data-order-apply", "o botão Ordenar existe para quem está sem JavaScript");
    }

    [TestMethod]
    public async Task US002S06_Paginar_30Resultados_24NaPrimeira_6NaSegunda_ComAPaginaAtualDestacada()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 30).Select(i => Row(i)));

        (_, string first) = await GetAsync(site, "/busca");
        (_, string second) = await GetAsync(site, "/busca?pagina=2");

        Assert.AreEqual(24, CardCount(first));
        StringAssert.Contains(Text(first), "30 anúncios encontrados");
        StringAssert.Matches(first, new Regex(@"<a [^>]*href=""/busca\?pagina=2""[^>]*rel=""next""[^>]*>Próxima</a>"));
        Assert.AreEqual(6, CardCount(second));
        StringAssert.Matches(second, new Regex(@"<li class=""page-item active""><a [^>]*aria-current=""page""[^>]*>2</a>"), "a página 2 aparece destacada");
        Assert.AreEqual(2, Stub(site).Last.Page);
        Assert.AreEqual(24, Stub(site).Last.PageSize);
    }

    [TestMethod]
    public async Task US002S06_PaginaAlemDoFim_MostraAUltima()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 30).Select(i => Row(i)));

        (HttpStatusCode status, string html) = await GetAsync(site, "/busca?pagina=999");

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual(6, CardCount(html));
        StringAssert.Matches(html, new Regex(@"<li class=""page-item active""><a [^>]*aria-current=""page""[^>]*>2</a>"));
    }

    [TestMethod]
    public async Task US002S07_BuscaSemResultados_MostraAMensagemELimparFiltrosVoltaATudo()
    {
        using DraftSite site = await DraftSite.StartAsync();

        (HttpStatusCode status, string html) = await GetAsync(site, "/busca?q=xyzabc&uf=SP");

        Assert.AreEqual(HttpStatusCode.OK, status);
        StringAssert.Contains(Text(html), "Nenhum anúncio encontrado para esses filtros");
        Assert.IsTrue(Regex.IsMatch(html, @"<a [^>]*href=""/busca""[^>]*>Limpar filtros</a>"), "o botão limpa os filtros: leva a /busca sem nada");
        Assert.AreEqual(0, CardCount(html));
        Assert.IsFalse(html.Contains("data-pagination", StringComparison.Ordinal));
        StringAssert.Contains(Text(html), "0 anúncios encontrados");
    }

    [TestMethod]
    public async Task US002S07_LimparFiltros_VoltaAoPadrao_TudoPublicadoDoMaisRecenteAoMaisAntigo()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 5).Select(i => Row(i)));

        (_, string html) = await GetAsync(site, "/busca");

        SearchCriteria criteria = Stub(site).Last;
        Assert.AreEqual(0, criteria.Words.Count);
        Assert.AreEqual(0, criteria.CategoryIds.Count);
        Assert.AreEqual(SearchOrder.Recent, criteria.Order);
        Assert.AreEqual(5, CardCount(html));
    }

    [TestMethod]
    public async Task BuscaSemNenhumAnuncioPublicado_NoSite_NaoPedeParaLimparFiltros()
    {
        using DraftSite site = await DraftSite.StartAsync();

        (_, string html) = await GetAsync(site, "/busca");

        StringAssert.Contains(Text(html), "Ainda não há anúncios publicados");
        Assert.IsFalse(html.Contains("data-no-results", StringComparison.Ordinal) && Text(html).Contains("Nenhum anúncio encontrado para esses filtros", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task US002S08_FaixaDePrecoInvertida_MostraOErroJuntoDoCampo_ESaiSemAFaixa()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 3).Select(i => Row(i)));

        (HttpStatusCode status, string html) = await GetAsync(site, "/busca?precoMin=5000&precoMax=1000");

        Assert.AreEqual(HttpStatusCode.OK, status);
        StringAssert.Matches(html, new Regex(@"<div [^>]*id=""erro-preco""[^>]*role=""alert""[^>]*>O preço mínimo não pode ser maior que o máximo</div>"));
        Assert.IsFalse(Regex.IsMatch(html, @"id=""erro-preco""[^>]*hidden"), "a mensagem está visível");
        StringAssert.Matches(Field(html, "precoMin"), new Regex(@"class=""form-control is-invalid"""));
        StringAssert.Contains(Field(html, "precoMin"), "aria-describedby=\"erro-preco\"");
        StringAssert.Contains(Field(html, "precoMin"), "value=\"5000\"", "o que a pessoa digitou continua no campo");
        StringAssert.Contains(Field(html, "precoMax"), "value=\"1000\"");
        Assert.IsNull(Stub(site).Last.PriceMinCents);
        Assert.IsNull(Stub(site).Last.PriceMaxCents, "a lista sai sem a faixa de preço");
        Assert.AreEqual(3, CardCount(html));
        StringAssert.Contains(html, "data-keep-open=\"true\"", "com erro o painel de filtros não recolhe");
        StringAssert.Matches(html, new Regex(@"<div class=""collapse show d-lg-block"" id=""filtros"""), "com erro o painel já vem aberto no HTML");
        StringAssert.Matches(html, new Regex(@"data-filters-toggle[^>]*aria-expanded=""true""[^>]*>Filtros ▴</button>"));
    }

    [TestMethod]
    [DataRow("precoMin", "7000000000000000000000000000")]
    [DataRow("precoMax", "7000000000000000000000000000")]
    [DataRow("precoMin", "79228162514264337593543950335")]
    [DataRow("precoMax", "9999999999999999999999999999")]
    public async Task PrecoEnormeNoFiltro_MostraOErroJuntoDoCampo_EAListaSaiSemAFaixa_SemDar503(string field, string typed)
    {
        // Um número de 27 a 29 dígitos cabe em decimal mas estourava na multiplicação por 100 (OverflowException): a busca respondia 503
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 3).Select(i => Row(i)));

        (HttpStatusCode status, string html) = await GetAsync(site, $"/busca?{field}={typed}");

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.IsFalse(html.Contains("Não foi possível buscar agora", StringComparison.Ordinal));
        StringAssert.Contains(WebUtility.HtmlDecode(html), SearchService.PriceFormatMessage);
        StringAssert.Matches(Field(html, field), new Regex(@"class=""form-control is-invalid"""));
        StringAssert.Contains(Field(html, field), $"value=\"{typed}\"", "o que a pessoa digitou continua no campo");
        Assert.IsNull(Stub(site).Last.PriceMinCents);
        Assert.IsNull(Stub(site).Last.PriceMaxCents, "a lista sai sem a faixa de preço");
        Assert.AreEqual(3, CardCount(html));
    }

    [TestMethod]
    public async Task US002S08_SemErro_AMensagemFicaEscondidaParaOJavaScriptPreencher()
    {
        using DraftSite site = await DraftSite.StartAsync();

        (_, string html) = await GetAsync(site, "/busca");

        StringAssert.Matches(html, new Regex(@"<div [^>]*id=""erro-preco""[^>]*hidden=""hidden""[^>]*></div>"));
        StringAssert.Contains(html, "data-keep-open=\"false\"");
    }

    [TestMethod]
    public async Task US002S09_CompartilharPeloEndereco_OsMesmosFiltrosOMesmoPedidoEAMesmaOrdem()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string cars = await SlugAsync(site, Cars);
        Stub(site).Rows.AddRange(Enumerable.Range(1, 30).Select(i => Row(i)));
        string url = $"/busca?q=civic&categoria={cars}&uf=SP&cidade=Campinas&precoMin=1000&precoMax=50.000,00&marca=1&modelo=11&anoDe=2015&anoAte=2020&kmMax=100000&ordem=menor-preco";

        (_, string firstTab) = await GetAsync(site, url);
        (_, string otherTab) = await GetAsync(site, url);

        Assert.AreEqual(2, Stub(site).Requests.Count);
        SearchCriteria a = Stub(site).Requests[0];
        SearchCriteria b = Stub(site).Requests[1];
        CollectionAssert.AreEqual(a.Words.ToArray(), b.Words.ToArray());
        CollectionAssert.AreEqual(a.CategoryIds.ToArray(), b.CategoryIds.ToArray());
        Assert.AreEqual(a with { Words = [], CategoryIds = [] }, b with { Words = [], CategoryIds = [] }, "o mesmo endereço pede a mesma consulta");
        Assert.AreEqual(
            string.Join(',', Regex.Matches(firstTab, @"<option [^>]*selected[^>]*>").Select(m => m.Value)),
            string.Join(',', Regex.Matches(otherTab, @"<option [^>]*selected[^>]*>").Select(m => m.Value)));
        foreach ((string name, string value) in new[] { ("precoMin", "1000"), ("precoMax", "50.000,00"), ("anoDe", "2015"), ("anoAte", "2020"), ("kmMax", "100000") })
        {
            StringAssert.Contains(Field(otherTab, name), $"value=\"{value}\"", name);
        }

        string[] pageLinks = Hrefs(otherTab, @"<nav aria-label=""Paginação""[\s\S]*?</nav>");
        Assert.IsTrue(pageLinks.Any(h => h.Contains("pagina=2", StringComparison.Ordinal) && h.Contains("ordem=menor-preco", StringComparison.Ordinal) && h.Contains("marca=1", StringComparison.Ordinal) && h.Contains("precoMax=50.000,00", StringComparison.Ordinal)), string.Join(" | ", pageLinks));
    }

    [TestMethod]
    public async Task US002S09_OsCamposDoFormulario_SaoOsMesmosNomesQueOEnderecoLe()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string cars = await SlugAsync(site, Cars);

        (_, string html) = await GetAsync(site, $"/busca?categoria={cars}");

        // Produtor: os nomes dos campos do formulário (e o do "Ordenar"); consumidor: os parâmetros que o servidor lê do endereço
        string[] sent = [.. Regex.Matches(FiltersForm(html), @"<(?:input|select)[^>]*\bname=""([^""]+)""").Select(m => m.Groups[1].Value).Concat(["ordem", "pagina"]).Distinct().Order()];
        string[] read = [.. typeof(SearchQueryModel).GetProperties().Select(p => p.GetCustomAttribute<FromQueryAttribute>().Name).Order()];

        CollectionAssert.AreEqual(read, sent, "todo campo do formulário é lido pelo servidor, e todo parâmetro lido tem campo");
    }

    [TestMethod]
    public async Task US002S10_UfEscolhida_ListaSoAsCidadesDela_SemUfACidadeFicaDesabilitada()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await site.Harness.AddCitiesAsync(new City { IbgeCode = 3304557, Name = "Rio de Janeiro", Uf = "RJ", NameSearch = "rio de janeiro" });

        (_, string sp) = await GetAsync(site, "/busca?uf=SP&cidade=Campinas");
        (_, string rj) = await GetAsync(site, "/busca?uf=RJ&cidade=Campinas");
        (_, string none) = await GetAsync(site, "/busca");

        string[] spCities = [.. Regex.Matches(Regex.Match(sp, @"<select[^>]*data-filter-city[\s\S]*?</select>").Value, @"<option value=""([^""]*)""").Select(m => m.Groups[1].Value)];
        string[] rjCities = [.. Regex.Matches(Regex.Match(rj, @"<select[^>]*data-filter-city[\s\S]*?</select>").Value, @"<option value=""([^""]*)""").Select(m => m.Groups[1].Value)];
        CollectionAssert.AreEqual(new[] { "", "Campinas", "São Paulo" }, spCities);
        CollectionAssert.AreEqual(new[] { "", "Rio de Janeiro" }, rjCities, "a cidade da outra UF não aparece e não vale");
        Assert.IsNull(Stub(site).Requests[1].City);
        StringAssert.Contains(Regex.Match(none, @"<select[^>]*data-filter-city[^>]*>").Value, "disabled", "a cidade só pode ser escolhida depois da UF");
        StringAssert.Contains(Text(none), "Todas as cidades");
    }

    [TestMethod]
    public async Task US002S11_FalhaAoBuscar_Mostra503ComAMensagemOsFiltrosPreservadosETentarNovamente()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string cars = await SlugAsync(site, Cars);
        Stub(site).Failure = new InvalidOperationException("segredo-da-conexao: Server=prod;Password=abc");

        (HttpStatusCode status, string html) = await GetAsync(site, $"/busca?q=civic&categoria={cars}&uf=SP&precoMax=50000");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, status);
        StringAssert.Contains(Text(html), "Não foi possível buscar agora. Tente novamente.");
        StringAssert.Matches(html, new Regex($@"<a [^>]*href=""/busca\?q=civic&categoria={cars}&uf=SP&precoMax=50000""[^>]*>Tentar novamente</a>"));
        StringAssert.Matches(html, new Regex(@"Código de referência: <code>[^<]+</code>"));
        StringAssert.Matches(html, new Regex(@"<input[^>]*id=""busca-q""[^>]*value=""civic"""), "o texto continua na caixa de busca");
        StringAssert.Matches(html, new Regex($@"<option value=""{cars}""[^>]*selected"), "a categoria continua escolhida");
        StringAssert.Matches(html, new Regex(@"<option value=""SP""[^>]*selected"));
        StringAssert.Contains(Field(html, "precoMax"), "value=\"50000\"");
        Assert.IsFalse(html.Contains("segredo-da-conexao", StringComparison.Ordinal) || html.Contains("InvalidOperationException", StringComparison.Ordinal), "nada técnico na tela");
        Assert.AreEqual(0, CardCount(html));
    }

    [TestMethod]
    public async Task US002S11_DepoisDaFalha_BuscarDeNovoFunciona()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1));
        Stub(site).Failure = new TimeoutException();
        (HttpStatusCode failed, _) = await GetAsync(site, "/busca?q=civic");

        Stub(site).Failure = null;
        (HttpStatusCode again, string html) = await GetAsync(site, "/busca?q=civic");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, failed);
        Assert.AreEqual(HttpStatusCode.OK, again);
        Assert.AreEqual(1, CardCount(html));
    }

    [TestMethod]
    public async Task US002S12_Celular320px_OPainelDeFiltrosJaVemRecolhidoNoServidor_ComBotaoParaAbrir_SemLarguraFixa()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 6).Select(i => Row(i)));

        (_, string html) = await GetAsync(site, "/busca");

        // O painel já nasce recolhido no HTML (sem "show"), e o botão de abrir aparece sem esperar o JavaScript: recolher depois da primeira pintura fazia a lista pular (CLS, NFR-03)
        StringAssert.Matches(html, new Regex(@"<div class=""collapse d-lg-block"" id=""filtros"""));
        StringAssert.Matches(html, new Regex(@"<button [^>]*d-lg-none[^>]*data-filters-toggle[^>]*aria-expanded=""false""[^>]*aria-controls=""filtros""[^>]*>Filtros ▾</button>"));
        StringAssert.Matches(html, new Regex(@"<button [^>]*data-filters-close[^>]*>Fechar ▴</button>"));
        Assert.IsFalse(Regex.IsMatch(html, @"<[^>]*(data-filters-toggle|data-filters-close)[^>]*\shidden[\s>]"), "os botões não dependem do JavaScript para aparecer");
        // Sem JavaScript: o sem-js.css, dentro de noscript, abre o painel e esconde os botões
        StringAssert.Matches(html, new Regex(@"<noscript><link rel=""stylesheet"" href=""/css/sem-js\.css\?v="));
        Assert.IsFalse(Regex.IsMatch(html, @"<(div|input|select|form)[^>]*style=""[^""]*width\s*:\s*\d+px"), "nada com largura fixa");
        // A ordem na tela estreita: busca, filtros, total e cards (só rolagem para baixo)
        int search = html.IndexOf("id=\"busca-q\"", StringComparison.Ordinal);
        int filters = html.IndexOf("id=\"filtros\"", StringComparison.Ordinal);
        int total = html.IndexOf("data-search-total", StringComparison.Ordinal);
        int grid = html.IndexOf("data-ad-grid", StringComparison.Ordinal);
        Assert.IsTrue(search < filters && filters < total && total < grid);
    }

    [TestMethod]
    public async Task BuscaDoCabecalho_LevaOsDemaisFiltrosEscondidos_ESemOTextoQueEleMesmoEdita()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string cars = await SlugAsync(site, Cars);

        (_, string html) = await GetAsync(site, $"/busca?q=civic&categoria={cars}&uf=SP&ordem=menor-preco");

        string header = Regex.Match(html, @"<form class=""cabecalho__busca""[\s\S]*?</form>").Value;
        StringAssert.Contains(header, $"name=\"categoria\" value=\"{cars}\"");
        StringAssert.Contains(header, "name=\"uf\" value=\"SP\"");
        StringAssert.Contains(header, "name=\"ordem\" value=\"menor-preco\"");
        Assert.AreEqual(1, Regex.Matches(header, @"name=""q""").Count, "o texto vem só da caixa");
    }

    [TestMethod]
    public async Task BuscaDoCabecalho_NaPaginaInicial_NaoLevaFiltroNenhum()
    {
        using DraftSite site = await DraftSite.StartAsync();

        (_, string html) = await GetAsync(site, "/");

        string header = Regex.Match(html, @"<form class=""cabecalho__busca""[\s\S]*?</form>").Value;
        Assert.AreEqual(1, Regex.Matches(header, @"<input").Count);
    }

    [TestMethod]
    public async Task TextoDeMaisDe100Caracteres_MostraOErroJuntoDaCaixaDeBusca_ESaiSemOTexto()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1));
        string longText = new('a', 101);

        (HttpStatusCode status, string html) = await GetAsync(site, "/busca?q=" + longText);

        Assert.AreEqual(HttpStatusCode.OK, status);
        StringAssert.Matches(html, new Regex(@"<div [^>]*id=""busca-q-erro""[^>]*role=""alert""[^>]*>O texto da busca pode ter no máximo 100 caracteres</div>"));
        StringAssert.Matches(html, new Regex(@"<input[^>]*id=""busca-q""[^>]*is-invalid|<input class=""form-control is-invalid""[^>]*id=""busca-q"""));
        Assert.AreEqual(0, Stub(site).Last.Words.Count);
        Assert.AreEqual(1, CardCount(html));
    }

    [TestMethod]
    public async Task TextoDoEndereco_NuncaViraHtml()
    {
        using DraftSite site = await DraftSite.StartAsync();
        const string attack = "\"><script>alert(1)</script>";

        string html = await GetEncodedAsync(site, "/busca?q=" + Uri.EscapeDataString(attack) + "&cidade=" + Uri.EscapeDataString(attack) + "&precoMin=" + Uri.EscapeDataString(attack));

        Assert.IsFalse(html.Contains("<script>alert(1)", StringComparison.Ordinal), "o texto é sempre codificado");
        StringAssert.Contains(html, "&quot;&gt;&lt;script&gt;alert(1)&lt;/script&gt;");
    }

    [TestMethod]
    public async Task PaginaDeCategoria_TemOLinkFiltrarEOrdenarParaABuscaDaCategoria()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string cars = await SlugAsync(site, Cars);

        (_, string html) = await GetAsync(site, "/categoria/" + cars);

        StringAssert.Matches(html, new Regex($@"<a [^>]*href=""/busca\?categoria={cars}""[^>]*data-filter-link[^>]*>[\s\S]*?Filtrar e ordenar</a>"));
    }

    [TestMethod]
    public async Task Busca_MostraACategoriaNoCaminhoDeNavegacao()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string cars = await SlugAsync(site, Cars);

        (_, string html) = await GetAsync(site, "/busca?categoria=" + cars);

        string crumbs = Text(Regex.Match(html, @"<nav aria-label=""Caminho de navegação""[\s\S]*?</nav>").Value);
        StringAssert.Contains(crumbs, "Início Busca Automóveis, Peças e Acessórios Carros, vans e utilitários");
    }

    [TestMethod]
    public async Task ParametroInvalido_VoltaAoPadraoSemErro_PaginaContinua200()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1));

        (HttpStatusCode status, string html) = await GetAsync(site, "/busca?pagina=abc&ordem=xyz&categoria=naoexiste&uf=XX&cidade=Nada&marca=abc&anoDe=zz");

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual(1, CardCount(html));
        SearchCriteria criteria = Stub(site).Last;
        Assert.AreEqual((1, SearchOrder.Recent, null, null), (criteria.Page, criteria.Order, criteria.Uf, criteria.City));
        Assert.IsFalse(html.Contains("is-invalid", StringComparison.Ordinal), "nenhuma mensagem de erro para parâmetro de endereço que não é de um campo");
    }

    [TestMethod]
    public async Task Busca_NaoPrecisaDeLogin_ENaoMostraDadoDoAutor()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1));

        (HttpStatusCode status, string html) = await GetAsync(site, "/busca");

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.IsFalse(Regex.IsMatch(html, @"@exemplo|autor|tel:|wa\.me", RegexOptions.IgnoreCase));
    }
}
