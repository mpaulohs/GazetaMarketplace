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
[ExigeSiteNoAr]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class BaseCssTests : PaginaDoSite
#pragma warning restore CA1515
{
    private static readonly int[] _larguras = [320, 768, 1024, 1280];

    private static string Url(string caminho)
    {
        string baseUrl = Environment.GetEnvironmentVariable(ExigeSiteNoArAttribute.Variavel);
        return baseUrl.TrimEnd('/') + caminho;
    }

    [TestMethod]
    public async Task Foco_TemContornoComContraste()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        // O primeiro Tab cai no link "Ir para o conteúdo"
        await Page.Keyboard.PressAsync("Tab").ConfigureAwait(false);
        ILocator foco = Page.Locator(":focus");
        await Expect(foco).ToHaveTextAsync("Ir para o conteúdo").ConfigureAwait(false);

        string estilo = await foco.EvaluateAsync<string>(
            "e => { const c = getComputedStyle(e); return [c.outlineStyle, c.outlineWidth, c.outlineColor].join('|'); }").ConfigureAwait(false);
        string[] partes = estilo.Split('|');

        Assert.AreEqual("solid", partes[0]);
        Assert.AreEqual("2px", partes[1]);
        Assert.AreEqual("rgb(10, 88, 202)", partes[2]);
        Assert.IsTrue(Contraste(partes[2], "rgb(255, 255, 255)") >= 3.0, "o contorno precisa de 3:1 contra o fundo");
    }

    [TestMethod]
    public async Task BordaDoCampoDeBusca_TemContraste()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        string cor = await Page.GetByRole(AriaRole.Searchbox, new() { Name = "Buscar anúncios" })
            .EvaluateAsync<string>("e => getComputedStyle(e).borderTopColor").ConfigureAwait(false);

        Assert.AreEqual("rgb(108, 117, 125)", cor);
        Assert.IsTrue(Contraste(cor, "rgb(255, 255, 255)") >= 3.0, "a borda do campo precisa de 3:1 (WCAG 1.4.11)");
    }

    [TestMethod]
    public async Task Link_NaoDependeSoDaCor()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        string decoracao = await Page.GetByRole(AriaRole.Link, new() { Name = "Área da equipe" })
            .EvaluateAsync<string>("e => getComputedStyle(e).textDecorationLine").ConfigureAwait(false);

        StringAssert.Contains(decoracao, "underline");
    }

    [TestMethod]
    public async Task PaginaInicial_NaoTemRolagemHorizontal_NasQuatroLarguras()
    {
        foreach (int largura in _larguras)
        {
            await Page.SetViewportSizeAsync(largura, 800).ConfigureAwait(false);
            await Page.GotoAsync(Url("/")).ConfigureAwait(false);

            bool cabe = await Page.EvaluateAsync<bool>(
                "() => document.documentElement.scrollWidth <= document.documentElement.clientWidth").ConfigureAwait(false);

            Assert.IsTrue(cabe, $"rolagem horizontal em {largura} px (NFR-17)");
        }
    }

    [TestMethod]
    public async Task PaginaInicial_NaoViolaACsp()
    {
        List<string> violacoes = [];
        Page.Console += (_, mensagem) =>
        {
            if (mensagem.Text.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase))
            {
                violacoes.Add(mensagem.Text);
            }
        };

        await Page.GotoAsync(Url("/")).ConfigureAwait(false);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);

        CollectionAssert.AreEqual(Array.Empty<string>(), violacoes.ToArray(), "a CSP bloqueou algo que o layout usa");
    }

    [TestMethod]
    public async Task PaginaInicial_SemViolacoesDeAcessibilidade()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        AxeResult resultado = await Page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] }
        }).ConfigureAwait(false);

        Assert.AreEqual(0, resultado.Violations.Length, string.Join("; ", resultado.Violations.Select(v => v.Id + ": " + v.Help)));
    }

    [TestMethod]
    public async Task Fonte_Poppins_EstaCarregada_DoProprioSite()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        bool carregada = await Page.EvaluateAsync<bool>(
            "async () => { await document.fonts.ready; return document.fonts.check('16px Poppins') && document.fonts.check('600 16px Poppins'); }").ConfigureAwait(false);
        string familia = await Page.Locator("body").EvaluateAsync<string>("e => getComputedStyle(e).fontFamily").ConfigureAwait(false);
        int faces = await Page.EvaluateAsync<int>(
            "() => [...document.fonts].filter(f => f.family.replace(/\"/g, '') === 'Poppins' && f.status === 'loaded').length").ConfigureAwait(false);

        Assert.IsTrue(carregada, "a Poppins 400 e 600 não carregaram");
        StringAssert.Contains(familia, "Poppins");
        Assert.IsTrue(faces >= 2, $"esperava ao menos 2 pesos carregados, vieram {faces}");
    }

    [TestMethod]
    public async Task Icones_FontAwesome_CarregamDoProprioSite()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        bool carregada = await Page.EvaluateAsync<bool>(
            "async () => { await document.fonts.ready; return document.fonts.check('14px FontAwesome'); }").ConfigureAwait(false);
        string conteudo = await Page.Locator("i.fa-search").First.EvaluateAsync<string>("e => getComputedStyle(e, '::before').content").ConfigureAwait(false);

        Assert.IsTrue(carregada, "a fonte do Font Awesome não carregou");
        Assert.AreNotEqual("none", conteudo);
    }

    [TestMethod]
    public async Task Cabecalho_TemODegradeEOBotaoPrimarioComContraste()
    {
        await Page.GotoAsync(Url("/")).ConfigureAwait(false);

        string fundo = await Page.Locator("header.cabecalho").EvaluateAsync<string>("e => getComputedStyle(e).backgroundImage").ConfigureAwait(false);
        string[] botao = (await Page.GetByRole(AriaRole.Button, new() { Name = "Buscar" })
            .EvaluateAsync<string>("e => { const c = getComputedStyle(e); return c.backgroundColor + '|' + c.color; }").ConfigureAwait(false)).Split('|');

        StringAssert.Contains(fundo, "linear-gradient");
        Assert.AreEqual("rgb(215, 34, 19)", botao[0]);
        Assert.AreEqual("rgb(255, 255, 255)", botao[1]);
        Assert.IsTrue(Contraste(botao[1], botao[0]) >= 4.5, "texto branco sobre o botão primário");
    }

    [TestMethod]
    public async Task PaginaInicial_SoFazRequisicoesAoProprioSite()
    {
        List<string> externas = [];
        string host = new Uri(Url("/")).Host;
        Page.Request += (_, requisicao) =>
        {
            Uri uri = new(requisicao.Url);
            if (uri.Scheme.StartsWith("http", StringComparison.Ordinal) && !string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase))
            {
                externas.Add(requisicao.Url);
            }
        };

        await Page.GotoAsync(Url("/")).ConfigureAwait(false);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);

        CollectionAssert.AreEqual(Array.Empty<string>(), externas.ToArray(), "o site buscou algo fora do próprio domínio (Google Fonts, CDN...)");
    }

    private static double Contraste(string a, string b)
    {
        double la = Luminancia(a);
        double lb = Luminancia(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminancia(string rgb)
    {
        double[] canais = Regex.Matches(rgb, @"\d+").Take(3)
            .Select(m => int.Parse(m.Value, CultureInfo.InvariantCulture) / 255.0)
            .Select(c => c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4))
            .ToArray();
        return (0.2126 * canais[0]) + (0.7152 * canais[1]) + (0.0722 * canais[2]);
    }
}
