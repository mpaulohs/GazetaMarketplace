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
/// O contato do intermediário na página do anúncio (US-004) no navegador, no site publicado: o visitante sem login vê o número e os dois botões (S01 a S03), a mensagem do WhatsApp chega
/// com o título exato (S04), o WhatsApp abre em nova aba e a página do anúncio continua aberta (S05), sem chamada ao servidor do site, e o bloco serve no celular de 320 px e sem JavaScript.
/// O telefone do site é o que estiver configurado no banco de teste: os testes leem o número escrito na tela e conferem os links contra ele.
/// Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class ContactE2ETests : SitePage
#pragma warning restore CA1515
{
    private const string SpecialPrefix = "Sítio \"Boa Vista\" & Cia";

    private static string _plainTitle;
    private static string _plainPath;
    private static string _specialTitle;
    private static string _specialPath;

    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static string Describe(AxeResultItem violation) =>
        violation.Id + ": " + violation.Help + " [" + string.Join(" | ", violation.Nodes.Take(3).Select(n => n.Html + " => " + string.Join("; ", n.Any.Select(a => a.Message)))) + "]";

    private async Task<string> PlainAsync()
    {
        if (_plainPath is null)
        {
            using FakeViaCep viaCep = new();
            await PublishingFlow.SignInAdminAsync(Page).ConfigureAwait(false);
            _plainTitle = await PublishingFlow.PublishAsync(Page, "Livro para contato", 1).ConfigureAwait(false);
            _plainPath = await PublishingFlow.PublicPathAsync(Browser, _plainTitle).ConfigureAwait(false);
        }

        return _plainPath;
    }

    private async Task<string> SpecialAsync()
    {
        if (_specialPath is null)
        {
            using FakeViaCep viaCep = new();
            await PublishingFlow.SignInAdminAsync(Page).ConfigureAwait(false);
            _specialTitle = await PublishingFlow.PublishAsync(Page, SpecialPrefix, 1).ConfigureAwait(false);
            _specialPath = await PublishingFlow.PublicPathAsync(Browser, _specialTitle).ConfigureAwait(false);
        }

        return _specialPath;
    }

    // Um visitante sem login, com a conta fechada: contexto novo, sem cookie
    private async Task<IPage> VisitorAsync(string path, int? width = null, bool javaScript = true)
    {
        IBrowserContext context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true, JavaScriptEnabled = javaScript }).ConfigureAwait(false);
        IPage visitor = await context.NewPageAsync().ConfigureAwait(false);
        if (width is { } w)
        {
            await visitor.SetViewportSizeAsync(w, 800).ConfigureAwait(false);
        }

        await visitor.GotoAsync(Url(path)).ConfigureAwait(false);
        return visitor;
    }

    private static ILocator Call(IPage page) => page.GetByRole(AriaRole.Link, new() { Name = "Ligar" });

    private static ILocator WhatsApp(IPage page) => page.GetByRole(AriaRole.Link, new() { Name = "Chamar no WhatsApp" });

    // Os dígitos do número escrito na tela: "(11) 91234-5678" -> "11912345678"
    private static async Task<string> WrittenDigitsAsync(IPage page)
    {
        string text = (await page.Locator("[data-phone]").InnerTextAsync().ConfigureAwait(false)).Trim();
        StringAssert.Matches(text, new Regex(@"^\(\d{2}\) \d{4,5}-\d{4}$"));
        return Regex.Replace(text, @"\D", string.Empty);
    }

    // O texto da conversa, lido pelo navegador do jeito que o WhatsApp lê
    private static async Task<string> MessageOfAsync(IPage page, string href) =>
        (await page.EvaluateAsync<string>("href => new URL(href).searchParams.get('text')", href).ConfigureAwait(false))!;

    private static async Task<int> SameOriginRequestsDuringAsync(IPage page, Func<Task> action)
    {
        string origin = new Uri(Url("/")).GetLeftPart(UriPartial.Authority);
        int count = 0;
        void Count(object sender, IRequest request)
        {
            if (request.Url.StartsWith(origin, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        page.Request += Count;
        try
        {
            await action().ConfigureAwait(false);
            await page.WaitForTimeoutAsync(500).ConfigureAwait(false);
        }
        finally
        {
            page.Request -= Count;
        }

        return count;
    }

    [TestMethod]
    public async Task US004S01_S02_S03_SemLogin_VeONumeroEOsDoisBotoes_ComALigacaoEOWhatsAppProntos()
    {
        string path = await PlainAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync(path).ConfigureAwait(false);

        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = "Fale com a Gazeta" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Call(visitor)).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(WhatsApp(visitor)).ToBeVisibleAsync().ConfigureAwait(false);
        string digits = await WrittenDigitsAsync(visitor).ConfigureAwait(false);

        // S02: o link de ligar leva o número pronto para discar
        Assert.AreEqual($"tel:+55{digits}", await Call(visitor).GetAttributeAsync("href").ConfigureAwait(false));

        // S01: o WhatsApp abre a conversa com o número do intermediário e a mensagem com o título e o endereço da página
        string href = (await WhatsApp(visitor).GetAttributeAsync("href").ConfigureAwait(false))!;
        StringAssert.StartsWith(href, $"https://wa.me/55{digits}?text=");
        Assert.AreEqual($"Olá! Tenho interesse no anúncio “{_plainTitle}”: {Url(path)}", await MessageOfAsync(visitor, href).ConfigureAwait(false));
    }

    [TestMethod]
    public async Task US004S04_TituloComAcentosAspasEECerca_AMensagemMostraOTituloExato()
    {
        string path = await SpecialAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync(path).ConfigureAwait(false);

        string href = (await WhatsApp(visitor).GetAttributeAsync("href").ConfigureAwait(false))!;

        Assert.AreEqual($"Olá! Tenho interesse no anúncio “{_specialTitle}”: {Url(path)}", await MessageOfAsync(visitor, href).ConfigureAwait(false));
        StringAssert.Contains(await MessageOfAsync(visitor, href).ConfigureAwait(false), "Sítio \"Boa Vista\" & Cia");
        Assert.AreEqual(1, await visitor.EvaluateAsync<int>("href => [...new URL(href).searchParams.keys()].length", href).ConfigureAwait(false), "o & do título não virou outro parâmetro");
    }

    [TestMethod]
    public async Task US004S05_WhatsApp_AbreEmNovaAba_AAbaDoAnuncioContinuaAbertaESemChamadaAoSite()
    {
        string path = await PlainAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync(path).ConfigureAwait(false);
        // O WhatsApp de verdade é de fora: a aba nova recebe uma página simulada, só para provar para onde o clique foi
        await visitor.Context.RouteAsync("https://wa.me/**", route => route.FulfillAsync(new RouteFulfillOptions { ContentType = "text/html", Body = "<title>WhatsApp simulado</title><p>conversa</p>" })).ConfigureAwait(false);
        string href = (await WhatsApp(visitor).GetAttributeAsync("href").ConfigureAwait(false))!;
        IPage popup = null!;

        int requestsToTheSite = await SameOriginRequestsDuringAsync(visitor, async () =>
        {
            popup = await visitor.RunAndWaitForPopupAsync(() => WhatsApp(visitor).ClickAsync()).ConfigureAwait(false);
            await popup.WaitForLoadStateAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        Assert.AreEqual(href, popup.Url, "a nova aba abriu o link do WhatsApp");
        Assert.IsNull(await popup.EvaluateAsync<string>("() => window.opener === null ? null : 'tem opener'").ConfigureAwait(false), "a aba nova não controla a do anúncio (noopener)");
        Assert.AreEqual(Url(path), visitor.Url, "a página do anúncio continua aberta na aba anterior");
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync(_plainTitle).ConfigureAwait(false);
        Assert.AreEqual(0, requestsToTheSite, "clicar no contato não faz nenhuma chamada ao servidor do site (o site não registra o clique)");
    }

    [TestMethod]
    public async Task Celular320px_ContatoDepoisDaDescricao_BotoesDeLarguraTotalEAltura44_SemRolagemHorizontal()
    {
        string path = await PlainAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync(path, 320).ConfigureAwait(false);

        LocatorBoundingBoxResult description = await visitor.Locator(".ad-corpo__descricao").BoundingBoxAsync().ConfigureAwait(false);
        LocatorBoundingBoxResult block = await visitor.Locator("[data-contact]").BoundingBoxAsync().ConfigureAwait(false);
        LocatorBoundingBoxResult call = await Call(visitor).BoundingBoxAsync().ConfigureAwait(false);
        LocatorBoundingBoxResult whatsApp = await WhatsApp(visitor).BoundingBoxAsync().ConfigureAwait(false);

        Assert.IsTrue(block.Y > description.Y, "no celular o contato vem depois da descrição");
        Assert.IsTrue(call.Height >= 44 && whatsApp.Height >= 44, $"alvos de toque de 44 px: {call.Height} e {whatsApp.Height}");
        Assert.IsTrue(call.Width >= block.Width * 0.8 && whatsApp.Width >= block.Width * 0.8, "os botões ocupam a largura do bloco");
        Assert.IsTrue(call.Y + call.Height <= whatsApp.Y + 1, "um botão sobre o outro");
        Assert.IsFalse(await visitor.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth").ConfigureAwait(false), "sem rolagem horizontal em 320 px");
    }

    [TestMethod]
    public async Task Desktop_ContatoNaColunaDaDireita_AoLadoDoCorpoDoAnuncio()
    {
        string path = await PlainAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync(path, 1280).ConfigureAwait(false);

        LocatorBoundingBoxResult body = await visitor.Locator("[data-ad-body]").BoundingBoxAsync().ConfigureAwait(false);
        LocatorBoundingBoxResult block = await visitor.Locator("[data-contact]").BoundingBoxAsync().ConfigureAwait(false);

        Assert.IsTrue(block.X >= body.X + body.Width - 1, "no desktop o contato fica à direita");
    }

    [TestMethod]
    public async Task SemJavaScript_OContatoFunciona_SaoLinksComuns()
    {
        string path = await PlainAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync(path, javaScript: false).ConfigureAwait(false);

        await Expect(Call(visitor)).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(WhatsApp(visitor)).ToBeVisibleAsync().ConfigureAwait(false);
        string digits = await WrittenDigitsAsync(visitor).ConfigureAwait(false);
        Assert.AreEqual($"tel:+55{digits}", await Call(visitor).GetAttributeAsync("href").ConfigureAwait(false));
        StringAssert.StartsWith((await WhatsApp(visitor).GetAttributeAsync("href").ConfigureAwait(false))!, $"https://wa.me/55{digits}?text=");
        Assert.AreEqual("_blank", await WhatsApp(visitor).GetAttributeAsync("target").ConfigureAwait(false));
    }

    [TestMethod]
    public async Task Teclado_OsDoisBotoesRecebemFocoComContornoVisivel()
    {
        string path = await PlainAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync(path).ConfigureAwait(false);

        await Call(visitor).FocusAsync().ConfigureAwait(false);
        await Expect(Call(visitor)).ToBeFocusedAsync().ConfigureAwait(false);
        string focusStyle = await Call(visitor).EvaluateAsync<string>("e => getComputedStyle(e).outlineStyle + '|' + getComputedStyle(e).boxShadow").ConfigureAwait(false);
        Assert.IsFalse(focusStyle.StartsWith("none|none", StringComparison.Ordinal), "o foco tem contorno ou sombra visível: " + focusStyle);
        await visitor.Keyboard.PressAsync("Tab").ConfigureAwait(false);
        await Expect(WhatsApp(visitor)).ToBeFocusedAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Acessibilidade_ContatoSemViolacoes_EmDesktopECelular()
    {
        string path = await SpecialAsync().ConfigureAwait(false);
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };

        foreach (int width in new[] { 1280, 320 })
        {
            IPage visitor = await VisitorAsync(path, width).ConfigureAwait(false);
            await Expect(WhatsApp(visitor)).ToBeVisibleAsync().ConfigureAwait(false);
            await visitor.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
            AxeResult result = await visitor.RunAxe(options).ConfigureAwait(false);
            Assert.AreEqual(0, result.Violations.Length, $"{path} em {width}px: " + string.Join("; ", result.Violations.Select(Describe)));
        }
    }
}
