using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Layout;

/// <summary>
/// Mede no navegador o que o base.css promete (architecture/design-system.md §2.4 e NFR-16/17).
/// Roda no /test: precisa do site no ar e da variável GAZETA_BASE_URL (por exemplo https://localhost:5001); sem ela os testes são ignorados.
/// </summary>
[TestClass]
[RequiresRunningSite]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class BaseCssTests : SitePage
#pragma warning restore CA1515
{
    private static readonly int[] _widths = [320, 768, 1024, 1280];

    private static string Url(string path)
    {
        string baseUrl = Environment.GetEnvironmentVariable(RequiresRunningSiteAttribute.Variable);
        return baseUrl.TrimEnd('/') + path;
    }

    [TestMethod]
    public async Task Foco_TemContornoComContraste()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        // O primeiro Tab cai no link "Ir para o conteúdo"
        await Page.Keyboard.PressAsync("Tab").ConfigureAwait(false);
        ILocator focus = Page.Locator(":focus");
        await Expect(focus).ToHaveTextAsync("Ir para o conteúdo").ConfigureAwait(false);

        string style = await focus.EvaluateAsync<string>(
            "e => { const c = getComputedStyle(e); return [c.outlineStyle, c.outlineWidth, c.outlineColor].join('|'); }").ConfigureAwait(false);
        string[] parts = style.Split('|');

        Assert.AreEqual("solid", parts[0]);
        Assert.AreEqual("2px", parts[1]);
        Assert.AreEqual("rgb(10, 88, 202)", parts[2]);
        Assert.IsTrue(Contrast(parts[2], "rgb(255, 255, 255)") >= 3.0, "o contorno precisa de 3:1 contra o fundo");
    }

    [TestMethod]
    public async Task BordaDoCampoDeBusca_TemContraste()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        string color = await Page.GetByRole(AriaRole.Searchbox, new() { Name = "Buscar anúncios" })
            .EvaluateAsync<string>("e => getComputedStyle(e).borderTopColor").ConfigureAwait(false);

        Assert.AreEqual("rgb(108, 117, 125)", color);
        Assert.IsTrue(Contrast(color, "rgb(255, 255, 255)") >= 3.0, "a borda do campo precisa de 3:1 (WCAG 1.4.11)");
    }

    [TestMethod]
    public async Task Link_NaoDependeSoDaCor()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        string decoration = await Page.GetByRole(AriaRole.Link, new() { Name = "Área da equipe" })
            .EvaluateAsync<string>("e => getComputedStyle(e).textDecorationLine").ConfigureAwait(false);

        StringAssert.Contains(decoration, "underline");
    }

    [TestMethod]
    public async Task PaginaInicial_NaoTemRolagemHorizontal_NasQuatroLarguras()
    {
        foreach (int width in _widths)
        {
            await Page.SetViewportSizeAsync(width, 800).ConfigureAwait(false);
            await Page.GotoAsync(Url("/")).ConfigureAwait(false);

            bool fits = await Page.EvaluateAsync<bool>(
                "() => document.documentElement.scrollWidth <= document.documentElement.clientWidth").ConfigureAwait(false);

            Assert.IsTrue(fits, $"rolagem horizontal em {width} px (NFR-17)");
        }
    }

    [TestMethod]
    public async Task PaginaInicial_NaoViolaACsp()
    {
        List<string> violations = [];
        Page.Console += (_, message) =>
        {
            if (message.Text.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add(message.Text);
            }
        };

        await Page.GotoAsync(Url("/")).ConfigureAwait(false);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);

        CollectionAssert.AreEqual(Array.Empty<string>(), violations.ToArray(), "a CSP bloqueou algo que o layout usa");
    }

    [TestMethod]
    public async Task PaginaInicial_SemViolacoesDeAcessibilidade()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        AxeResult result = await Page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] }
        }).ConfigureAwait(false);

        Assert.AreEqual(0, result.Violations.Length, string.Join("; ", result.Violations.Select(v => v.Id + ": " + v.Help)));
    }

    [TestMethod]
    public async Task Fonte_Poppins_EstaCarregada_DoProprioSite()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        bool loaded = await Page.EvaluateAsync<bool>(
            "async () => { await document.fonts.ready; return document.fonts.check('16px Poppins') && document.fonts.check('600 16px Poppins'); }").ConfigureAwait(false);
        string family = await Page.Locator("body").EvaluateAsync<string>("e => getComputedStyle(e).fontFamily").ConfigureAwait(false);
        int faces = await Page.EvaluateAsync<int>(
            "() => [...document.fonts].filter(f => f.family.replace(/\"/g, '') === 'Poppins' && f.status === 'loaded').length").ConfigureAwait(false);

        Assert.IsTrue(loaded, "a Poppins 400 e 600 não carregaram");
        StringAssert.Contains(family, "Poppins");
        Assert.IsTrue(faces >= 2, $"esperava ao menos 2 pesos carregados, vieram {faces}");
    }

    [TestMethod]
    public async Task Icones_FontAwesome_CarregamDoProprioSite()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        bool loaded = await Page.EvaluateAsync<bool>(
            "async () => { await document.fonts.ready; return document.fonts.check('14px FontAwesome'); }").ConfigureAwait(false);
        string content = await Page.Locator("i.fa-search").First.EvaluateAsync<string>("e => getComputedStyle(e, '::before').content").ConfigureAwait(false);

        Assert.IsTrue(loaded, "a fonte do Font Awesome não carregou");
        Assert.AreNotEqual("none", content);
    }

    [TestMethod]
    public async Task Cabecalho_TemODegradeEOBotaoPrimarioComContraste()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        string background = await Page.Locator("header.cabecalho").EvaluateAsync<string>("e => getComputedStyle(e).backgroundImage").ConfigureAwait(false);
        string[] button = (await Page.GetByRole(AriaRole.Button, new() { Name = "Buscar" })
            .EvaluateAsync<string>("e => { const c = getComputedStyle(e); return c.backgroundColor + '|' + c.color; }").ConfigureAwait(false)).Split('|');

        StringAssert.Contains(background, "linear-gradient");
        Assert.AreEqual("rgb(215, 34, 19)", button[0]);
        Assert.AreEqual("rgb(255, 255, 255)", button[1]);
        Assert.IsTrue(Contrast(button[1], button[0]) >= 4.5, "texto branco sobre o botão primário");
    }

    [TestMethod]
    public async Task PaginaInicial_SoFazRequisicoesAoProprioSite()
    {
        List<string> external = [];
        string host = new Uri(Url("/")).Host;
        Page.Request += (_, request) =>
        {
            Uri uri = new(request.Url);
            if (uri.Scheme.StartsWith("http", StringComparison.Ordinal) && !string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase))
            {
                external.Add(request.Url);
            }
        };

        await Page.GotoAsync(Url("/")).ConfigureAwait(false);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);

        CollectionAssert.AreEqual(Array.Empty<string>(), external.ToArray(), "o site buscou algo fora do próprio domínio (Google Fonts, CDN...)");
    }

    private static double Contrast(string a, string b)
    {
        double la = Luminance(a);
        double lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(string rgb)
    {
        double[] channels = Regex.Matches(rgb, @"\d+").Take(3)
            .Select(m => int.Parse(m.Value, CultureInfo.InvariantCulture) / 255.0)
            .Select(c => c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4))
            .ToArray();
        return (0.2126 * channels[0]) + (0.7152 * channels[1]) + (0.0722 * channels[2]);
    }
}
