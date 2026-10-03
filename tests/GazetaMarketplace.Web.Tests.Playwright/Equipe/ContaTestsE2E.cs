using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Equipe;

/// <summary>
/// US-006 no navegador (roda no /test). Variáveis: GAZETA_BASE_URL (site no ar), GAZETA_E2E_EMAIL e GAZETA_E2E_SENHA
/// (conta de Redator ou Administrador criada no banco de teste) e, só para a sessão expirada,
/// GAZETA_E2E_SESSAO_MINUTOS (o mesmo valor de Autenticacao__SessaoMinutos com que o site foi iniciado, por exemplo 1).
/// </summary>
[TestClass]
[ExigeVariaveis("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_SENHA")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class ContaTestsE2E : PaginaDoSite
#pragma warning restore CA1515
{
    private static string Url(string caminho) => ExigeVariaveisAttribute.Valor("GAZETA_BASE_URL").TrimEnd('/') + caminho;

    private async Task EntrarAsync()
    {
        await Page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await Page.GetByLabel("E-mail").FillAsync(ExigeVariaveisAttribute.Valor("GAZETA_E2E_EMAIL")).ConfigureAwait(false);
        await Page.GetByLabel("Senha").FillAsync(ExigeVariaveisAttribute.Valor("GAZETA_E2E_SENHA")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Sair" })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US006S03_SairDoPainel() // @US-006-S03
    {
        await EntrarAsync().ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Sair" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/entrar$")).ConfigureAwait(false);

        // O botão Voltar do navegador não pode reabrir o painel
        await Page.GoBackAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/entrar")).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Sair" })).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Entrar" })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    [ExigeVariaveis("GAZETA_E2E_SESSAO_MINUTOS")]
    public async Task US006S08_SessaoExpiradaPorInatividade() // @US-006-S08
    {
        int minutos = int.Parse(ExigeVariaveisAttribute.Valor("GAZETA_E2E_SESSAO_MINUTOS"), System.Globalization.CultureInfo.InvariantCulture);
        await EntrarAsync().ConfigureAwait(false);

        // Sem tocar em nada: a sessão deslizante só vence depois do tempo configurado
        await Task.Delay(TimeSpan.FromSeconds((minutos * 60) + 5)).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Meus anúncios" }).Or(Page.GetByRole(AriaRole.Link, new() { Name = "Anúncios" })).First.ClickAsync().ConfigureAwait(false);

        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/entrar")).ConfigureAwait(false);
        await Expect(Page.GetByText("Sua sessão expirou. Entre novamente.")).ToBeVisibleAsync().ConfigureAwait(false);
    }
}

/// <summary>A página de entrada é pública: não precisa de conta, só do site no ar.</summary>
[TestClass]
[ExigeSiteNoAr]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class EntrarE2E : PaginaDoSite
#pragma warning restore CA1515
{
    private static string Url(string caminho) => Environment.GetEnvironmentVariable(ExigeSiteNoArAttribute.Variavel).TrimEnd('/') + caminho;

    [TestMethod]
    public async Task Entrar_SemViolacoesDeAcessibilidade_NemDeCsp()
    {
        System.Collections.Generic.List<string> csp = [];
        Page.Console += (_, mensagem) =>
        {
            if (mensagem.Text.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase))
            {
                csp.Add(mensagem.Text);
            }
        };

        await Page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);

        AxeResult resultado = await Page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] }
        }).ConfigureAwait(false);

        Assert.AreEqual(0, resultado.Violations.Length, string.Join("; ", System.Linq.Enumerable.Select(resultado.Violations, v => v.Id + ": " + v.Help)));
        CollectionAssert.AreEqual(Array.Empty<string>(), csp.ToArray());
    }

    [TestMethod]
    public async Task Entrar_CamposTemRotuloVisivel_ESenhaOculta()
    {
        await Page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);

        await Expect(Page.GetByLabel("E-mail")).ToHaveAttributeAsync("autocomplete", "username").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Senha")).ToHaveAttributeAsync("type", "password").ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Esqueci minha senha" })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Entrar_NoCelular_NaoTemRolagemHorizontal()
    {
        await Page.SetViewportSizeAsync(320, 700).ConfigureAwait(false);
        await Page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);

        bool cabe = await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= document.documentElement.clientWidth").ConfigureAwait(false);
        Assert.IsTrue(cabe, "rolagem horizontal em 320 px (NFR-17)");
    }
}
