using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Ads;

/// <summary>
/// A lista de anúncios do painel (4.4) no navegador, no site publicado: o Administrador busca pelo título (sem acento, sem diferença de maiúsculas), filtra por situação,
/// abre cada linha no destino certo, vê os arquivados só ao filtrar por "Arquivado" e pagina a lista (o banco do E2E tem mais de 20 anúncios); sem JavaScript o filtro
/// continua funcionando (formulário GET); axe e rolagem em 1280 e 320 px, com a tabela virando cartões. O Redator é provado nos testes HTTP e na integração.
/// Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class PanelAdListE2ETests : SitePage
#pragma warning restore CA1515
{
    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";

    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Photos", "Fixtures", name);

    private static async Task SignInAsync(IPage page, string email, string password)
    {
        await page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await page.GetByLabel("E-mail").FillAsync(email).ConfigureAwait(false);
        await page.GetByLabel("Senha").FillAsync(password).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
        await page.WaitForURLAsync(new Regex(@"/painel/(?!entrar)")).ConfigureAwait(false);
    }

    private Task SignInAdminAsync() => SignInAsync(Page, RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL"), RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD"));

    /// <summary>Cria um anúncio de "Livros e revistas" com uma foto e o envia para revisão pela tela, como o Redator faria.</summary>
    private async Task SubmitAdAsync(string title)
    {
        await Page.GotoAsync(Url("/painel/anuncios/novo")).ConfigureAwait(false);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        await Page.GetByLabel("Título").FillAsync(title).ConfigureAwait(false);
        await Page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Livros e revistas" }).ConfigureAwait(false);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false); // a troca de categoria busca os campos do grupo; o que se digita antes de a resposta chegar se perderia
        await Page.GetByLabel("Descrição").FillAsync("Edição 2020, sem anotações").ConfigureAwait(false);
        await Page.GetByLabel("Condição").SelectOptionAsync(new SelectOptionValue { Index = 1 }).ConfigureAwait(false);
        await Page.GetByLabel("Preço").FillAsync("5000").ConfigureAwait(false);
        await Page.GetByLabel("CEP").FillAsync("13015-100").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Cidade (automático)")).ToHaveValueAsync("Campinas").ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Rascunho salvo" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        await Page.WaitForFunctionAsync("() => document.getElementById('arquivo-foto')?.multiple === true").ConfigureAwait(false);
        await Page.GetByLabel("Escolher fotos").SetInputFilesAsync(Fixture("foto-1.jpg")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar fotos" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Fotos (1 de" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Anúncio enviado para revisão")).ToBeVisibleAsync().ConfigureAwait(false);
    }

    private static string Describe(AxeResultItem violation) =>
        violation.Id + ": " + violation.Help + " [" + string.Join(" | ", violation.Nodes.Take(3).Select(n => n.Html + " => " + string.Join("; ", n.Any.Select(a => a.Message)))) + "]";

    private ILocator Rows(IPage page) => page.Locator("tbody tr[data-ad-id]");

    private static async Task SearchAsync(IPage page, string term, string situation = null)
    {
        await page.GetByLabel("Buscar por título").FillAsync(term).ConfigureAwait(false);
        if (situation is not null)
        {
            await page.GetByLabel("Situação").SelectOptionAsync(new SelectOptionValue { Label = situation }).ConfigureAwait(false);
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Filtrar" }).ClickAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US012S03_S04_S05_Administrador_BuscaFiltraEAbreCadaLinhaNoDestinoCerto()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        string unique = Guid.NewGuid().ToString("N")[..8];
        string reviewTitle = $"Álbum Zeta {unique} revisão";
        string draftTitle = $"Álbum Zeta {unique} rascunho";
        await SubmitAdAsync(reviewTitle).ConfigureAwait(false);
        await Page.GotoAsync(Url("/painel/anuncios/novo")).ConfigureAwait(false);
        await Page.GetByLabel("Título").FillAsync(draftTitle).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Rascunho salvo" })).ToBeVisibleAsync().ConfigureAwait(false);

        // S04: o termo sem acento e em minúsculas acha os dois anúncios do teste; a lista mostra o autor e as abas do Administrador
        await Page.GotoAsync(Url("/painel/anuncios")).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Anúncios", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Todos os anúncios" })).ToHaveAttributeAsync("aria-current", "page").ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Columnheader, new() { Name = "Autor" })).ToBeVisibleAsync().ConfigureAwait(false);
        await SearchAsync(Page, $"album zeta {unique}").ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"\?q=album\+zeta\+" + unique + "|\\?q=album%20zeta%20" + unique)).ConfigureAwait(false);
        await Expect(Rows(Page)).ToHaveCountAsync(2).ConfigureAwait(false);
        await Expect(Page.Locator("[data-ads-total]")).ToHaveTextAsync("2 anúncios").ConfigureAwait(false);
        await Expect(Page.GetByLabel("Buscar por título")).ToHaveValueAsync($"album zeta {unique}").ConfigureAwait(false);

        // S03: a situação "Em revisão" deixa só o enviado; "Rascunho", só o rascunho
        await SearchAsync(Page, $"album zeta {unique}", "Em revisão").ConfigureAwait(false);
        await Expect(Rows(Page)).ToHaveCountAsync(1).ConfigureAwait(false);
        await Expect(Rows(Page).First).ToContainTextAsync(reviewTitle).ConfigureAwait(false);
        await Expect(Rows(Page).First).ToContainTextAsync("Em revisão").ConfigureAwait(false);
        await Expect(Page.Locator("[data-ads-total]")).ToHaveTextAsync("1 anúncio").ConfigureAwait(false);
        await SearchAsync(Page, $"album zeta {unique}", "Rascunho").ConfigureAwait(false);
        await Expect(Rows(Page)).ToHaveCountAsync(1).ConfigureAwait(false);
        await Expect(Rows(Page).First).ToContainTextAsync(draftTitle).ConfigureAwait(false);
        await Expect(Rows(Page).First).ToContainTextAsync("Rascunho").ConfigureAwait(false);

        // S05: o Rascunho abre a edição; o Em revisão abre a pré-visualização do Administrador
        await Rows(Page).First.GetByRole(AriaRole.Link, new() { Name = draftTitle }).ClickAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/anuncios/\d+/editar$")).ConfigureAwait(false);
        await Page.GoBackAsync().ConfigureAwait(false);
        await SearchAsync(Page, $"album zeta {unique}", "Em revisão").ConfigureAwait(false);
        await Rows(Page).First.GetByRole(AriaRole.Link, new() { Name = reviewTitle }).ClickAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/anuncios/\d+/pre-visualizacao$")).ConfigureAwait(false);

        // "Limpar filtros" volta à lista sem busca nem situação
        await Page.GotoAsync(Url($"/painel/anuncios?q=album+zeta+{unique}&situacao=rascunho")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Limpar filtros" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/anuncios$")).ConfigureAwait(false);
        await Expect(Page.GetByLabel("Buscar por título")).ToHaveValueAsync(string.Empty).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Arquivados_FicamEscondidosAteFiltrarPorArquivado_ESemResultadoMostraLimparFiltros()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        string unique = Guid.NewGuid().ToString("N")[..8];
        string title = $"Livro Omega {unique}";
        await SubmitAdAsync(title).ConfigureAwait(false);
        await OpenFromQueueAndArchiveAsync(title).ConfigureAwait(false);

        await Page.GotoAsync(Url("/painel/anuncios")).ConfigureAwait(false);
        await SearchAsync(Page, $"omega {unique}").ConfigureAwait(false);
        await Expect(Page.GetByText("Nenhum anúncio encontrado")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Rows(Page)).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Limpar filtros" }).First).ToBeVisibleAsync().ConfigureAwait(false);

        await SearchAsync(Page, $"omega {unique}", "Arquivado").ConfigureAwait(false);
        await Expect(Rows(Page)).ToHaveCountAsync(1).ConfigureAwait(false);
        await Expect(Rows(Page).First).ToContainTextAsync(title).ConfigureAwait(false);
        await Expect(Rows(Page).First).ToContainTextAsync("Arquivado").ConfigureAwait(false);
        await Rows(Page).First.GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Situação:")).ToContainTextAsync("Arquivado").ConfigureAwait(false);
    }

    private async Task OpenFromQueueAndArchiveAsync(string title)
    {
        await Page.GotoAsync(Url("/painel/anuncios/fila")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Row).Filter(new() { HasText = title }).GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio arquivado" })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US012S07_ListaComMaisDe20_Pagina20PorVez_ELinksDePaginaFuncionam()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);

        await Page.GotoAsync(Url("/painel/anuncios")).ConfigureAwait(false);
        await Expect(Rows(Page)).ToHaveCountAsync(20).ConfigureAwait(false);
        ILocator pagination = Page.GetByRole(AriaRole.Navigation, new() { Name = "Paginação" });
        await Expect(pagination).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(pagination.GetByRole(AriaRole.Link, new() { Name = "Página 1, página atual" })).ToHaveAttributeAsync("aria-current", "page").ConfigureAwait(false);
        await Expect(pagination.GetByText("Anterior")).ToHaveAttributeAsync("aria-disabled", "true").ConfigureAwait(false);
        string firstTitle = (await Rows(Page).First.GetByRole(AriaRole.Link).First.InnerTextAsync().ConfigureAwait(false)).Trim();

        await pagination.GetByRole(AriaRole.Link, new() { Name = "Próxima" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"\?pagina=2$")).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Navigation, new() { Name = "Paginação" }).GetByRole(AriaRole.Link, new() { Name = "Página 2, página atual" })).ToBeVisibleAsync().ConfigureAwait(false);
        Assert.IsTrue(await Rows(Page).CountAsync().ConfigureAwait(false) >= 1, "a página 2 tem anúncios");
        Assert.AreNotEqual(firstTitle, (await Rows(Page).First.GetByRole(AriaRole.Link).First.InnerTextAsync().ConfigureAwait(false)).Trim(), "a página 2 começa em outro anúncio");

        await Page.GetByRole(AriaRole.Navigation, new() { Name = "Paginação" }).GetByRole(AriaRole.Link, new() { Name = "Anterior" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/anuncios$")).ConfigureAwait(false);
        await Expect(Rows(Page)).ToHaveCountAsync(20).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SemJavaScript_BuscaEFiltroFuncionamPorFormularioComum()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        string unique = Guid.NewGuid().ToString("N")[..8];
        string title = $"Livro Sigma {unique}";
        await SubmitAdAsync(title).ConfigureAwait(false);

        IBrowserContext noJs = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true, JavaScriptEnabled = false }).ConfigureAwait(false);
        IPage page = await noJs.NewPageAsync().ConfigureAwait(false);
        await SignInAsync(page, RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL"), RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await page.GotoAsync(Url("/painel/anuncios")).ConfigureAwait(false);
        await SearchAsync(page, $"sigma {unique}", "Em revisão").ConfigureAwait(false);

        await Expect(page).ToHaveURLAsync(new Regex(@"situacao=em-revisao")).ConfigureAwait(false);
        await Expect(Rows(page)).ToHaveCountAsync(1).ConfigureAwait(false);
        await Expect(Rows(page).First).ToContainTextAsync(title).ConfigureAwait(false);
        await Expect(page.Locator("[data-ads-total]")).ToHaveTextAsync("1 anúncio").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Acessibilidade_Lista_SemViolacoes_SemRolagemHorizontal_ETabelaViraCartoesEm320()
    {
        using FakeViaCep viaCep = new();
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };
        await SignInAdminAsync().ConfigureAwait(false);

        foreach (int width in new[] { 1280, 768, 320 })
        {
            await Page.SetViewportSizeAsync(width, 900).ConfigureAwait(false);
            foreach (string path in new[] { "/painel/anuncios", "/painel/anuncios?pagina=2", "/painel/anuncios?situacao=em-revisao", "/painel/anuncios?q=nada-assim-existe" })
            {
                await Page.GotoAsync(Url(path)).ConfigureAwait(false);
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
                await Page.Mouse.MoveAsync(0, 0).ConfigureAwait(false);
                AxeResult result = await Page.RunAxe(options).ConfigureAwait(false);
                Assert.AreEqual(0, result.Violations.Length, $"{path} em {width}px: " + string.Join("; ", result.Violations.Select(Describe)));
                Assert.IsFalse(
                    await Page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth").ConfigureAwait(false),
                    $"{path} sem rolagem horizontal em {width} px");
            }
        }

        // Em 320 px cada linha vira um cartão empilhado; em 1280 px é uma tabela comum
        await Page.SetViewportSizeAsync(320, 900).ConfigureAwait(false);
        await Page.GotoAsync(Url("/painel/anuncios")).ConfigureAwait(false);
        Assert.AreEqual("block", await Page.Locator("tbody td").First.EvaluateAsync<string>("el => getComputedStyle(el).display").ConfigureAwait(false));
        await Page.SetViewportSizeAsync(1280, 900).ConfigureAwait(false);
        Assert.AreEqual("table-cell", await Page.Locator("tbody td").First.EvaluateAsync<string>("el => getComputedStyle(el).display").ConfigureAwait(false));
    }
}
