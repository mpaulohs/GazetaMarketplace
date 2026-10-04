using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Components;

/// <summary>
/// Os três cards e o corpo do anúncio (3.8) no navegador, na página de componentes do painel. Essa página só existe em Development, então o teste usa um segundo site
/// subido em Development (GAZETA_DEV_BASE_URL) ligado ao mesmo banco do E2E; a conta é a de Administrador (GAZETA_E2E_EMAIL e GAZETA_E2E_PASSWORD).
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_DEV_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class ComponentsE2ETests : SitePage
#pragma warning restore CA1515
{
    private const string CivicName = "Honda Civic 2018 automático, único dono, revisões na concessionária, R$ 62.000, Campinas/SP";

    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_DEV_BASE_URL").TrimEnd('/') + path;

    private static string Describe(AxeResultItem violation) =>
        violation.Id + ": " + violation.Help + " [" + string.Join(" | ", violation.Nodes.Take(3).Select(n => n.Html + " => " + string.Join("; ", n.Any.Select(a => a.Message)))) + "]";

    private async Task OpenAsync(int width = 1280)
    {
        await Page.SetViewportSizeAsync(width, 900).ConfigureAwait(false);
        await Page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await Page.GetByLabel("E-mail").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL")).ConfigureAwait(false);
        await Page.GetByLabel("Senha").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
        await Page.WaitForURLAsync(new Regex(@"/painel/(?!entrar)")).ConfigureAwait(false);
        await Page.GotoAsync(Url("/painel/componentes")).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Componentes do anúncio", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    private ILocator Card(int index) => Page.Locator("[data-ad-card]").Nth(index);

    private static async Task<(double Width, double Height)> BoxAsync(ILocator locator)
    {
        LocatorBoundingBoxResult box = (await locator.BoundingBoxAsync().ConfigureAwait(false))!;
        return (box.Width, box.Height);
    }

    [TestMethod]
    public async Task TresVariantes_LadoALado_MesmaAlturaDeMidia_ENomeAcessivelDoLink()
    {
        await OpenAsync().ConfigureAwait(false);

        await Expect(Page.Locator("[data-ad-card]")).ToHaveCountAsync(4).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = CivicName, Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Diarista com experiência, atendo toda a região, Tipo: Serviços domésticos, São Paulo/SP", Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Pizzaiolo com experiência, período integral, Salário R$ 2.800, Campinas/SP", Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Violão Giannini, com capa, cordas novas, R$ 2.499,90, Goiânia/GO", Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);

        // Padrão: a capa carregou e tem 4:3; Serviços também; Vagas e sem foto mostram o bloco neutro do mesmo tamanho
        await Expect(Card(0).Locator("img")).ToBeVisibleAsync().ConfigureAwait(false);
        (double width, double height) = await BoxAsync(Card(0).Locator("img")).ConfigureAwait(false);
        Assert.AreEqual(4d / 3d, width / height, 0.02, "capa em 4:3");
        (double serviceWidth, double serviceHeight) = await BoxAsync(Card(1).Locator("img")).ConfigureAwait(false);
        (double jobWidth, double jobHeight) = await BoxAsync(Card(2).Locator("[data-ad-placeholder]")).ConfigureAwait(false);
        (double noPhotoWidth, double noPhotoHeight) = await BoxAsync(Card(3).Locator("[data-ad-placeholder]")).ConfigureAwait(false);
        Assert.AreEqual(width, serviceWidth, 1);
        Assert.AreEqual(height, serviceHeight, 1);
        Assert.AreEqual(width, jobWidth, 1, "o bloco da vaga tem a largura da foto");
        Assert.AreEqual(height, jobHeight, 1, "e a altura da foto");
        Assert.AreEqual(height, noPhotoHeight, 1);
        Assert.AreEqual(width, noPhotoWidth, 1);

        await Expect(Card(2)).ToContainTextAsync("Vaga de emprego").ConfigureAwait(false);
        await Expect(Card(2)).ToContainTextAsync("Alimentação e restaurantes").ConfigureAwait(false);
        await Expect(Card(2)).ToContainTextAsync("Salário").ConfigureAwait(false);
        await Expect(Card(2).Locator("img")).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Card(1)).Not.ToContainTextAsync("R$").ConfigureAwait(false);
        await Expect(Card(3)).ToContainTextAsync("Foto indisponível").ConfigureAwait(false);
        await Expect(Card(0).Locator("img")).ToHaveAttributeAsync("loading", "eager").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FotoQueNaoCarrega_ViraFotoIndisponivel_NoMesmoTamanho()
    {
        await Page.RouteAsync("**/images/componentes/capa-exemplo.svg", route => route.AbortAsync()).ConfigureAwait(false);
        await OpenAsync().ConfigureAwait(false);

        await Expect(Page.Locator("[data-ad-card] img")).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Page.Locator("[data-ad-card]").GetByText("Foto indisponível")).ToHaveCountAsync(3).ConfigureAwait(false);
        (double width, double height) = await BoxAsync(Card(0).Locator("[data-ad-placeholder]")).ConfigureAwait(false);
        (double jobWidth, double jobHeight) = await BoxAsync(Card(2).Locator("[data-ad-placeholder]")).ConfigureAwait(false);
        Assert.AreEqual(jobWidth, width, 1);
        Assert.AreEqual(jobHeight, height, 1, "a página não pula quando a foto falha");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = CivicName, Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Foco_ContornoNoCardInteiro_EHover_SublinhaOTitulo()
    {
        await OpenAsync().ConfigureAwait(false);
        ILocator link = Page.GetByRole(AriaRole.Link, new() { Name = CivicName, Exact = true });

        await link.FocusAsync().ConfigureAwait(false);
        await Page.Keyboard.PressAsync("Shift+Tab").ConfigureAwait(false);
        await Page.Keyboard.PressAsync("Tab").ConfigureAwait(false);
        string outline = await link.EvaluateAsync<string>("el => getComputedStyle(el, '::after').outlineStyle").ConfigureAwait(false);
        Assert.AreEqual("solid", outline, "o contorno do foco está no card inteiro (::after do link)");
        string own = await link.EvaluateAsync<string>("el => getComputedStyle(el).outlineStyle").ConfigureAwait(false);
        Assert.AreEqual("none", own, "o link em si não desenha um segundo contorno");

        await Page.Mouse.MoveAsync(0, 0).ConfigureAwait(false);
        await Expect(link).Not.ToHaveCSSAsync("text-decoration-line", "underline").ConfigureAwait(false);
        await Card(0).HoverAsync().ConfigureAwait(false);
        await Expect(link).ToHaveCSSAsync("text-decoration-line", "underline").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Acessibilidade_SemViolacoes_ENaoRolaNaHorizontal_NosQuatroTamanhos()
    {
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };
        await OpenAsync().ConfigureAwait(false);

        foreach (int width in new[] { 1280, 1024, 768, 320 })
        {
            await Page.SetViewportSizeAsync(width, 900).ConfigureAwait(false);
            await Page.Mouse.MoveAsync(0, 0).ConfigureAwait(false);
            AxeResult result = await Page.RunAxe(options).ConfigureAwait(false);
            Assert.AreEqual(0, result.Violations.Length, $"{width}px: " + string.Join("; ", result.Violations.Select(Describe)));
            Assert.IsFalse(
                await Page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth").ConfigureAwait(false),
                $"sem rolagem horizontal em {width} px");
        }

        // 2 colunas em 320 e 768 px, 3 em 1024 e 4 em 1280 (design-system §7)
        await Page.SetViewportSizeAsync(1280, 900).ConfigureAwait(false);
        double first = (await BoxAsync(Card(0)).ConfigureAwait(false)).Width;
        double perRow = Math.Round((await Page.Locator("[data-componentes-cards]").BoundingBoxAsync().ConfigureAwait(false))!.Width / first);
        Assert.AreEqual(4d, perRow, "4 cards por linha em 1280 px");
        await Page.SetViewportSizeAsync(320, 900).ConfigureAwait(false);
        first = (await BoxAsync(Card(0)).ConfigureAwait(false)).Width;
        perRow = Math.Round((await Page.Locator("[data-componentes-cards]").BoundingBoxAsync().ConfigureAwait(false))!.Width / first);
        Assert.AreEqual(2d, perRow, "2 cards por linha em 320 px");
    }

    [TestMethod]
    public async Task Corpo_TituloEmH3_DescricaoComQuebrasECaracteristicas()
    {
        await OpenAsync().ConfigureAwait(false);
        ILocator body = Page.Locator("[data-ad-body]");

        await Expect(body.GetByRole(AriaRole.Heading, new() { Name = "Honda Civic 2018 automático", Level = 3 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(body).ToContainTextAsync("R$ 62.000").ConfigureAwait(false);
        await Expect(body).ToContainTextAsync("Campinas/SP").ConfigureAwait(false);
        await Expect(body.GetByRole(AriaRole.Heading, new() { Name = "Características" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(body.Locator(".ad-corpo__descricao")).ToHaveCSSAsync("white-space", "pre-line").ConfigureAwait(false);
        double oneLine = Convert.ToDouble(await body.Locator(".ad-corpo__descricao").EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).lineHeight)").ConfigureAwait(false), CultureInfo.InvariantCulture);
        Assert.IsGreaterThanOrEqualTo(oneLine * 2 - 1, (await BoxAsync(body.Locator(".ad-corpo__descricao")).ConfigureAwait(false)).Height, "a quebra de linha da descrição vira duas linhas");
    }
}

/// <summary>No site de Production a página de componentes não existe: 404, mesmo para o Administrador logado.</summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class ComponentsProductionE2ETests : SitePage
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task EmProduction_ARotaNaoExiste_NemParaOAdministradorLogado()
    {
        string baseUrl = RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/');
        IResponse anonymous = (await Page.GotoAsync(baseUrl + "/painel/componentes").ConfigureAwait(false))!;
        Assert.AreEqual(404, anonymous.Status, "sem login: 404 e não a página de entrada");

        await Page.GotoAsync(baseUrl + "/painel/entrar").ConfigureAwait(false);
        await Page.GetByLabel("E-mail").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL")).ConfigureAwait(false);
        await Page.GetByLabel("Senha").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
        await Page.WaitForURLAsync(new Regex(@"/painel/(?!entrar)")).ConfigureAwait(false);

        IResponse signedIn = (await Page.GotoAsync(baseUrl + "/painel/componentes").ConfigureAwait(false))!;
        Assert.AreEqual(404, signedIn.Status);
    }
}
