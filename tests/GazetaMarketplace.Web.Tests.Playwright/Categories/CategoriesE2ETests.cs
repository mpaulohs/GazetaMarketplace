using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Categories;

/// <summary>
/// US-013 no navegador (roda no /test). Variáveis: GAZETA_BASE_URL (site no ar) e GAZETA_E2E_EMAIL / GAZETA_E2E_PASSWORD (conta de <b>Administrador</b>).
/// Cada teste cria categorias com nome único e as apaga no fim; o que mexe na ordem das categorias da carga a restaura. A classe não roda em paralelo
/// porque a ordem e a árvore são do site inteiro. Os cenários que dependem de anúncios (S08) são provados nos testes de unidade, até a tarefa 3.1.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class CategoriesE2ETests : SitePage
#pragma warning restore CA1515
{
    // A lista tem uns 150 itens e o Bootstrap rola até a âncora com animação; com movimento reduzido a rolagem é imediata e o teste não espera o botão parar
    public override BrowserNewContextOptions ContextOptions() => new() { IgnoreHTTPSErrors = true, ReducedMotion = ReducedMotion.Reduce };

    private static string Describe(AxeResultItem violation) =>
        violation.Id + ": " + violation.Help + " [" + string.Join(" | ", violation.Nodes.Take(3).Select(n => n.Html)) + "]";

    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";

    private static async Task SignInAsync(IPage page)
    {
        await page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await page.GetByLabel("E-mail").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL")).ConfigureAwait(false);
        await page.GetByLabel("Senha").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
        await page.WaitForURLAsync(new Regex(@"/painel/(?!entrar)")).ConfigureAwait(false);
    }

    private async Task OpenCategoriesAsync(IPage page = null)
    {
        page ??= Page;
        await page.GotoAsync(Url("/painel/categorias")).ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Categorias", Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    private static async Task CreateAsync(IPage page, string name, string parent = null)
    {
        await page.GetByRole(AriaRole.Link, new() { Name = "Nova categoria" }).ClickAsync().ConfigureAwait(false);
        await page.GetByLabel(new Regex("^Nome")).FillAsync(name).ConfigureAwait(false);
        if (parent is not null)
        {
            await page.GetByLabel("Categoria pai").SelectOptionAsync(new SelectOptionValue { Label = parent }).ConfigureAwait(false);
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
    }

    private static async Task DeleteWithDialogAsync(IPage page, string name)
    {
        await page.GetByRole(AriaRole.Link, new() { Name = $"Excluir {name}", Exact = true }).ClickAsync().ConfigureAwait(false);
        await page.GetByRole(AriaRole.Alertdialog).GetByRole(AriaRole.Button, new() { Name = "Excluir" }).ClickAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US013S02_S03_S05_CriarCategoriaPrincipal_Renomear_EExcluirPelaJanela()
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenCategoriesAsync().ConfigureAwait(false);
        string name = Unique("Colecionáveis");
        string renamed = name + " (renomeada)";

        await CreateAsync(Page, name).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Categoria {name} criada.").ConfigureAwait(false);
        await Expect(Page.GetByText(name, new() { Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Link, new() { Name = $"Editar {name}", Exact = true }).ClickAsync().ConfigureAwait(false);
        await Page.GetByLabel(new Regex("^Nome")).FillAsync(renamed).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Categoria renomeada para {renamed}.").ConfigureAwait(false);
        await Expect(Page.GetByText(name, new() { Exact = true })).ToHaveCountAsync(0).ConfigureAwait(false);

        // Recarregar mostra só o que o servidor guardou
        await OpenCategoriesAsync().ConfigureAwait(false);
        await Expect(Page.GetByText(renamed, new() { Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);

        await DeleteWithDialogAsync(Page, renamed).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Categoria {renamed} excluída.").ConfigureAwait(false);
        await OpenCategoriesAsync().ConfigureAwait(false);
        await Expect(Page.GetByText(renamed, new() { Exact = true })).ToHaveCountAsync(0).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US013S01_S11_CriarSubcategoria_ENoTerceiroNivel_AListaDePaiNaoTemOTerceiro() // @US-013-S01 @US-013-S11
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenCategoriesAsync().ConfigureAwait(false);
        string sub = Unique("Quadriciclos");
        string third = Unique("Peças para quadriciclos");

        await CreateAsync(Page, sub, "Automóveis, Peças e Acessórios").ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Categoria {sub} criada.").ConfigureAwait(false);
        await CreateAsync(Page, third, "— Autopeças").ConfigureAwait(false); // a lista recua os filhos com traço
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Categoria {third} criada.").ConfigureAwait(false);
        // O terceiro nível aparece dentro de uma lista dentro de outra lista, e o leitor de tela ouve de quem é filha
        await Expect(Page.Locator("li li li", new() { HasText = third })).ToHaveCountAsync(1).ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Link, new() { Name = "Nova categoria" }).ClickAsync().ConfigureAwait(false);
        ILocator options = Page.GetByLabel("Categoria pai").Locator("option");
        await Expect(options.Filter(new() { HasText = "Autopeças" })).ToHaveCountAsync(1).ConfigureAwait(false);
        await Expect(options.Filter(new() { HasText = "Peças para carros, vans e utilitários" })).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(options.Filter(new() { HasText = third })).ToHaveCountAsync(0).ConfigureAwait(false);

        await OpenCategoriesAsync().ConfigureAwait(false);
        await DeleteWithDialogAsync(Page, third).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("excluída").ConfigureAwait(false);
        await DeleteWithDialogAsync(Page, sub).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Categoria {sub} excluída.").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US013S04_MoverPorBotoes_TrocaAOrdem_EDevolveOFocoAoBotaoUsado() // @US-013-S04
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenCategoriesAsync().ConfigureAwait(false);
        string[] before = await RootNamesAsync().ConfigureAwait(false);
        Assert.IsTrue(Array.IndexOf(before, "Imóveis") < Array.IndexOf(before, "Automóveis, Peças e Acessórios"), "pré-condição: Imóveis vem antes");

        try
        {
            ILocator up = Page.GetByRole(AriaRole.Button, new() { Name = "Mover Automóveis, Peças e Acessórios para cima" });
            await up.ClickAsync().ConfigureAwait(false);

            await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Automóveis, Peças e Acessórios agora está antes de Imóveis").ConfigureAwait(false);
            await Expect(up).ToBeFocusedAsync().ConfigureAwait(false);
            string[] after = await RootNamesAsync().ConfigureAwait(false);
            Assert.IsLessThan(Array.IndexOf(after, "Imóveis"), Array.IndexOf(after, "Automóveis, Peças e Acessórios"));

            // A ordem persiste depois de recarregar
            await OpenCategoriesAsync().ConfigureAwait(false);
            string[] reloaded = await RootNamesAsync().ConfigureAwait(false);
            CollectionAssert.AreEqual(after, reloaded);
        }
        finally
        {
            await OpenCategoriesAsync().ConfigureAwait(false);
            string[] now = await RootNamesAsync().ConfigureAwait(false);
            if (Array.IndexOf(now, "Automóveis, Peças e Acessórios") < Array.IndexOf(now, "Imóveis"))
            {
                await Page.GetByRole(AriaRole.Button, new() { Name = "Mover Automóveis, Peças e Acessórios para baixo" }).ClickAsync().ConfigureAwait(false);
                await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("agora está depois de").ConfigureAwait(false);
            }
        }

        string[] restored = await RootNamesAsync().ConfigureAwait(false);
        CollectionAssert.AreEqual(before, restored, "a ordem original foi restaurada");
    }

    private async Task<string[]> RootNamesAsync()
    {
        // As categorias principais são os itens do primeiro nível da lista; o nome é o primeiro texto sem o "Categoria principal: " oculto
        return [.. await Page.Locator("ul.categorias-arvore > li > div > div > span.fw-semibold").AllInnerTextsAsync().ConfigureAwait(false)];
    }

    [TestMethod]
    public async Task US013S06_S07_NomeRepetidoEVazio_MostramAMensagemNoFormulario() // @US-013-S06 @US-013-S07
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenCategoriesAsync().ConfigureAwait(false);

        await CreateAsync(Page, "Motos", "Automóveis, Peças e Acessórios").ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Já existe uma categoria com esse nome neste grupo" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel(new Regex("^Nome"))).ToHaveAttributeAsync("aria-invalid", "true").ConfigureAwait(false);

        await Page.GetByLabel(new Regex("^Nome")).FillAsync("").ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Informe o nome da categoria" })).ToBeVisibleAsync().ConfigureAwait(false);

        await OpenCategoriesAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Excluir Motos", Exact = true })).ToHaveCountAsync(1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US013S09_ExcluirComSubcategorias_BloqueiaDepoisDeConfirmar() // @US-013-S09
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenCategoriesAsync().ConfigureAwait(false);

        await DeleteWithDialogAsync(Page, "Imóveis").ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Exclua ou mova antes as subcategorias desta categoria" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Imóveis", new() { Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US013S10_CategoriaComCamposEspecificos_BloqueiaDireto_SemJanela_EAindaRenomeia() // @US-013-S10
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenCategoriesAsync().ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Link, new() { Name = "Excluir Terrenos, sítios e fazendas", Exact = true }).ClickAsync().ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Esta categoria é usada pelos campos específicos e não pode ser excluída. Você pode renomeá-la." })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Alertdialog)).ToBeHiddenAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Terrenos, sítios e fazendas", new() { Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Editar Terrenos, sítios e fazendas", Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task JanelaDeExclusao_FocoInicialEmCancelar_EscapeFecha_ECancelarNaoExclui_EDevolveOFoco()
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenCategoriesAsync().ConfigureAwait(false);
        string name = Unique("Para cancelar");
        await CreateAsync(Page, name).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("criada").ConfigureAwait(false);

        ILocator trigger = Page.GetByRole(AriaRole.Link, new() { Name = $"Excluir {name}", Exact = true });
        await trigger.ClickAsync().ConfigureAwait(false);
        ILocator dialog = Page.GetByRole(AriaRole.Alertdialog);
        await Expect(dialog).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(dialog).ToContainTextAsync($"Excluir a categoria \"{name}\"?").ConfigureAwait(false);
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Cancelar" })).ToBeFocusedAsync().ConfigureAwait(false);

        AxeResult result = await Page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] }
        }).ConfigureAwait(false);
        Assert.AreEqual(0, result.Violations.Length, string.Join("; ", result.Violations.Select(Describe)));

        await Page.Keyboard.PressAsync("Escape").ConfigureAwait(false);
        await Expect(dialog).ToBeHiddenAsync().ConfigureAwait(false);
        await Expect(trigger).ToBeFocusedAsync().ConfigureAwait(false);

        await trigger.ClickAsync().ConfigureAwait(false);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancelar" }).ClickAsync().ConfigureAwait(false);
        await Expect(dialog).ToBeHiddenAsync().ConfigureAwait(false);
        await Expect(trigger).ToHaveCountAsync(1).ConfigureAwait(false);

        await DeleteWithDialogAsync(Page, name).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("excluída").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Paginas_SemViolacoesDeAcessibilidade_ESemRolagemHorizontalEm320()
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await OpenCategoriesAsync().ConfigureAwait(false);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };

        AxeResult index = await Page.RunAxe(options).ConfigureAwait(false);
        Assert.AreEqual(0, index.Violations.Length, "lista: " + string.Join("; ", index.Violations.Select(Describe)));

        await Page.SetViewportSizeAsync(320, 800).ConfigureAwait(false);
        bool overflows = await Page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth").ConfigureAwait(false);
        Assert.IsFalse(overflows, "a lista não rola na horizontal em 320 px");

        await Page.GotoAsync(Url("/painel/categorias/nova")).ConfigureAwait(false);
        AxeResult form = await Page.RunAxe(options).ConfigureAwait(false);
        Assert.AreEqual(0, form.Violations.Length, "formulário: " + string.Join("; ", form.Violations.Select(Describe)));
    }

    [TestMethod]
    public async Task SemJavaScript_CriarMoverEExcluirContinuamFuncionando() // @progressive-enhancement
    {
        IBrowserContext noScript = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true, JavaScriptEnabled = false, ReducedMotion = ReducedMotion.Reduce }).ConfigureAwait(false);
        IPage page = await noScript.NewPageAsync().ConfigureAwait(false);
        await SignInAsync(page).ConfigureAwait(false);
        await OpenCategoriesAsync(page).ConfigureAwait(false);
        string name = Unique("Sem script");

        await CreateAsync(page, name).ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Categoria {name} criada.").ConfigureAwait(false);

        await page.GetByRole(AriaRole.Button, new() { Name = $"Mover {name} para cima" }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync($"{name} agora está antes de").ConfigureAwait(false);

        // Sem JavaScript o "Excluir" abre a página de confirmação
        await page.GetByRole(AriaRole.Link, new() { Name = $"Excluir {name}", Exact = true }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = $"Excluir {name}?" })).ToBeVisibleAsync().ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Excluir" }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Categoria {name} excluída.").ConfigureAwait(false);
    }
}
