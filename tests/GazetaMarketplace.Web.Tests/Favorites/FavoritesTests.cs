using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Favorites;

/// <summary>
/// Favoritos (US-005; tarefa 5.5), a parte do servidor: a API <c>GET /api/v1/ads?ids=</c> (só publicados, na ordem pedida, <c>PagedResult</c>), o fragmento que a página "Meus favoritos" pede,
/// a moldura da página (aviso permanente, sem JavaScript, <c>noindex</c>) e o coração dos cards. O que o navegador faz com o <c>localStorage</c> é provado no E2E. A consulta de verdade (só publicados no
/// SQL Server) é provada na integração; aqui o repositório é o de mentira, que devolve as linhas pedidas que o teste pôs em <c>Rows</c> (as "publicadas"), fora de ordem de propósito.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class FavoritesTests
#pragma warning restore CA1515
{
    private const int Cars = 33;
    private const int Services = 66;
    private const int Jobs = 96;

    private static string Text(string html) => WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(html, @"<(script|style)[\s\S]*?</\1>", " "), @"<[^>]+>", " ")) is { } t ? Regex.Replace(t, @"\s+", " ").Trim() : string.Empty;

    private static StubShowcaseRepository Stub(DraftSite site) => site.Harness.Factory.Services.GetRequiredService<StubShowcaseRepository>();

    private static ShowcaseRow Row(int id, string title = null, int? categoryId = Cars, long? price = 6_200_000, bool cover = true, string attributes = "{}") =>
        new(id, title ?? $"Anúncio {id:00}", categoryId, price, attributes, "Campinas", "SP", cover ? id * 10 : null, cover ? 1600 : null, cover ? 1200 : null);

    private static async Task<(HttpStatusCode Status, string Body, HttpResponseMessage Response)> GetAsync(DraftSite site, string path)
    {
        using HttpClient visitor = site.Harness.Anonymous();
        HttpResponseMessage response = await visitor.GetAsync(path);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(), response);
    }

    private static JsonElement[] Items(string json) => [.. JsonDocument.Parse(json).RootElement.GetProperty("items").EnumerateArray().Select(e => e.Clone())];

    // ---------- Os ids ----------

    [TestMethod]
    [DataRow("12", new[] { 12 })]
    [DataRow("12,57,104", new[] { 12, 57, 104 })]
    [DataRow("104,12,57", new[] { 104, 12, 57 })]
    [DataRow("5,5,7,5", new[] { 5, 7 })]
    [DataRow("1", new[] { 1 })]
    [DataRow("2147483647", new[] { int.MaxValue })]
    public void Ids_Validos_MantemAOrdemPedida_RepetidoContaUmaVez(string text, int[] expected)
    {
        Assert.IsTrue(FavoriteIds.TryParse(text, out int[] ids));
        CollectionAssert.AreEqual(expected, ids);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("abc")]
    [DataRow("1,")]
    [DataRow(",1")]
    [DataRow("1,,2")]
    [DataRow("1, 2")]
    [DataRow(" 1")]
    [DataRow("0")]
    [DataRow("007")]
    [DataRow("01")]
    [DataRow("1,0")]
    [DataRow("-1")]
    [DataRow("+1")]
    [DataRow("1.5")]
    [DataRow("1e3")]
    [DataRow("2147483648")]
    [DataRow("99999999999999999999")]
    [DataRow("٣")]
    [DataRow("1;2")]
    [DataRow("1%2C2")]
    public void Ids_Invalidos_SaoRecusados(string text)
    {
        Assert.IsFalse(FavoriteIds.TryParse(text, out int[] ids));
        Assert.AreEqual(0, ids.Length);
    }

    [TestMethod]
    public void Ids_Ate100Passa_101NaoPassa_RepetidosContamNoLimite()
    {
        Assert.IsTrue(FavoriteIds.TryParse(string.Join(',', Enumerable.Range(1, 100)), out int[] hundred));
        Assert.AreEqual(100, hundred.Length);
        Assert.IsFalse(FavoriteIds.TryParse(string.Join(',', Enumerable.Range(1, 101)), out _));
        Assert.IsFalse(FavoriteIds.TryParse(string.Join(',', Enumerable.Repeat(1, 101)), out _), "o limite é de ids pedidos, não de ids diferentes");
    }

    // ---------- A API ----------

    [TestMethod]
    public async Task Api_DevolveOsPublicadosNaOrdemPedida_NoEnvelopeDePaginacao_SemLogin()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange([Row(3), Row(10), Row(7)]);

        (HttpStatusCode status, string body, HttpResponseMessage response) = await GetAsync(site, "/api/v1/ads?ids=10,3,99,7");

        Assert.AreEqual(HttpStatusCode.OK, status);
        CollectionAssert.AreEqual(new[] { 10, 3, 7 }, Items(body).Select(i => i.GetProperty("id").GetInt32()).ToArray(), "na ordem pedida; o 99 (não publicado ou inexistente) não volta");
        JsonElement root = JsonDocument.Parse(body).RootElement;
        Assert.AreEqual(1, root.GetProperty("page").GetInt32());
        Assert.AreEqual(4, root.GetProperty("pageSize").GetInt32());
        Assert.AreEqual(3, root.GetProperty("totalCount").GetInt32());
        Assert.AreEqual(1, root.GetProperty("totalPages").GetInt32());
        Assert.IsFalse(root.GetProperty("hasPrevious").GetBoolean());
        Assert.IsFalse(root.GetProperty("hasNext").GetBoolean());
        StringAssert.Contains(response.Headers.CacheControl!.ToString(), "no-store");
    }

    [TestMethod]
    public async Task Api_OCardTemOsCamposDoContrato_SemDadoDoAutor()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(104, "Honda Civic 2018"));

        (_, string body, _) = await GetAsync(site, "/api/v1/ads?ids=104");

        JsonElement card = Items(body).Single();
        CollectionAssert.AreEquivalent(
            new[] { "id", "title", "priceCents", "priceLabel", "serviceType", "city", "uf", "categoryName", "fieldGroup", "coverUrl", "url" },
            card.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.AreEqual("Honda Civic 2018", card.GetProperty("title").GetString());
        Assert.AreEqual(6_200_000L, card.GetProperty("priceCents").GetInt64());
        Assert.AreEqual("Preço", card.GetProperty("priceLabel").GetString());
        Assert.AreEqual("Campinas", card.GetProperty("city").GetString());
        Assert.AreEqual("SP", card.GetProperty("uf").GetString());
        Assert.AreEqual("Carros, vans e utilitários", card.GetProperty("categoryName").GetString());
        Assert.AreEqual("Cars", card.GetProperty("fieldGroup").GetString());
        Assert.AreEqual("/fotos/104/1040-480.webp", card.GetProperty("coverUrl").GetString());
        Assert.AreEqual("/anuncio/104/honda-civic-2018", card.GetProperty("url").GetString());
        Assert.IsFalse(Regex.IsMatch(body, @"autor|author|email|@exemplo", RegexOptions.IgnoreCase), "nenhum dado do autor");
    }

    [TestMethod]
    public async Task Api_ServicosNaoTemPreco_VagasNaoTemCapa()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange([
            Row(2, "Diarista com experiência", Services, price: null, attributes: """{"serviceTypeId":1}"""),
            Row(3, "Pizzaiolo com experiência", Jobs, price: 280_000, cover: false, attributes: """{"jobAreaIds":[2,1]}""")]);

        (_, string body, _) = await GetAsync(site, "/api/v1/ads?ids=2,3");

        JsonElement[] items = Items(body);
        JsonElement service = items[0];
        JsonElement job = items[1];
        Assert.AreEqual(JsonValueKind.Null, service.GetProperty("priceCents").ValueKind, "Serviços não têm preço (A6)");
        Assert.AreEqual(JsonValueKind.Null, service.GetProperty("priceLabel").ValueKind);
        Assert.AreNotEqual(JsonValueKind.Null, service.GetProperty("serviceType").ValueKind, "o tipo do serviço aparece no lugar do preço");
        Assert.AreEqual(JsonValueKind.Null, job.GetProperty("coverUrl").ValueKind, "Vagas não têm foto (A4)");
        Assert.AreEqual(280_000L, job.GetProperty("priceCents").GetInt64());
        Assert.AreEqual("Salário", job.GetProperty("priceLabel").GetString());
    }

    [TestMethod]
    public async Task Api_AnuncioSemFoto_TemCapaNula()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(5, cover: false));

        (_, string body, _) = await GetAsync(site, "/api/v1/ads?ids=5");

        Assert.AreEqual(JsonValueKind.Null, Items(body).Single().GetProperty("coverUrl").ValueKind);
    }

    [TestMethod]
    public async Task Api_Aceita100Ids_E_MaisDe100Da400()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 100).Select(i => Row(i)));

        (HttpStatusCode ok, string body, _) = await GetAsync(site, "/api/v1/ads?ids=" + string.Join(',', Enumerable.Range(1, 100)));
        (HttpStatusCode tooMany, string problem, HttpResponseMessage response) = await GetAsync(site, "/api/v1/ads?ids=" + string.Join(',', Enumerable.Range(1, 101)));

        Assert.AreEqual(HttpStatusCode.OK, ok);
        Assert.AreEqual(100, Items(body).Length);
        Assert.AreEqual(HttpStatusCode.BadRequest, tooMany);
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        StringAssert.Contains(problem, "VALIDATION_ERROR");
        StringAssert.Contains(problem, "ids");
        Assert.AreEqual(1, Stub(site).IdRequests.Count, "o pedido de 101 ids nem chega ao repositório");
    }

    [TestMethod]
    [DataRow("/api/v1/ads")]
    [DataRow("/api/v1/ads?ids=")]
    [DataRow("/api/v1/ads?ids=abc")]
    [DataRow("/api/v1/ads?ids=1,x")]
    [DataRow("/api/v1/ads?ids=-1")]
    [DataRow("/api/v1/ads?ids=1.5")]
    [DataRow("/api/v1/ads?ids=1,,2")]
    [DataRow("/api/v1/ads?ids=99999999999")]
    public async Task Api_IdsNaoNumericosOuAusentes_Da400_SemConsultar(string path)
    {
        using DraftSite site = await DraftSite.StartAsync();

        (HttpStatusCode status, _, _) = await GetAsync(site, path);

        Assert.AreEqual(HttpStatusCode.BadRequest, status);
        Assert.AreEqual(0, Stub(site).IdRequests.Count);
    }

    [TestMethod]
    public async Task Api_150Ids_EmDuasChamadas100Mais50_CadaUmaNaOrdemPedida_ENenhumaPassaDoLimite()
    {
        // O que a página faz com mais de 100 favoritos (favorites.js, lotes de 100): duas chamadas, de 100 e de 50 ids; o servidor atende cada uma e recusa a que passa de 100
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 150).Select(i => Row(i)));
        int[] all = [.. Enumerable.Range(1, 150).Reverse()];

        (HttpStatusCode firstStatus, string firstBody, _) = await GetAsync(site, "/api/v1/ads?ids=" + string.Join(',', all.Take(100)));
        (HttpStatusCode secondStatus, string secondBody, _) = await GetAsync(site, "/api/v1/ads?ids=" + string.Join(',', all.Skip(100)));
        (HttpStatusCode wholeStatus, _, _) = await GetAsync(site, "/api/v1/ads?ids=" + string.Join(',', all));

        Assert.AreEqual(HttpStatusCode.OK, firstStatus);
        Assert.AreEqual(HttpStatusCode.OK, secondStatus);
        Assert.AreEqual(HttpStatusCode.BadRequest, wholeStatus, "150 ids numa chamada só passam do limite");
        CollectionAssert.AreEqual(all.Take(100).ToArray(), Items(firstBody).Select(i => i.GetProperty("id").GetInt32()).ToArray(), "o primeiro lote, na ordem pedida");
        CollectionAssert.AreEqual(all.Skip(100).ToArray(), Items(secondBody).Select(i => i.GetProperty("id").GetInt32()).ToArray(), "o segundo lote, na ordem pedida");
        CollectionAssert.AreEqual(new[] { 100, 50 }, Stub(site).IdRequests.Select(r => r.Length).ToArray(), "o repositório recebeu um pedido de 100 e outro de 50");
    }

    [TestMethod]
    public async Task Fragmento_150Ids_EmDuasChamadas100Mais50_TrazOsCardsDeCadaLote()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange(Enumerable.Range(1, 150).Select(i => Row(i)));

        (HttpStatusCode first, string firstHtml, _) = await GetAsync(site, "/favoritos/lista?ids=" + string.Join(',', Enumerable.Range(1, 100)));
        (HttpStatusCode second, string secondHtml, _) = await GetAsync(site, "/favoritos/lista?ids=" + string.Join(',', Enumerable.Range(101, 50)));
        (HttpStatusCode whole, _, _) = await GetAsync(site, "/favoritos/lista?ids=" + string.Join(',', Enumerable.Range(1, 150)));

        Assert.AreEqual(HttpStatusCode.OK, first);
        Assert.AreEqual(HttpStatusCode.OK, second);
        Assert.AreEqual(HttpStatusCode.BadRequest, whole);
        Assert.AreEqual(100, Regex.Matches(firstHtml, @"<li [^>]*data-ad-id=""").Count);
        Assert.AreEqual(50, Regex.Matches(secondHtml, @"<li [^>]*data-ad-id=""").Count);
    }

    [TestMethod]
    public async Task Api_IdsRepetidos_ContamUmaVez_EUmIdSoDevolveUmCard()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange([Row(1), Row(2)]);

        (_, string body, _) = await GetAsync(site, "/api/v1/ads?ids=2,1,2,1");

        CollectionAssert.AreEqual(new[] { 2, 1 }, Items(body).Select(i => i.GetProperty("id").GetInt32()).ToArray());
        CollectionAssert.AreEqual(new[] { 2, 1 }, Stub(site).IdRequests.Single());
    }

    [TestMethod]
    public async Task Api_NenhumIdPublicado_DevolveListaVazia_200()
    {
        using DraftSite site = await DraftSite.StartAsync();

        (HttpStatusCode status, string body, _) = await GetAsync(site, "/api/v1/ads?ids=1,2,3");

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual(0, Items(body).Length);
        Assert.AreEqual(0, JsonDocument.Parse(body).RootElement.GetProperty("totalCount").GetInt32());
    }

    [TestMethod]
    public async Task Api_FalhaNaConsulta_Da500SemDetalheTecnico()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Failure = new InvalidOperationException("segredo-da-conexao");

        (HttpStatusCode status, string body, _) = await GetAsync(site, "/api/v1/ads?ids=1");

        Assert.AreEqual(HttpStatusCode.InternalServerError, status);
        Assert.IsFalse(body.Contains("segredo-da-conexao", StringComparison.Ordinal));
    }

    // ---------- O fragmento da página ----------

    [TestMethod]
    public async Task Fragmento_CardsPublicadosNaOrdemPedida_CadaUmComRemover_SemLayoutDaPagina()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange([Row(1, "Honda Civic 2018"), Row(2, "Terreno plano"), Row(3, "Moto CG")]);

        (HttpStatusCode status, string html, HttpResponseMessage response) = await GetAsync(site, "/favoritos/lista?ids=3,1,2,77");

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        CollectionAssert.AreEqual(new[] { 3, 1, 2 }, Regex.Matches(html, @"<li class=""col"" data-ad-id=""(\d+)""").Select(m => int.Parse(m.Groups[1].Value)).ToArray(), "os ids que voltam estão no HTML, na ordem pedida (o 77 ficou de fora)");
        Assert.AreEqual(3, Regex.Matches(html, @"data-favorite-remove").Count);
        Assert.AreEqual(3, Regex.Matches(html, @"<article class=""card ad-card[^""]*"" data-ad-card>").Count, "o mesmo AdCard das demais listas");
        Assert.IsFalse(html.Contains("data-favorite-toggle", StringComparison.Ordinal), "na página dos favoritos o controle é o Remover, não o coração");
        Assert.IsFalse(html.Contains("<html", StringComparison.OrdinalIgnoreCase) || html.Contains("<header", StringComparison.OrdinalIgnoreCase), "só o fragmento");
        StringAssert.Contains(response.Headers.CacheControl!.ToString(), "no-store");
    }

    [TestMethod]
    public async Task Fragmento_RemoverTemNomeAcessivelComOTitulo()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1, "Honda Civic 2018"));

        (_, string html, _) = await GetAsync(site, "/favoritos/lista?ids=1");

        string button = Regex.Match(html, @"<button [^>]*data-favorite-remove[\s\S]*?</button>").Value;
        StringAssert.Contains(Text(button), "Remover Honda Civic 2018 dos favoritos");
        StringAssert.Contains(button, "alvo-toque");
    }

    [TestMethod]
    public async Task Fragmento_SemNenhumPublicado_DevolveAListaVazia()
    {
        using DraftSite site = await DraftSite.StartAsync();

        (HttpStatusCode status, string html, _) = await GetAsync(site, "/favoritos/lista?ids=1,2");

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual(0, Regex.Matches(html, @"data-ad-id").Count);
    }

    [TestMethod]
    [DataRow("/favoritos/lista")]
    [DataRow("/favoritos/lista?ids=abc")]
    [DataRow("/favoritos/lista?ids=1,2,x")]
    public async Task Fragmento_IdsInvalidos_Da400(string path)
    {
        using DraftSite site = await DraftSite.StartAsync();

        (HttpStatusCode status, _, _) = await GetAsync(site, path);

        Assert.AreEqual(HttpStatusCode.BadRequest, status);
    }

    [TestMethod]
    public async Task Fragmento_TextoDoTituloNuncaViraHtml()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1, "<script>alert(1)</script> \"x\""));

        (_, string html, _) = await GetAsync(site, "/favoritos/lista?ids=1");

        Assert.IsFalse(html.Contains("<script>alert(1)", StringComparison.Ordinal));
        StringAssert.Contains(html, "&lt;script&gt;alert(1)&lt;/script&gt;");
    }

    // ---------- A moldura da página ----------

    [TestMethod]
    public async Task US005S05_S08_Pagina_TemOAvisoPermanente_AMensagemSemJavaScript_ENoindex()
    {
        using DraftSite site = await DraftSite.StartAsync();

        (HttpStatusCode status, string html, _) = await GetAsync(site, "/favoritos");
        string visible = Text(html);

        Assert.AreEqual(HttpStatusCode.OK, status);
        StringAssert.Matches(html, new Regex(@"<h1[^>]*data-favorites-title[^>]*>Meus favoritos</h1>"));
        StringAssert.Contains(visible, "Seus favoritos ficam salvos apenas neste navegador.");
        StringAssert.Matches(html, new Regex(@"<div class=""alert alert-info"" role=""note"" data-favorites-permanent-notice>"), "o aviso está sempre no HTML, não depende de JavaScript nem de a lista estar vazia");
        StringAssert.Matches(html, new Regex(@"<noscript>\s*<div[^>]*data-favorites-no-js>Para ver seus favoritos, ative o JavaScript\.</div>"));
        StringAssert.Contains(html, "<meta name=\"robots\" content=\"noindex, follow\" />");
        StringAssert.Matches(html, new Regex(@"<section(?=[^>]*\bhidden\b)[^>]*data-favorites-empty"), "o estado vazio vem escondido; o favorites.js decide");
        StringAssert.Contains(visible, "Você ainda não favoritou nenhum anúncio");
        StringAssert.Matches(html, new Regex(@"<a [^>]*href=""/""[^>]*>Ir para a página inicial</a>"));
        StringAssert.Contains(html, "/js/pages/favorites.js");
    }

    [TestMethod]
    public async Task Pagina_NaoFazConsultaNenhuma_OServidorNaoSabeOsFavoritos()
    {
        using DraftSite site = await DraftSite.StartAsync();

        await GetAsync(site, "/favoritos");

        Assert.AreEqual(0, Stub(site).IdRequests.Count);
        Assert.AreEqual(0, Stub(site).CategoryRequests.Count);
    }

    // ---------- O coração e o botão ----------

    [TestMethod]
    public async Task Coracao_NoCardDaLista_TemNomeAcessivelComOTitulo_AriaPressed_EVemEscondidoSemJavaScript()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.Add(Row(1, "Honda Civic 2018"));

        (_, string html, _) = await GetAsync(site, "/");

        string heart = Regex.Match(html, @"<button [^>]*data-favorite-toggle[^>]*>[\s\S]*?</button>").Value;
        StringAssert.Contains(heart, "aria-label=\"Favoritar anúncio Honda Civic 2018\"");
        StringAssert.Contains(heart, "aria-pressed=\"false\"");
        StringAssert.Contains(heart, "data-ad-id=\"1\"");
        StringAssert.Contains(heart, "type=\"button\"");
        StringAssert.Matches(heart, new Regex(@"^<button [^>]*\shidden[\s>]"), "sem JavaScript não há onde guardar: o coração só aparece quando o JavaScript o liga");
        StringAssert.Contains(heart, "ad-card__favorito");
        StringAssert.Contains(heart, "<i class=\"fa fa-heart-o\" aria-hidden=\"true\"></i>");
    }

    [TestMethod]
    public async Task Coracao_EstaEmTodasAsListas_Inicial_Categoria_Busca()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Stub(site).Rows.AddRange([Row(1), Row(2)]);
        Site.Search(site);

        foreach (string path in new[] { "/", "/categoria/cars", "/busca" })
        {
            (HttpStatusCode status, string html, _) = await GetAsync(site, path);
            Assert.AreEqual(HttpStatusCode.OK, status, path);
            Assert.AreEqual(2, Regex.Matches(html, @"data-favorite-toggle").Count, $"{path}: um coração por card");
        }
    }

    [TestMethod]
    public async Task US005S02_PaginaDoAnuncio_TemOBotaoFavoritar_EscondidoAteOJavaScript_ComTextoEPressed()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await Detail.AdDetailTests.AddAsync(site, "Honda Civic 2018", 33);

        (_, string raw) = await Detail.AdDetailTests.GetRawAsync(site, Detail.AdDetailTests.Url(id, "Honda Civic 2018"));

        string button = Regex.Match(raw, @"<button [^>]*data-favorite-toggle[\s\S]*?</button>").Value;
        StringAssert.Contains(button, $"data-ad-id=\"{id}\"");
        StringAssert.Contains(button, "aria-pressed=\"false\"");
        StringAssert.Matches(button, new Regex(@"^<button [^>]*\shidden[\s>]"), "o botão só aparece quando o JavaScript o liga");
        StringAssert.Contains(Text(button), "Favoritar");
        StringAssert.Matches(button, new Regex(@"<span data-favorite-label>Favoritar</span>"));
        StringAssert.Contains(button, "alvo-toque");
    }

    [TestMethod]
    public async Task PreVisualizacaoDoPainel_NaoMostraCoracao()
    {
        using PanelFixture panel = await PanelFixture.StartAsync(environment: "Development");

        string html = await panel.Admin.GetStringAsync("/painel/componentes");

        Assert.IsFalse(html.Contains("data-favorite-toggle", StringComparison.Ordinal), "a página de componentes do painel não é do visitante");
    }

    [TestMethod]
    public async Task ContadorDoTopo_VemComZero_ComOLinkParaMeusFavoritos()
    {
        using DraftSite site = await DraftSite.StartAsync();

        (_, string html, _) = await GetAsync(site, "/");

        StringAssert.Matches(html, new Regex(@"<a [^>]*href=""/favoritos""[^>]*>[\s\S]*?\(<span data-favoritos-contagem>0</span>\)</a>"));
    }

    private static class Site
    {
        // A busca precisa das linhas no repositório de busca (o de mentira da busca é outro)
        public static void Search(DraftSite site)
        {
            StubSearchReadRepository search = site.Harness.Factory.Services.GetRequiredService<StubSearchReadRepository>();
            search.Rows.AddRange(Stub(site).Rows);
        }
    }
}
