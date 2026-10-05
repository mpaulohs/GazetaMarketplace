using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Showcase;

/// <summary>
/// Os favoritos (US-005 e US-011-S04) no navegador, no site publicado: favoritar pelo card (S01) e pela página do anúncio (S02), a lista que continua em outra visita (S03), "Remover" (S04), a lista
/// vazia (S05), o anúncio que sai do ar (S06 e US-011-S04, no singular e no plural), o armazenamento bloqueado (S07), outro navegador sem a lista (S08), outra aba, teclado, lixo no armazenamento,
/// 320 px, sem JavaScript e o axe. Três anúncios com um texto único isolam os testes dos anúncios que os outros testes deixaram no banco; a conta do E2E (Administrador) os publica e arquiva pelas telas.
/// Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class FavoritesE2ETests : SitePage
#pragma warning restore CA1515
{
    private const string StorageKey = "gazeta:favoritos:v1";

    private static string _token;
    private static string[] _titles;

    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static string Describe(AxeResultItem violation) =>
        violation.Id + ": " + violation.Help + " [" + string.Join(" | ", violation.Nodes.Take(3).Select(n => n.Html + " => " + string.Join("; ", n.Any.Select(a => a.Message)))) + "]";

    // Três anúncios com o mesmo texto único, publicados uma vez e reaproveitados (nenhum teste os arquiva)
    private async Task<string[]> TitlesAsync()
    {
        if (_titles is null)
        {
            using FakeViaCep viaCep = new();
            await PublishingFlow.SignInAdminAsync(Page).ConfigureAwait(false);
            string token = "fv" + Guid.NewGuid().ToString("N")[..6];
            List<string> titles = [];
            foreach (string price in new[] { "100000", "200000", "300000" })
            {
                titles.Add(await PublishingFlow.PublishAsync(Page, $"{token} caderno", 1, price: price).ConfigureAwait(false));
            }

            _token = token;
            _titles = [.. titles];
        }

        return _titles;
    }

    private async Task<IPage> VisitorAsync(string path, int? width = null, bool javaScript = true, IBrowserContext existing = null, string init = null)
    {
        IBrowserContext context = existing ?? await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true, JavaScriptEnabled = javaScript }).ConfigureAwait(false);
        if (init is not null)
        {
            await context.AddInitScriptAsync(init).ConfigureAwait(false);
        }

        IPage visitor = await context.NewPageAsync().ConfigureAwait(false);
        if (width is { } w)
        {
            await visitor.SetViewportSizeAsync(w, 800).ConfigureAwait(false);
        }

        await visitor.GotoAsync(Url(path)).ConfigureAwait(false);
        return visitor;
    }

    private static ILocator Heart(IPage page, string title) => page.GetByRole(AriaRole.Button, new() { Name = "Favoritar anúncio " + title, Exact = true });

    private static ILocator Counter(IPage page) => page.Locator("[data-favoritos-contagem]");

    private static ILocator FavoriteCards(IPage page) => page.Locator("[data-favorites-host] li[data-ad-id]");

    private static async Task<int> IdOfAsync(IPage page, string title) => int.Parse((await Heart(page, title).GetAttributeAsync("data-ad-id").ConfigureAwait(false))!);

    private static async Task<int[]> StoredAsync(IPage page)
    {
        string raw = await page.EvaluateAsync<string>($"() => localStorage.getItem('{StorageKey}')").ConfigureAwait(false);
        return raw is null ? [] : [.. System.Text.Json.JsonSerializer.Deserialize<int[]>(raw)];
    }

    private static async Task<bool> HasHorizontalScrollAsync(IPage page) =>
        await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth").ConfigureAwait(false);

    // A página de cada teste é nova: entra no painel só se ainda não entrou (o login tem limite de tentativas)
    private async Task EnsureAdminAsync()
    {
        await Page.GotoAsync(Url("/painel/anuncios")).ConfigureAwait(false);
        if (Page.Url.Contains("/entrar", StringComparison.Ordinal))
        {
            await PublishingFlow.SignInAdminAsync(Page).ConfigureAwait(false);
        }
    }

    private async Task<string> ArchiveAsync(string title)
    {
        string path = await PublishingFlow.PublicPathAsync(Browser, title).ConfigureAwait(false);
        string id = Regex.Match(path, @"/anuncio/(\d+)/").Groups[1].Value;
        await Page.GotoAsync(Url($"/painel/anuncios/{id}/editar")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio arquivado" })).ToBeVisibleAsync().ConfigureAwait(false);
        return id;
    }

    [TestMethod]
    public async Task US005S01_S02_FavoritarPeloCardEPelaPaginaDoAnuncio_OContadorAcompanha_ECliqueDeNovoDesfavorita()
    {
        string[] titles = await TitlesAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={_token}").ConfigureAwait(false);

        // S01: o coração do card
        await Expect(Heart(visitor, titles[0])).ToHaveAttributeAsync("aria-pressed", "false").ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("0").ConfigureAwait(false);
        await Heart(visitor, titles[0]).ClickAsync().ConfigureAwait(false);
        await Expect(Heart(visitor, titles[0])).ToHaveAttributeAsync("aria-pressed", "true").ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("1").ConfigureAwait(false);
        await Heart(visitor, titles[1]).ClickAsync().ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("2").ConfigureAwait(false);
        int a = await IdOfAsync(visitor, titles[0]).ConfigureAwait(false);
        int b = await IdOfAsync(visitor, titles[1]).ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { a, b }, await StoredAsync(visitor).ConfigureAwait(false), "o armazenamento guarda os ids, na ordem em que foram favoritados");

        // Clicar de novo desfavorita
        await Heart(visitor, titles[0]).ClickAsync().ConfigureAwait(false);
        await Expect(Heart(visitor, titles[0])).ToHaveAttributeAsync("aria-pressed", "false").ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("1").ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { b }, await StoredAsync(visitor).ConfigureAwait(false));

        // S02: o botão da página do anúncio, e o estado volta depois de recarregar
        await visitor.GotoAsync(Url($"/anuncio/{a}/")).ConfigureAwait(false);
        ILocator button = visitor.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Favorit") });
        await Expect(button).ToContainTextAsync("Favoritar").ConfigureAwait(false);
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "false").ConfigureAwait(false);
        await button.ClickAsync().ConfigureAwait(false);
        await Expect(button).ToContainTextAsync("Favoritado").ConfigureAwait(false);
        await Expect(button).ToHaveAttributeAsync("aria-pressed", "true").ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("2").ConfigureAwait(false);
        await visitor.ReloadAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Favorit") })).ToContainTextAsync("Favoritado").ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("2").ConfigureAwait(false);
        await visitor.Context.CloseAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US005S03_S04_S05_S08_AListaContinuaEmOutraVisita_RemoverTiraEMostraAListaVazia_OutroNavegadorNaoTemNada()
    {
        string[] titles = await TitlesAsync().ConfigureAwait(false);
        IPage first = await VisitorAsync($"/busca?q={_token}").ConfigureAwait(false);
        await Heart(first, titles[0]).ClickAsync().ConfigureAwait(false);
        await Heart(first, titles[1]).ClickAsync().ConfigureAwait(false);
        await Expect(Counter(first)).ToHaveTextAsync("2").ConfigureAwait(false);
        string state = await first.Context.StorageStateAsync().ConfigureAwait(false);
        await first.Context.CloseAsync().ConfigureAwait(false);

        // S03: o navegador "voltou" com os dados guardados; a lista mostra os dois, na ordem em que foram favoritados
        IBrowserContext returning = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true, StorageState = state }).ConfigureAwait(false);
        IPage list = await VisitorAsync("/favoritos", existing: returning).ConfigureAwait(false);
        await Expect(list.GetByRole(AriaRole.Heading, new() { Name = "Meus favoritos", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(list.Locator("[data-favorites-count]")).ToHaveTextAsync("2 anúncios").ConfigureAwait(false);
        await Expect(FavoriteCards(list)).ToHaveCountAsync(2).ConfigureAwait(false);
        string[] names = [.. (await FavoriteCards(list).AllInnerTextsAsync().ConfigureAwait(false))];
        StringAssert.Contains(names[0], titles[0]);
        StringAssert.Contains(names[1], titles[1]);
        await Expect(list.Locator("[data-favorites-permanent-notice]")).ToContainTextAsync("apenas neste navegador").ConfigureAwait(false);
        await Expect(Counter(list)).ToHaveTextAsync("2").ConfigureAwait(false);
        Assert.AreEqual(0, await list.Locator("[data-favorite-toggle]:visible").CountAsync().ConfigureAwait(false), "na lista não há coração, há \"Remover\"");

        // S04: Remover tira o card, atualiza o total e o contador do topo
        await list.GetByRole(AriaRole.Button, new() { Name = $"Remover {titles[0]} dos favoritos" }).ClickAsync().ConfigureAwait(false);
        await Expect(FavoriteCards(list)).ToHaveCountAsync(1).ConfigureAwait(false);
        await Expect(list.Locator("[data-favorites-count]")).ToHaveTextAsync("1 anúncio").ConfigureAwait(false);
        await Expect(Counter(list)).ToHaveTextAsync("1").ConfigureAwait(false);
        Assert.AreEqual(1, (await StoredAsync(list).ConfigureAwait(false)).Length);

        // S05: a lista vazia leva à página inicial
        await list.GetByRole(AriaRole.Button, new() { Name = $"Remover {titles[1]} dos favoritos" }).ClickAsync().ConfigureAwait(false);
        await Expect(list.GetByText("Você ainda não favoritou nenhum anúncio")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Counter(list)).ToHaveTextAsync("0").ConfigureAwait(false);
        await Expect(list.Locator("[data-favorites-count]")).ToBeHiddenAsync().ConfigureAwait(false);
        await list.GetByRole(AriaRole.Link, new() { Name = "Ir para a página inicial" }).ClickAsync().ConfigureAwait(false);
        await Expect(list).ToHaveURLAsync(Url("/")).ConfigureAwait(false);
        await returning.CloseAsync().ConfigureAwait(false);

        // S08: outro navegador (contexto novo) não tem a lista, e o aviso de que ela fica só neste navegador continua lá
        IPage other = await VisitorAsync("/favoritos").ConfigureAwait(false);
        await Expect(other.GetByText("Você ainda não favoritou nenhum anúncio")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Counter(other)).ToHaveTextAsync("0").ConfigureAwait(false);
        await Expect(other.Locator("[data-favorites-permanent-notice]")).ToBeVisibleAsync().ConfigureAwait(false);
        await other.Context.CloseAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US005S06_US011S04_AnuncioArquivadoSaiDaLista_ComAvisoNoSingularENoPlural_ESaiDoArmazenamento()
    {
        string[] titles = await TitlesAsync().ConfigureAwait(false);
        using FakeViaCep viaCep = new();
        await EnsureAdminAsync().ConfigureAwait(false);
        string gone1 = await PublishingFlow.PublishAsync(Page, $"{_token} sairá um", 1).ConfigureAwait(false);
        string gone2 = await PublishingFlow.PublishAsync(Page, $"{_token} sairá dois", 1).ConfigureAwait(false);
        string gone3 = await PublishingFlow.PublishAsync(Page, $"{_token} sairá tres", 1).ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={_token}").ConfigureAwait(false);
        foreach (string title in new[] { titles[2], gone1, gone2, gone3 })
        {
            await Heart(visitor, title).ClickAsync().ConfigureAwait(false);
        }

        await Expect(Counter(visitor)).ToHaveTextAsync("4").ConfigureAwait(false);
        int keep = await IdOfAsync(visitor, titles[2]).ConfigureAwait(false);

        // Um anúncio sai do ar (o Administrador arquiva): aviso no singular
        await ArchiveAsync(gone1).ConfigureAwait(false);
        await visitor.GotoAsync(Url("/favoritos")).ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-unavailable]")).ToHaveTextAsync("1 anúncio favoritado deixou de estar disponível e foi removido da sua lista.").ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-count]")).ToHaveTextAsync("3 anúncios").ConfigureAwait(false);
        await Expect(FavoriteCards(visitor)).ToHaveCountAsync(3).ConfigureAwait(false);
        await Expect(visitor.GetByText(gone1)).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("3").ConfigureAwait(false);
        Assert.AreEqual(3, (await StoredAsync(visitor).ConfigureAwait(false)).Length, "o anúncio que saiu também sai do armazenamento");

        // Recarregar não repete o aviso: o anúncio já foi removido da lista
        await visitor.ReloadAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-count]")).ToHaveTextAsync("3 anúncios").ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-unavailable]")).ToBeHiddenAsync().ConfigureAwait(false);

        // Dois de uma vez: aviso no plural
        await ArchiveAsync(gone2).ConfigureAwait(false);
        await ArchiveAsync(gone3).ConfigureAwait(false);
        await visitor.ReloadAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-unavailable]")).ToHaveTextAsync("2 anúncios favoritados deixaram de estar disponíveis e foram removidos da sua lista.").ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-count]")).ToHaveTextAsync("1 anúncio").ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { keep }, await StoredAsync(visitor).ConfigureAwait(false));
        await visitor.Context.CloseAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US005S07_ArmazenamentoBloqueado_AvisaNaoSalvou_ECoracaoFicaVazio_ENaListaAvisaDoBloqueio()
    {
        string[] titles = await TitlesAsync().ConfigureAwait(false);
        const string blocking = "Storage.prototype.setItem = function () { throw new DOMException('bloqueado', 'QuotaExceededError'); };";
        IPage visitor = await VisitorAsync($"/busca?q={_token}", init: blocking).ConfigureAwait(false);

        await Heart(visitor, titles[0]).ClickAsync().ConfigureAwait(false);

        await Expect(visitor.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Não foi possível salvar seus favoritos neste navegador" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Heart(visitor, titles[0])).ToHaveAttributeAsync("aria-pressed", "false").ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("0").ConfigureAwait(false);

        await visitor.GotoAsync(Url("/favoritos")).ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-blocked]")).ToContainTextAsync("bloqueando o armazenamento").ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-empty]")).ToBeHiddenAsync().ConfigureAwait(false);
        await visitor.Context.CloseAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task OutraAba_FavoritarNumaAtualizaAOutra_SemRecarregar()
    {
        string[] titles = await TitlesAsync().ConfigureAwait(false);
        IPage tab1 = await VisitorAsync($"/busca?q={_token}").ConfigureAwait(false);
        IPage tab2 = await VisitorAsync($"/busca?q={_token}", existing: tab1.Context).ConfigureAwait(false);
        IPage list = await VisitorAsync("/favoritos", existing: tab1.Context).ConfigureAwait(false);
        await Expect(list.GetByText("Você ainda não favoritou nenhum anúncio")).ToBeVisibleAsync().ConfigureAwait(false);

        await Heart(tab1, titles[2]).ClickAsync().ConfigureAwait(false);

        await Expect(Counter(tab2)).ToHaveTextAsync("1").ConfigureAwait(false);
        await Expect(Heart(tab2, titles[2])).ToHaveAttributeAsync("aria-pressed", "true").ConfigureAwait(false);
        await Expect(FavoriteCards(list)).ToHaveCountAsync(1).ConfigureAwait(false);
        await Expect(list.Locator("[data-favorites-count]")).ToHaveTextAsync("1 anúncio").ConfigureAwait(false);
        await tab1.Context.CloseAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Teclado_EnterEEspacoFavoritam_ONomeDoCoracaoTemOTituloDoAnuncio()
    {
        string[] titles = await TitlesAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={_token}").ConfigureAwait(false);

        await Heart(visitor, titles[0]).FocusAsync().ConfigureAwait(false);
        await visitor.Keyboard.PressAsync("Enter").ConfigureAwait(false);
        await Expect(Heart(visitor, titles[0])).ToHaveAttributeAsync("aria-pressed", "true").ConfigureAwait(false);
        await Heart(visitor, titles[1]).FocusAsync().ConfigureAwait(false);
        await visitor.Keyboard.PressAsync("Space").ConfigureAwait(false);
        await Expect(Heart(visitor, titles[1])).ToHaveAttributeAsync("aria-pressed", "true").ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("2").ConfigureAwait(false);

        // O foco continua no coração depois de ativado (não some do teclado)
        await Expect(Heart(visitor, titles[1])).ToBeFocusedAsync().ConfigureAwait(false);
        await visitor.Context.CloseAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ArmazenamentoComLixo_SeAjusta_SemErro_EAOrdemEOsDuplicadosSaoTratados()
    {
        string[] titles = await TitlesAsync().ConfigureAwait(false);
        IPage seed = await VisitorAsync($"/busca?q={_token}").ConfigureAwait(false);
        int a = await IdOfAsync(seed, titles[0]).ConfigureAwait(false);
        int b = await IdOfAsync(seed, titles[1]).ConfigureAwait(false);
        int c = await IdOfAsync(seed, titles[2]).ConfigureAwait(false);

        // JSON quebrado: lista vazia, sem erro, e favoritar de novo funciona
        await seed.EvaluateAsync($"() => localStorage.setItem('{StorageKey}', '{{nao e json')").ConfigureAwait(false);
        await seed.GotoAsync(Url("/favoritos")).ConfigureAwait(false);
        await Expect(seed.GetByText("Você ainda não favoritou nenhum anúncio")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Counter(seed)).ToHaveTextAsync("0").ConfigureAwait(false);
        await Expect(seed.Locator("[data-favorites-error]")).ToBeHiddenAsync().ConfigureAwait(false);

        // Lista com repetidos, texto, decimal, negativo, nulo, objeto e número fora do limite: só os ids válidos, sem repetir
        await seed.EvaluateAsync($"() => localStorage.setItem('{StorageKey}', JSON.stringify([{a}, {a}, '{b}', 1.5, -4, null, {{x: 1}}, 99999999999, {b}]))").ConfigureAwait(false);
        await seed.ReloadAsync().ConfigureAwait(false);
        await Expect(FavoriteCards(seed)).ToHaveCountAsync(2).ConfigureAwait(false);
        await Expect(seed.Locator("[data-favorites-count]")).ToHaveTextAsync("2 anúncios").ConfigureAwait(false);
        await Expect(Counter(seed)).ToHaveTextAsync("2").ConfigureAwait(false);

        // O próximo favorito grava a lista já limpa
        await seed.GotoAsync(Url($"/busca?q={_token}")).ConfigureAwait(false);
        await Heart(seed, titles[2]).ClickAsync().ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { a, b, c }, await StoredAsync(seed).ConfigureAwait(false));

        // Um valor que não é lista (objeto) também vira lista vazia
        await seed.EvaluateAsync($"() => localStorage.setItem('{StorageKey}', '{{\"a\": 1}}')").ConfigureAwait(false);
        await seed.GotoAsync(Url("/favoritos")).ConfigureAwait(false);
        await Expect(seed.GetByText("Você ainda não favoritou nenhum anúncio")).ToBeVisibleAsync().ConfigureAwait(false);
        await seed.Context.CloseAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Celular320px_ListaSemRolagemHorizontal_ComBotoesDeTamanhoDeToque()
    {
        string[] titles = await TitlesAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={_token}", 320).ConfigureAwait(false);
        await Heart(visitor, titles[0]).ClickAsync().ConfigureAwait(false);
        await Heart(visitor, titles[1]).ClickAsync().ConfigureAwait(false);
        Assert.IsFalse(await HasHorizontalScrollAsync(visitor).ConfigureAwait(false), "busca com corações em 320 px");
        LocatorBoundingBoxResult box = (await Heart(visitor, titles[0]).BoundingBoxAsync().ConfigureAwait(false))!;
        Assert.IsTrue(box.Width >= 24 && box.Height >= 24, $"o coração tem {box.Width}x{box.Height}, o mínimo é 24x24");

        await visitor.GotoAsync(Url("/favoritos")).ConfigureAwait(false);
        await Expect(FavoriteCards(visitor)).ToHaveCountAsync(2).ConfigureAwait(false);
        Assert.IsFalse(await HasHorizontalScrollAsync(visitor).ConfigureAwait(false), "lista de favoritos em 320 px");
        LocatorBoundingBoxResult remove = (await visitor.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Remover") }).First.BoundingBoxAsync().ConfigureAwait(false))!;
        Assert.IsTrue(remove.Height >= 24 && remove.Width >= 24, $"\"Remover\" tem {remove.Width}x{remove.Height}");
        await visitor.Context.CloseAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SemJavaScript_NaoHaCoracaoNemBotao_ELista_PedeParaAtivarOJavaScript()
    {
        string[] titles = await TitlesAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync($"/busca?q={_token}", javaScript: false).ConfigureAwait(false);

        await Expect(visitor.Locator("[data-ad-card]")).ToHaveCountAsync(3).ConfigureAwait(false);
        Assert.AreEqual(0, await visitor.Locator("[data-favorite-toggle]:visible").CountAsync().ConfigureAwait(false), "sem JavaScript os corações não aparecem");

        await visitor.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^" + Regex.Escape(titles[0])) }).First.ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = titles[0], Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        Assert.AreEqual(0, await visitor.Locator("[data-favorite-toggle]:visible").CountAsync().ConfigureAwait(false), "a página do anúncio também não mostra o botão");

        await visitor.GotoAsync(Url("/favoritos")).ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = "Meus favoritos", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-permanent-notice]")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-no-js]")).ToContainTextAsync("Para ver seus favoritos, ative o JavaScript").ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-empty]")).ToBeHiddenAsync().ConfigureAwait(false);
        await visitor.Context.CloseAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Acessibilidade_Favoritos_SemViolacoes_NaBuscaNaPaginaDoAnuncioENaLista()
    {
        string[] titles = await TitlesAsync().ConfigureAwait(false);
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };

        foreach (int width in new[] { 1280, 320 })
        {
            IPage visitor = await VisitorAsync($"/busca?q={_token}", width).ConfigureAwait(false);
            await Heart(visitor, titles[0]).ClickAsync().ConfigureAwait(false);
            int id = await IdOfAsync(visitor, titles[0]).ConfigureAwait(false);

            // Busca com um coração marcado, a página do anúncio com o botão marcado, a lista com cards e a lista vazia
            foreach (string path in new[] { $"/busca?q={_token}", $"/anuncio/{id}/", "/favoritos" })
            {
                await visitor.GotoAsync(Url(path)).ConfigureAwait(false);
                await visitor.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
                if (path == "/favoritos")
                {
                    await Expect(FavoriteCards(visitor)).ToHaveCountAsync(1).ConfigureAwait(false);
                }

                AxeResult result = await visitor.RunAxe(options).ConfigureAwait(false);
                Assert.AreEqual(0, result.Violations.Length, $"{path} em {width}px: " + string.Join("; ", result.Violations.Select(Describe)));
            }

            await visitor.EvaluateAsync($"() => localStorage.removeItem('{StorageKey}')").ConfigureAwait(false);
            await visitor.GotoAsync(Url("/favoritos")).ConfigureAwait(false);
            await Expect(visitor.GetByText("Você ainda não favoritou nenhum anúncio")).ToBeVisibleAsync().ConfigureAwait(false);
            AxeResult empty = await visitor.RunAxe(options).ConfigureAwait(false);
            Assert.AreEqual(0, empty.Violations.Length, $"/favoritos vazio em {width}px: " + string.Join("; ", empty.Violations.Select(Describe)));
            await visitor.Context.CloseAsync().ConfigureAwait(false);
        }
    }
}
