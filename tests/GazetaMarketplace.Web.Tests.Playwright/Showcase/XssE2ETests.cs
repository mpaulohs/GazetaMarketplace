using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Showcase;

/// <summary>
/// NFR-15 no navegador de verdade, no site publicado: texto digitado com código dentro (tag de script, imagem com <c>onerror</c>, aspas, "&amp;") é cadastrado e aberto nas telas do painel e do site
/// público. Em nenhuma delas pode abrir uma janela (<c>alert</c>), existir <c>window.__xss</c> (o que o código do ataque tentaria criar) ou nascer uma imagem do ataque; e o texto aparece <b>literal</b>, do jeito que
/// a pessoa digitou. Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class XssE2ETests : SitePage
#pragma warning restore CA1515
{
    private const string Marker = "<script>alert(1)</script>";

    // O que cada ataque tenta: abrir uma janela, ou criar window.__xss
    private const string Attack = "<img src=x onerror=window.__xss=1><script>alert(1)</script>\"'&";

    private static string BaseUrl => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/');

    private static string Url(string path) => BaseUrl + path;

    /// <summary>Vigia de uma página: guarda cada janela que abrir e confere, em cada passo, que o código do ataque não rodou.</summary>
    private sealed class Watch
    {
        public List<string> Dialogs { get; } = [];

        public static Watch On(IPage page)
        {
            Watch watch = new();
            page.Dialog += async (_, dialog) =>
            {
                watch.Dialogs.Add(dialog.Message);
                await dialog.DismissAsync().ConfigureAwait(false);
            };
            return watch;
        }

        public async Task AssertCleanAsync(IPage page, string where)
        {
            Assert.IsEmpty(Dialogs, $"{where}: o ataque abriu uma janela: {string.Join(" | ", Dialogs)}");
            Assert.IsTrue(await page.EvaluateAsync<bool>("() => typeof window.__xss === 'undefined'").ConfigureAwait(false), $"{where}: o código do ataque rodou (window.__xss existe)");
            Assert.AreEqual(0, await page.Locator("img[src='x']").CountAsync().ConfigureAwait(false), $"{where}: nasceu a imagem do ataque");
        }
    }

    private async Task EnsureAdminAsync()
    {
        await Page.GotoAsync(Url("/painel/anuncios")).ConfigureAwait(false);
        if (Page.Url.Contains("/entrar", StringComparison.Ordinal))
        {
            await PublishingFlow.SignInAdminAsync(Page).ConfigureAwait(false);
        }
    }

    private async Task SetPhoneAsync()
    {
        await Page.GotoAsync(Url("/painel/configuracoes")).ConfigureAwait(false);
        await Page.GetByLabel(new Regex("^Telefone/WhatsApp do site")).FillAsync("11912345678").ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Configurações salvas").ConfigureAwait(false);
    }

    private async Task<(IPage Page, Watch Watch)> VisitorAsync()
    {
        IBrowserContext context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true }).ConfigureAwait(false);
        IPage page = await context.NewPageAsync().ConfigureAwait(false);
        return (page, Watch.On(page));
    }

    [TestMethod]
    public async Task AdWithHostileTitleAndDescription_IsPublishedAndShownLiterally_OnPanelAndPublicScreens()
    {
        using FakeViaCep viaCep = new();
        Watch panel = Watch.On(Page);
        await EnsureAdminAsync().ConfigureAwait(false);
        await SetPhoneAsync().ConfigureAwait(false);

        // O cadastro inteiro (formulário, confirmação, fila, pré-visualização, publicar) já abre as telas do painel com o texto hostil
        string title = await PublishingFlow.PublishAsync(Page, Attack, 1, description: Attack + "\n" + Attack).ConfigureAwait(false);
        await panel.AssertCleanAsync(Page, "painel, depois de publicar").ConfigureAwait(false);

        await Page.GotoAsync(Url("/painel/anuncios")).ConfigureAwait(false);
        await Expect(Page.Locator("body")).ToContainTextAsync(Marker).ConfigureAwait(false);
        await panel.AssertCleanAsync(Page, "lista do painel").ConfigureAwait(false);

        (IPage visitor, Watch watch) = await VisitorAsync().ConfigureAwait(false);
        await visitor.GotoAsync(Url("/")).ConfigureAwait(false);
        ILocator card = visitor.Locator("[data-ad-card]").Filter(new() { HasText = title });
        await Expect(card.First).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(card.First).ToContainTextAsync(Marker).ConfigureAwait(false);
        await watch.AssertCleanAsync(visitor, "início").ConfigureAwait(false);

        // A página do anúncio: título, descrição, contato e o título da aba mostram o texto como texto
        await card.First.GetByRole(AriaRole.Link).First.ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync(Marker).ConfigureAwait(false);
        await Expect(visitor.Locator("body")).ToContainTextAsync(Marker).ConfigureAwait(false);
        Assert.IsTrue((await visitor.TitleAsync().ConfigureAwait(false)).Contains(Marker, StringComparison.Ordinal), "o título da aba mostra o texto literal, sem fechar o <title>");
        await watch.AssertCleanAsync(visitor, "página do anúncio").ConfigureAwait(false);
        string adPath = new Uri(visitor.Url).AbsolutePath;

        // Favoritar e abrir "Meus favoritos" (o card volta de um pedido ao servidor e entra na página por JavaScript)
        await visitor.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Favoritar") }).First.ClickAsync().ConfigureAwait(false);
        await visitor.GotoAsync(Url("/favoritos")).ConfigureAwait(false);
        await Expect(visitor.Locator("[data-ad-card]").Filter(new() { HasText = title }).First).ToContainTextAsync(Marker).ConfigureAwait(false);
        await watch.AssertCleanAsync(visitor, "favoritos").ConfigureAwait(false);

        // A busca pelo texto digitado devolve o anúncio e o campo mostra o texto literal
        await visitor.GotoAsync(Url("/busca?q=" + Uri.EscapeDataString(title))).ConfigureAwait(false);
        await Expect(visitor.Locator("[data-ad-card]").Filter(new() { HasText = title }).First).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("#busca-q")).ToHaveValueAsync(title).ConfigureAwait(false);
        await watch.AssertCleanAsync(visitor, "busca").ConfigureAwait(false);
        Assert.IsTrue(adPath.StartsWith("/anuncio/", StringComparison.Ordinal));
        Assert.IsFalse(adPath.Contains('<', StringComparison.Ordinal) || adPath.Contains('"', StringComparison.Ordinal), "o endereço do anúncio não leva marcação: " + adPath);
    }

    [TestMethod]
    public async Task UserNameAndCategoryNameWithAttack_AreShownLiterally_InPanelAndSite()
    {
        Watch panel = Watch.On(Page);
        await EnsureAdminAsync().ConfigureAwait(false);
        string unique = Guid.NewGuid().ToString("N")[..8];
        string name = Attack + " " + unique;
        string email = $"xss-{unique}@exemplo.com.br";

        await Page.GotoAsync(Url("/painel/usuarios/novo")).ConfigureAwait(false);
        await Page.GetByLabel("Nome").FillAsync(name).ConfigureAwait(false);
        await Page.GetByLabel("E-mail").FillAsync(email).ConfigureAwait(false);
        await Page.GetByLabel("Redator").CheckAsync().ConfigureAwait(false);
        await Page.GetByLabel("Senha provisória").FillAsync("Provis0ria@xss").ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync(Marker).ConfigureAwait(false);
        await panel.AssertCleanAsync(Page, "mensagem de usuário criado").ConfigureAwait(false);
        await Expect(Page.Locator("tr", new() { HasText = unique })).ToContainTextAsync(Marker).ConfigureAwait(false);
        await panel.AssertCleanAsync(Page, "lista de usuários").ConfigureAwait(false);

        // Desativa a conta de teste para ela não ficar ativa no site de verificação
        await Page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^Desativar .*" + unique) }).First.ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync(Marker).ConfigureAwait(false);
        await panel.AssertCleanAsync(Page, "confirmar desativação").ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Desativar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync(Marker).ConfigureAwait(false);
        await panel.AssertCleanAsync(Page, "conta desativada").ConfigureAwait(false);

        // Categoria com nome de ataque: lista do painel, menu do site (visitante) e exclusão
        string category = Attack + " " + unique;
        await Page.GotoAsync(Url("/painel/categorias")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Nova categoria" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByLabel(new Regex("^Nome")).FillAsync(category).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.Locator("body")).ToContainTextAsync(Marker).ConfigureAwait(false);
        await panel.AssertCleanAsync(Page, "lista de categorias").ConfigureAwait(false);

        (IPage visitor, Watch watch) = await VisitorAsync().ConfigureAwait(false);
        await visitor.GotoAsync(Url("/")).ConfigureAwait(false);
        await Expect(visitor.Locator("body")).ToContainTextAsync(unique).ConfigureAwait(false);
        await watch.AssertCleanAsync(visitor, "início com a categoria").ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^Excluir .*" + unique) }).First.ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Alertdialog).GetByRole(AriaRole.Button, new() { Name = "Excluir" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.Locator(".categorias-arvore")).Not.ToContainTextAsync(unique).ConfigureAwait(false); // a mensagem de sucesso ainda cita o nome; a lista, não
        await panel.AssertCleanAsync(Page, "categoria excluída").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RejectionReasonWithAttack_IsShownLiterally_ToTheAuthor()
    {
        using FakeViaCep viaCep = new();
        Watch panel = Watch.On(Page);
        await EnsureAdminAsync().ConfigureAwait(false);
        string title = await PublishingFlow.SubmitAsync(Page, "Livro XSS motivo").ConfigureAwait(false);

        await Page.GotoAsync(Url("/painel/anuncios/fila")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Row).Filter(new() { HasText = title }).GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync().ConfigureAwait(false);
        int adId = int.Parse(Regex.Match(Page.Url, @"/anuncios/(\d+)/").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Rejeitar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByLabel("Motivo da rejeição").FillAsync(Attack).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Rejeitar anúncio" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio rejeitado" })).ToBeVisibleAsync().ConfigureAwait(false);
        await panel.AssertCleanAsync(Page, "fila depois de rejeitar").ConfigureAwait(false);

        await Page.GotoAsync(Url($"/painel/anuncios/{adId}/editar")).ConfigureAwait(false);
        await Expect(Page.GetByText("Este anúncio foi rejeitado.")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.Locator("body")).ToContainTextAsync("Motivo: " + Attack).ConfigureAwait(false);
        await panel.AssertCleanAsync(Page, "editar anúncio rejeitado").ConfigureAwait(false);

        await Page.GotoAsync(Url("/painel/anuncios")).ConfigureAwait(false);
        await Expect(Page.Locator("tr", new() { HasText = title })).ToContainTextAsync("Motivo da rejeição: " + Attack).ConfigureAwait(false);
        await panel.AssertCleanAsync(Page, "lista com o motivo").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SearchTextWithAttack_ComesBackLiteral_FromTheAddressAndFromTheTypedField()
    {
        (IPage visitor, Watch watch) = await VisitorAsync().ConfigureAwait(false);
        string[] payloads = ["<script>alert(1)</script>", "\"><img src=x onerror=window.__xss=1>", "'", "&", "</title><script>alert(1)</script>", "javascript:alert(1)", Attack];

        foreach (string payload in payloads)
        {
            await visitor.GotoAsync(Url("/busca?q=" + Uri.EscapeDataString(payload))).ConfigureAwait(false);
            await Expect(visitor.Locator("#busca-q")).ToHaveValueAsync(payload).ConfigureAwait(false);
            await watch.AssertCleanAsync(visitor, "busca pelo endereço: " + payload).ConfigureAwait(false);
        }

        // Digitado no campo e enviado pelo botão (o caminho do uso comum)
        await visitor.GotoAsync(Url("/")).ConfigureAwait(false);
        await visitor.Locator("#busca-q").First.FillAsync(Attack).ConfigureAwait(false);
        await visitor.Locator("#busca-q").First.PressAsync("Enter").ConfigureAwait(false);
        await visitor.WaitForURLAsync(new Regex("/busca")).ConfigureAwait(false);
        await Expect(visitor.Locator("#busca-q")).ToHaveValueAsync(Attack).ConfigureAwait(false);
        await Expect(visitor.Locator("main")).ToContainTextAsync(Marker).ConfigureAwait(false);
        await watch.AssertCleanAsync(visitor, "busca digitada").ConfigureAwait(false);
    }
}
