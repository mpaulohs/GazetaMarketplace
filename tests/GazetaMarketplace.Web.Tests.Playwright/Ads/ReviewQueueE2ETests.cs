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
/// A fila de revisão e a pré-visualização (4.1) no navegador, no site publicado: o Administrador vê o anúncio enviado na fila, abre a pré-visualização com a foto carregada
/// e "Editar", o layout empilha em 320 px, o axe não acha violações e o Redator recebe "acesso negado". Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e
/// GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class ReviewQueueE2ETests : SitePage
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

    [TestMethod]
    public async Task Administrador_VeOAnuncioNaFila_AbreAPreVisualizacaoComFoto_EEditar_EVolta()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        string title = Unique("Livro da fila");
        await SubmitAdAsync(title).ConfigureAwait(false);

        await Page.GotoAsync(Url("/painel/anuncios/fila")).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Anúncios", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex(@"^Fila de revisão \(\d+\)$") })).ToHaveAttributeAsync("aria-current", "page").ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Todos os anúncios" })).ToBeVisibleAsync().ConfigureAwait(false);
        ILocator row = Page.GetByRole(AriaRole.Row).Filter(new() { HasText = title });
        await Expect(row).ToContainTextAsync("Livros e revistas").ConfigureAwait(false);
        await Expect(row.Locator("time")).ToHaveTextAsync(new Regex(@"^\d{2}/\d{2}/\d{4}$")).ConfigureAwait(false);

        await row.GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync().ConfigureAwait(false);

        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/anuncios/\d+/pre-visualizacao$")).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Pré-visualização do anúncio", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Pré-visualização — ainda não publicado" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = title, Level = 2 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("R$ 50", new() { Exact = false }).First).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Fale com a Gazeta" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Editar" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Publicar|Rejeitar|Arquivar") })).ToHaveCountAsync(0).ConfigureAwait(false);
        // A foto de destaque carrega de verdade (entregue ao Administrador mesmo com o anúncio ainda não publicado)
        await Page.WaitForFunctionAsync("() => { const i = document.querySelector('[data-ad-photos] img'); return i && i.complete && i.naturalWidth > 0; }").ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Link, new() { Name = "Editar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/anuncios/\d+/editar$")).ConfigureAwait(false);
        await Page.GoBackAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Fila de revisão" }).First.ClickAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/anuncios/fila$")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Acessibilidade_FilaEPreVisualizacao_SemViolacoes_RolagemEEmpilhamentoNosTamanhos()
    {
        using FakeViaCep viaCep = new();
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };
        await SignInAdminAsync().ConfigureAwait(false);
        string title = Unique("Livro acessível");
        await SubmitAdAsync(title).ConfigureAwait(false);

        await Page.GotoAsync(Url("/painel/anuncios/fila")).ConfigureAwait(false);
        string previewUrl = (await Page.GetByRole(AriaRole.Link, new() { Name = title }).GetAttributeAsync("href").ConfigureAwait(false))!;
        foreach (int width in new[] { 1280, 1024, 768, 320 })
        {
            await Page.SetViewportSizeAsync(width, 900).ConfigureAwait(false);
            foreach (string path in new[] { "/painel/anuncios/fila", previewUrl })
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

        // Em 320 px cada linha da fila vira um cartão empilhado; em 1280 px é uma tabela comum
        await Page.GotoAsync(Url("/painel/anuncios/fila")).ConfigureAwait(false);
        Assert.AreEqual("block", await Page.Locator("tbody td").First.EvaluateAsync<string>("el => getComputedStyle(el).display").ConfigureAwait(false));
        await Page.SetViewportSizeAsync(1280, 900).ConfigureAwait(false);
        Assert.AreEqual("table-cell", await Page.Locator("tbody td").First.EvaluateAsync<string>("el => getComputedStyle(el).display").ConfigureAwait(false));
    }

    [TestMethod]
    public async Task Redator_NaoAcessaAFilaNemAPreVisualizacao_VeAcessoNegado()
    {
        const string Provisional = "Provis0ria!";
        const string Final = "Senh@Final#2026";
        await SignInAdminAsync().ConfigureAwait(false);
        string unique = Guid.NewGuid().ToString("N")[..8];
        string email = $"redator-{unique}@exemplo.com.br";
        await Page.GotoAsync(Url("/painel/usuarios/novo")).ConfigureAwait(false);
        await Page.GetByLabel("Nome").FillAsync("Redator " + unique).ConfigureAwait(false);
        await Page.GetByLabel("E-mail").FillAsync(email).ConfigureAwait(false);
        await Page.GetByLabel("Redator").CheckAsync().ConfigureAwait(false);
        await Page.GetByLabel("Senha provisória").FillAsync(Provisional).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("criado").ConfigureAwait(false);

        IBrowserContext separate = await Browser.NewContextAsync(ContextOptions()).ConfigureAwait(false);
        IPage writer = await separate.NewPageAsync().ConfigureAwait(false);
        await SignInAsync(writer, email, Provisional).ConfigureAwait(false);
        await writer.Locator("#NewPassword").FillAsync(Final).ConfigureAwait(false);
        await writer.Locator("#ConfirmPassword").FillAsync(Final).ConfigureAwait(false);
        await writer.GetByRole(AriaRole.Button, new() { Name = "Salvar senha" }).ClickAsync().ConfigureAwait(false);
        await writer.WaitForURLAsync(new Regex(@"/painel/anuncios")).ConfigureAwait(false);

        foreach (string path in new[] { "/painel/anuncios/fila", "/painel/anuncios/1/pre-visualizacao" })
        {
            await writer.GotoAsync(Url(path)).ConfigureAwait(false);
            await Expect(writer).ToHaveURLAsync(new Regex(@"/painel/acesso-negado")).ConfigureAwait(false);
            await Expect(writer.GetByText("Você não tem permissão para acessar esta página")).ToBeVisibleAsync().ConfigureAwait(false);
            await Expect(writer.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Publicar|Rejeitar") })).ToHaveCountAsync(0).ConfigureAwait(false);
        }
    }
}
