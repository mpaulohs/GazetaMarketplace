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
/// A página do anúncio (US-003) no navegador, no site publicado: percorrer a galeria de 20 fotos pelas miniaturas, setas e teclado (S02), ampliar com Esc devolvendo o foco e a rolagem (S03),
/// uma foto só (S05), uma foto que não carrega (S07), 320 px sem rolagem horizontal e deslizar (S08), sem JavaScript e o axe. A conta do E2E (Administrador) publica os anúncios pelas telas.
/// Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class AdDetailE2ETests : SitePage
#pragma warning restore CA1515
{
    private const int Photos = 20;

    // O anúncio de 20 fotos é publicado uma vez e reaproveitado pelos testes da classe (enviar 20 fotos leva tempo)
    private static string _twentyTitle;
    private static string _twentyPath;
    private static string _singleTitle;
    private static string _singlePath;

    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static string Describe(AxeResultItem violation) =>
        violation.Id + ": " + violation.Help + " [" + string.Join(" | ", violation.Nodes.Take(3).Select(n => n.Html + " => " + string.Join("; ", n.Any.Select(a => a.Message)))) + "]";

    private async Task<string> TwentyAsync()
    {
        if (_twentyPath is null)
        {
            using FakeViaCep viaCep = new();
            await PublishingFlow.SignInAdminAsync(Page).ConfigureAwait(false);
            _twentyTitle = await PublishingFlow.PublishAsync(Page, "Livro com 20 fotos", Photos).ConfigureAwait(false);
            _twentyPath = await FindPathAsync(_twentyTitle).ConfigureAwait(false);
        }

        return _twentyPath;
    }

    private async Task<string> SingleAsync()
    {
        if (_singlePath is null)
        {
            using FakeViaCep viaCep = new();
            await PublishingFlow.SignInAdminAsync(Page).ConfigureAwait(false);
            _singleTitle = await PublishingFlow.PublishAsync(Page, "Livro com uma foto", 1).ConfigureAwait(false);
            _singlePath = await FindPathAsync(_singleTitle).ConfigureAwait(false);
        }

        return _singlePath;
    }

    // O endereço do anúncio publicado: o card dele é o primeiro da página inicial (os mais recentes vêm primeiro)
    private async Task<string> FindPathAsync(string title)
    {
        IBrowserContext context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true }).ConfigureAwait(false);
        IPage visitor = await context.NewPageAsync().ConfigureAwait(false);
        await visitor.GotoAsync(Url("/")).ConfigureAwait(false);
        string href = (await visitor.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^" + Regex.Escape(title)) }).First.GetAttributeAsync("href").ConfigureAwait(false))!;
        await context.CloseAsync().ConfigureAwait(false);
        return href;
    }

    private async Task<IPage> VisitorAsync(string path, int? width = null, bool javaScript = true)
    {
        IBrowserContext context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true, JavaScriptEnabled = javaScript, HasTouch = false }).ConfigureAwait(false);
        IPage visitor = await context.NewPageAsync().ConfigureAwait(false);
        if (width is { } w)
        {
            await visitor.SetViewportSizeAsync(w, 800).ConfigureAwait(false);
        }

        await visitor.GotoAsync(Url(path)).ConfigureAwait(false);
        return visitor;
    }

    private static ILocator Counter(IPage page) => page.Locator("[data-gallery-counter]");

    private static ILocator Thumb(IPage page, int number) => page.GetByRole(AriaRole.Link, new() { Name = $"Ver a foto {number} de {Photos}" });

    [TestMethod]
    public async Task US003S02_Galeria20Fotos_QuintaMiniaturaTeclaSetaEVoltarAoComeco()
    {
        string path = await TwentyAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync(path).ConfigureAwait(false);

        await Expect(Counter(visitor)).ToHaveTextAsync("1 de 20").ConfigureAwait(false);
        Assert.AreEqual(Photos, await visitor.Locator("[data-gallery-thumb]").CountAsync().ConfigureAwait(false));
        await Thumb(visitor, 5).ClickAsync().ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("5 de 20").ConfigureAwait(false);
        await Expect(visitor.Locator("[data-gallery-main]")).ToHaveAttributeAsync("alt", new Regex(@"^Foto 5 de 20: ")).ConfigureAwait(false);
        await Expect(Thumb(visitor, 5)).ToHaveAttributeAsync("aria-current", "true").ConfigureAwait(false);
        string fifth = (await visitor.Locator("[data-gallery-main]").GetAttributeAsync("src").ConfigureAwait(false))!;

        await visitor.Keyboard.PressAsync("ArrowRight").ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("6 de 20").ConfigureAwait(false);
        Assert.AreNotEqual(fifth, await visitor.Locator("[data-gallery-main]").GetAttributeAsync("src").ConfigureAwait(false), "a foto em destaque mudou");
        await Expect(Thumb(visitor, 5)).Not.ToHaveAttributeAsync("aria-current", "true").ConfigureAwait(false);
        await visitor.Keyboard.PressAsync("ArrowLeft").ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("5 de 20").ConfigureAwait(false);

        await visitor.GetByRole(AriaRole.Button, new() { Name = "Próxima foto" }).First.ClickAsync().ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("6 de 20").ConfigureAwait(false);
        await Thumb(visitor, 1).ClickAsync().ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Button, new() { Name = "Foto anterior" }).First.ClickAsync().ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("20 de 20").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US003S02_NaCarga_SoAPrimeiraFotoGrandeEBuscada_AsDemaisSoQuandoEscolhidas()
    {
        string path = await TwentyAsync().ConfigureAwait(false);
        IBrowserContext context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true }).ConfigureAwait(false);
        IPage visitor = await context.NewPageAsync().ConfigureAwait(false);
        List<string> large = [];
        visitor.Request += (_, request) =>
        {
            if (request.Url.EndsWith("-1600.webp", StringComparison.Ordinal))
            {
                large.Add(request.Url);
            }
        };

        await visitor.GotoAsync(Url(path)).ConfigureAwait(false);
        await visitor.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);

        Assert.AreEqual(1, large.Distinct().Count(), "só a foto em destaque");
        await Thumb(visitor, 7).ClickAsync().ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("7 de 20").ConfigureAwait(false);
        await visitor.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        Assert.AreEqual(2, large.Distinct().Count(), "a sétima só foi buscada quando escolhida");
    }

    [TestMethod]
    public async Task US003S03_Ampliar_ComFechar_EscDevolveOFocoEAPaginaFicaNoMesmoPonto()
    {
        string path = await TwentyAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync(path).ConfigureAwait(false);
        await Thumb(visitor, 5).ClickAsync().ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("5 de 20").ConfigureAwait(false);
        await visitor.EvaluateAsync("window.scrollTo(0, 60)").ConfigureAwait(false);

        await visitor.Locator("[data-gallery-open]").ClickAsync().ConfigureAwait(false);

        ILocator dialog = visitor.GetByRole(AriaRole.Dialog);
        await Expect(dialog).ToBeVisibleAsync().ConfigureAwait(false);
        double before = await visitor.EvaluateAsync<double>("window.scrollY").ConfigureAwait(false); // o ponto em que o visitante estava ao abrir
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Fechar" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(dialog.GetByRole(AriaRole.Heading, new() { Name = "Foto 5 de 20" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(dialog.Locator("img")).ToHaveAttributeAsync("alt", new Regex(@"^Foto 5 de 20: ")).ConfigureAwait(false);
        bool focusInside = await visitor.EvaluateAsync<bool>("document.querySelector('dialog').contains(document.activeElement)").ConfigureAwait(false);
        Assert.IsTrue(focusInside, "o foco entra na janela");
        await visitor.Keyboard.PressAsync("ArrowRight").ConfigureAwait(false);
        await Expect(dialog.GetByRole(AriaRole.Heading, new() { Name = "Foto 6 de 20" })).ToBeVisibleAsync().ConfigureAwait(false);
        await visitor.Keyboard.PressAsync("ArrowLeft").ConfigureAwait(false);
        await Expect(dialog.GetByRole(AriaRole.Heading, new() { Name = "Foto 5 de 20" })).ToBeVisibleAsync().ConfigureAwait(false);

        await visitor.Keyboard.PressAsync("Escape").ConfigureAwait(false);

        await Expect(dialog).Not.ToBeVisibleAsync().ConfigureAwait(false);
        Assert.AreEqual(before, await visitor.EvaluateAsync<double>("window.scrollY").ConfigureAwait(false), 1, "a página volta ao mesmo ponto");
        bool focusBack = await visitor.EvaluateAsync<bool>("document.activeElement === document.querySelector('[data-gallery-open]')").ConfigureAwait(false);
        Assert.IsTrue(focusBack, "o foco volta à foto de onde a janela abriu");
        await Expect(Counter(visitor)).ToHaveTextAsync("5 de 20").ConfigureAwait(false);

        await visitor.Locator("[data-gallery-open]").ClickAsync().ConfigureAwait(false);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Fechar" }).ClickAsync().ConfigureAwait(false);
        await Expect(dialog).Not.ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US003S05_UmaUnicaFoto_SemMiniaturasNemSetas_MasAmplia()
    {
        string path = await SingleAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync(path).ConfigureAwait(false);

        await Expect(visitor.Locator("[data-gallery-main]")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("[data-gallery-thumb]")).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^(Foto anterior|Próxima foto)$") })).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(visitor.Locator("[data-gallery-counter]")).ToHaveCountAsync(0).ConfigureAwait(false);
        await visitor.Locator("[data-gallery-open]").ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync().ConfigureAwait(false);
        await visitor.Keyboard.PressAsync("Escape").ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Dialog)).Not.ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US003S07_UmaFotoNaoCarrega_VeFotoIndisponivel_EContinuaNavegandoPelasOutras()
    {
        string path = await TwentyAsync().ConfigureAwait(false);
        IBrowserContext context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true }).ConfigureAwait(false);
        IPage visitor = await context.NewPageAsync().ConfigureAwait(false);
        await visitor.GotoAsync(Url(path)).ConfigureAwait(false);
        string brokenLarge = (await Thumb(visitor, 2).GetAttributeAsync("data-large").ConfigureAwait(false))!;
        await visitor.RouteAsync("**/*-1600.webp", route =>
            route.Request.Url.EndsWith(brokenLarge, StringComparison.Ordinal) ? route.AbortAsync() : route.ContinueAsync()).ConfigureAwait(false);

        await Thumb(visitor, 2).ClickAsync().ConfigureAwait(false);

        await Expect(visitor.GetByRole(AriaRole.Status).Filter(new() { HasText = "Foto indisponível" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("[data-gallery-main]")).Not.ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("2 de 20").ConfigureAwait(false);
        await Thumb(visitor, 3).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("[data-gallery-main]")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("[data-gallery-broken]")).Not.ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("3 de 20").ConfigureAwait(false);
        await visitor.Keyboard.PressAsync("ArrowLeft").ConfigureAwait(false);
        await Expect(visitor.Locator("[data-gallery-broken]")).ToBeVisibleAsync().ConfigureAwait(false);
        await visitor.Keyboard.PressAsync("ArrowLeft").ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("1 de 20").ConfigureAwait(false);
        await Expect(visitor.Locator("[data-gallery-main]")).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US003S08_Celular320px_SemRolagemHorizontal_DeslizarTrocaDeFoto_MiniaturasRolamNaPropriaFaixa()
    {
        string path = await TwentyAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync(path, 320).ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("1 de 20").ConfigureAwait(false);

        Assert.IsFalse(await visitor.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth").ConfigureAwait(false), "a página não rola na horizontal em 320 px");
        bool stripScrolls = await visitor.EvaluateAsync<bool>("(() => { const s = document.querySelector('[data-gallery-thumbs]'); return s.scrollWidth > s.clientWidth; })()").ConfigureAwait(false);
        Assert.IsTrue(stripScrolls, "as 20 miniaturas rolam dentro da própria faixa");

        LocatorBoundingBoxResult stage = (await visitor.Locator("[data-gallery-stage]").BoundingBoxAsync().ConfigureAwait(false))!;
        // O gesto começa na parte de cima da foto, longe das setas (que ficam no meio, nas bordas)
        double y = stage.Y + stage.Height * 0.2;
        await visitor.Mouse.MoveAsync((float)(stage.X + stage.Width * 0.75), (float)y).ConfigureAwait(false);
        await visitor.Mouse.DownAsync().ConfigureAwait(false);
        await visitor.Mouse.MoveAsync((float)(stage.X + stage.Width * 0.2), (float)y, new() { Steps = 6 }).ConfigureAwait(false);
        await visitor.Mouse.UpAsync().ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("2 de 20").ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Dialog)).Not.ToBeVisibleAsync().ConfigureAwait(false);

        foreach (string name in new[] { "Foto anterior", "Próxima foto" })
        {
            LocatorBoundingBoxResult button = (await visitor.GetByRole(AriaRole.Button, new() { Name = name }).First.BoundingBoxAsync().ConfigureAwait(false))!;
            Assert.IsTrue(button.Width >= 44 && button.Height >= 44, $"{name} tem alvo de toque de 44 px");
            Assert.IsTrue(button.X >= 0 && button.X + button.Width <= 320.5, $"{name} cabe na tela");
        }

        await visitor.GetByRole(AriaRole.Button, new() { Name = "Próxima foto" }).First.ClickAsync().ConfigureAwait(false);
        await Expect(Counter(visitor)).ToHaveTextAsync("3 de 20").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SemJavaScript_FotoEmDestaqueEMiniaturasSaoLinksParaAVersaoGrande()
    {
        string path = await TwentyAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync(path, javaScript: false).ConfigureAwait(false);

        await Expect(visitor.Locator("[data-gallery-main]")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("[data-gallery-prev]")).Not.ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("[data-gallery-counter]")).Not.ToBeVisibleAsync().ConfigureAwait(false);
        Assert.AreEqual(Photos, await visitor.Locator("[data-gallery-thumb]").CountAsync().ConfigureAwait(false));
        string href = (await Thumb(visitor, 5).GetAttributeAsync("href").ConfigureAwait(false))!;
        IResponse response = (await visitor.GotoAsync(Url(href)).ConfigureAwait(false))!;
        Assert.AreEqual(200, response.Status);
        StringAssert.StartsWith(response.Headers["content-type"], "image/");
    }

    [TestMethod]
    public async Task Endereco_SlugAntigo_O301LevaAoEnderecoAtual()
    {
        string path = await SingleAsync().ConfigureAwait(false);
        string id = Regex.Match(path, @"/anuncio/(\d+)/").Groups[1].Value;
        IBrowserContext context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true }).ConfigureAwait(false);
        IPage visitor = await context.NewPageAsync().ConfigureAwait(false);

        await visitor.GotoAsync(Url($"/anuncio/{id}/titulo-antigo")).ConfigureAwait(false);

        await Expect(visitor).ToHaveURLAsync(Url(path)).ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = _singleTitle, Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Indisponivel_EnderecoQueNaoExiste_Da404ComAMensagemEOsCaminhosDeVolta()
    {
        IBrowserContext context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true }).ConfigureAwait(false);
        IPage visitor = await context.NewPageAsync().ConfigureAwait(false);

        IResponse response = (await visitor.GotoAsync(Url("/anuncio/2000000000/nao-existe")).ConfigureAwait(false))!;

        Assert.AreEqual(404, response.Status);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = "Este anúncio não está mais disponível", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Link, new() { Name = "página inicial" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Navigation, new() { Name = "Categorias principais" })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Acessibilidade_PaginaDoAnuncio_SemViolacoes_EmDesktopECelular_InclusiveComAJanelaAberta()
    {
        string twenty = await TwentyAsync().ConfigureAwait(false);
        string single = await SingleAsync().ConfigureAwait(false);
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };

        foreach (int width in new[] { 1280, 320 })
        {
            foreach (string path in new[] { twenty, single, "/anuncio/2000000000/nao-existe" })
            {
                IPage visitor = await VisitorAsync(path, width).ConfigureAwait(false);
                await visitor.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
                AxeResult result = await visitor.RunAxe(options).ConfigureAwait(false);
                Assert.AreEqual(0, result.Violations.Length, $"{path} em {width}px: " + string.Join("; ", result.Violations.Select(Describe)));
            }
        }

        IPage withDialog = await VisitorAsync(twenty, 1280).ConfigureAwait(false);
        await Thumb(withDialog, 3).ClickAsync().ConfigureAwait(false);
        await withDialog.Locator("[data-gallery-open]").ClickAsync().ConfigureAwait(false);
        await Expect(withDialog.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync().ConfigureAwait(false);
        AxeResult dialogResult = await withDialog.RunAxe(options).ConfigureAwait(false);
        Assert.AreEqual(0, dialogResult.Violations.Length, "janela ampliada: " + string.Join("; ", dialogResult.Violations.Select(Describe)));
    }
}
