using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Photos;

/// <summary>
/// As fotos do rascunho (US-008-S02 a S06) no navegador (roda no /test): enviar várias de uma vez, trocar a capa, remover com confirmação, o limite de 20, as recusas
/// por arquivo, a falha de conexão de uma foto só e o mesmo caminho sem JavaScript. Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL e GAZETA_E2E_PASSWORD (conta de
/// Administrador). O site do E2E precisa de <c>RateLimiting__PhotoUploadsPerMinute=1000</c> (a suíte sobe dezenas de fotos de uma conta só). Cada teste cria o próprio rascunho.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class PhotosE2ETests : SitePage
#pragma warning restore CA1515
{
    private const string PdfMessage = "Formato não aceito. Use JPG, PNG, WebP, GIF ou HEIC";
    private const string TooBigMessage = "A foto excede o limite de 10 MB";

    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";

    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Photos", "Fixtures", name);

    private static string[] ThreePhotos => [Fixture("foto-1.jpg"), Fixture("foto-2.jpg"), Fixture("foto-3.jpg")];

    private static string Describe(AxeResultItem violation) =>
        violation.Id + ": " + violation.Help + " [" + string.Join(" | ", violation.Nodes.Take(3).Select(n => n.Html)) + "]";

    private static async Task SignInAsync(IPage page)
    {
        await page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await page.GetByLabel("E-mail").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL")).ConfigureAwait(false);
        await page.GetByLabel("Senha").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
        await page.WaitForURLAsync(new Regex(@"/painel/(?!entrar)")).ConfigureAwait(false);
    }

    /// <summary>Cria um rascunho só com o título (US-008-S07) e fica na página de edição dele.</summary>
    private static async Task<string> CreateDraftAsync(IPage page, string title = null)
    {
        await page.GotoAsync(Url("/painel/anuncios/novo")).ConfigureAwait(false);
        await page.GetByLabel("Título").FillAsync(title ?? Unique("Fotos")).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).ClickAsync().ConfigureAwait(false);
        await page.GetByText("Rascunho salvo").WaitForAsync().ConfigureAwait(false);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Fotos (0 de 20)" }).WaitForAsync().ConfigureAwait(false);
        return page.Url;
    }

    private static ILocator Gallery(IPage page) => page.GetByRole(AriaRole.List, new() { Name = "Fotos do anúncio" });

    private static ILocator Items(IPage page) => Gallery(page).Locator("> li[data-photo-id]");

    /// <summary>As marcas "Capa" que aparecem na tela (a das outras fotos existe no HTML, mas escondida).</summary>
    private static ILocator VisibleCovers(IPage page) => Gallery(page).GetByText("Capa", new() { Exact = true }).And(page.Locator(":visible"));

    private static async Task<string[]> IdsAsync(IPage page) =>
        await Items(page).EvaluateAllAsync<string[]>("els => els.map(e => e.dataset.photoId)").ConfigureAwait(false);

    private static async Task ChooseAndSendAsync(IPage page, params string[] paths)
    {
        await page.GetByLabel("Escolher fotos").SetInputFilesAsync(paths).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar fotos" }).ClickAsync().ConfigureAwait(false);
    }

    private static List<string> WatchFailures(IPage page)
    {
        List<string> failures = [];
        string origin = new Uri(RequiresVariablesAttribute.Value("GAZETA_BASE_URL")).GetLeftPart(UriPartial.Authority);
        page.Response += (_, response) =>
        {
            if (response.Url.StartsWith(origin, StringComparison.OrdinalIgnoreCase) && response.Status >= 400)
            {
                failures.Add($"{response.Status} {response.Url}");
            }
        };
        return failures;
    }

    private static async Task AssertThumbnailsLoadedAsync(IPage page)
    {
        bool allLoaded = await page.EvaluateAsync<bool>("[...document.querySelectorAll('[data-photo-list] img')].every(i => i.complete && i.naturalWidth > 0)").ConfigureAwait(false);
        Assert.IsTrue(allLoaded, "todas as miniaturas carregaram (a rota de entrega devolveu a imagem)");
    }

    [TestMethod]
    public async Task US008S02_AdicionarFotosAoAnuncio_TresMiniaturasNaOrdemEnviada_ComCapa_ESobrevivemAoSalvarEReabrir()
    {
        List<string> failures = WatchFailures(Page);
        await SignInAsync(Page).ConfigureAwait(false);
        await CreateDraftAsync(Page).ConfigureAwait(false);

        await ChooseAndSendAsync(Page, ThreePhotos).ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Fotos (3 de 20)" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(3).ConfigureAwait(false);
        string[] ids = await IdsAsync(Page).ConfigureAwait(false);
        await Expect(Items(Page).First.GetByText("Capa", new() { Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(VisibleCovers(Page)).ToHaveCountAsync(1).ConfigureAwait(false);
        await Expect(Items(Page).Nth(1).GetByRole(AriaRole.Img)).ToHaveAttributeAsync("alt", "Foto 2").ConfigureAwait(false);
        await AssertThumbnailsLoadedAsync(Page).ConfigureAwait(false);

        // Salvar o rascunho e reabrir: as 3 fotos continuam, na mesma ordem (o botão fica fora do formulário das fotos e salva o anúncio)
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Rascunho salvo")).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(3).ConfigureAwait(false);
        CollectionAssert.AreEqual(ids, await IdsAsync(Page).ConfigureAwait(false), "mesma ordem depois de reabrir");
        await Expect(Items(Page).First.GetByText("Capa", new() { Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);
        await AssertThumbnailsLoadedAsync(Page).ConfigureAwait(false);
        Assert.AreEqual(0, failures.Count, "nenhuma resposta de erro na jornada: " + string.Join(", ", failures));
    }

    [TestMethod]
    public async Task US008S03_TrocarACapaERemoverUmaFoto_ComConfirmacao_SemArrastar()
    {
        List<string> failures = WatchFailures(Page);
        await SignInAsync(Page).ConfigureAwait(false);
        await CreateDraftAsync(Page).ConfigureAwait(false);
        await ChooseAndSendAsync(Page, ThreePhotos).ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(3).ConfigureAwait(false);
        string[] ids = await IdsAsync(Page).ConfigureAwait(false);

        // "Tornar capa" na 3ª foto: ela passa para a primeira posição e recebe a marca
        await Page.GetByRole(AriaRole.Button, new() { Name = "Tornar capa da foto 3" }).ClickAsync().ConfigureAwait(false);
        await Expect(Items(Page).First).ToHaveAttributeAsync("data-photo-id", ids[2]).ConfigureAwait(false);
        await Expect(Items(Page).First.GetByText("Capa", new() { Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(VisibleCovers(Page)).ToHaveCountAsync(1).ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { ids[2], ids[0], ids[1] }, await IdsAsync(Page).ConfigureAwait(false));
        await Expect(Items(Page).First.GetByRole(AriaRole.Button, new() { Name = "Tornar capa" })).ToBeHiddenAsync().ConfigureAwait(false);

        // "Remover" na 2ª: pede confirmação; cancelar não remove
        await Page.GetByRole(AriaRole.Button, new() { Name = "Remover foto 2" }).ClickAsync().ConfigureAwait(false);
        ILocator dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Remover esta foto?" });
        await Expect(dialog).ToBeVisibleAsync().ConfigureAwait(false);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancelar" }).ClickAsync().ConfigureAwait(false);
        await Expect(dialog).ToBeHiddenAsync().ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(3).ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Remover foto 2" }).ClickAsync().ConfigureAwait(false);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Remover foto" }).ClickAsync().ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(2).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Fotos (2 de 20)" })).ToBeVisibleAsync().ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { ids[2], ids[1] }, await IdsAsync(Page).ConfigureAwait(false));

        // Só vale o que o servidor guardou: depois de recarregar, a ordem e a quantidade são as mesmas
        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(2).ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { ids[2], ids[1] }, await IdsAsync(Page).ConfigureAwait(false));
        await Expect(Items(Page).First.GetByText("Capa", new() { Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);
        Assert.AreEqual(0, failures.Count, "nenhuma resposta de erro na jornada: " + string.Join(", ", failures));
    }

    [TestMethod]
    public async Task US008S04_PassarDoLimiteDe20Fotos_MostraAMensagem_EOAnuncioContinuaCom20()
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await CreateDraftAsync(Page).ConfigureAwait(false);
        FilePayload[] twenty = [.. Enumerable.Range(1, 20).Select(i => new FilePayload { Name = $"foto-{i}.jpg", MimeType = "image/jpeg", Buffer = File.ReadAllBytes(Fixture("foto-1.jpg")) })];
        await Page.GetByLabel("Escolher fotos").SetInputFilesAsync(twenty).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar fotos" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Fotos (20 de 20)" })).ToBeVisibleAsync(new() { Timeout = 90_000 }).ConfigureAwait(false);

        await ChooseAndSendAsync(Page, Fixture("foto-2.jpg")).ConfigureAwait(false);

        await Expect(Page.GetByText("Cada anúncio pode ter no máximo 20 fotos")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(20).ConfigureAwait(false);
        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(20).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US008S05_PdfEFotoDe15MB_RecebemAsMensagensDaSpec_ENenhumEhAdicionado()
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await CreateDraftAsync(Page).ConfigureAwait(false);
        byte[] fifteenMb = new byte[15 * 1024 * 1024];
        fifteenMb[0] = 0xFF;
        fifteenMb[1] = 0xD8;

        await Page.GetByLabel("Escolher fotos").SetInputFilesAsync(
        [
            new FilePayload { Name = "contrato.pdf", MimeType = "application/pdf", Buffer = File.ReadAllBytes(Fixture("contrato.pdf")) },
            new FilePayload { Name = "grande.jpg", MimeType = "image/jpeg", Buffer = fifteenMb }
        ]).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar fotos" }).ClickAsync().ConfigureAwait(false);

        ILocator pdfTile = Page.Locator("[data-photo-pending]", new() { HasText = "contrato.pdf" });
        ILocator bigTile = Page.Locator("[data-photo-pending]", new() { HasText = "grande.jpg" });
        await Expect(pdfTile.GetByText(PdfMessage)).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(bigTile.GetByText(TooBigMessage)).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Fotos (0 de 20)" })).ToBeVisibleAsync().ConfigureAwait(false);

        // "Descartar" tira o quadro da lista; recarregar mostra que nada foi adicionado
        await pdfTile.GetByRole(AriaRole.Button, new() { Name = "Descartar" }).ClickAsync().ConfigureAwait(false);
        await Expect(pdfTile).ToHaveCountAsync(0).ConfigureAwait(false);
        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Fotos (0 de 20)" })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US008S06_FalhaDeConexaoNoEnvio_MarcaSoAquelaFoto_ComTentarDeNovo_EOsTextosContinuam()
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await CreateDraftAsync(Page).ConfigureAwait(false);
        string title = Unique("Título que não some");
        await Page.GetByLabel("Título").FillAsync(title).ConfigureAwait(false);
        await Page.GetByLabel("Descrição").FillAsync("Descrição já preenchida").ConfigureAwait(false);
        await ChooseAndSendAsync(Page, Fixture("foto-1.jpg")).ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(1).ConfigureAwait(false);

        // A conexão cai durante o envio da segunda foto
        await Page.RouteAsync("**/api/v1/ads/*/photos", route => route.AbortAsync()).ConfigureAwait(false);
        await ChooseAndSendAsync(Page, Fixture("foto-2.jpg")).ConfigureAwait(false);

        ILocator failedTile = Page.Locator("[data-photo-pending]", new() { HasText = "foto-2.jpg" });
        await Expect(failedTile.GetByText("Falha ao enviar")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(failedTile.GetByRole(AriaRole.Button, new() { Name = "Tentar de novo" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(1).ConfigureAwait(false);
        await Expect(Page.GetByLabel("Título")).ToHaveValueAsync(title).ConfigureAwait(false);
        await Expect(Page.GetByLabel("Descrição")).ToHaveValueAsync("Descrição já preenchida").ConfigureAwait(false);

        // Com a conexão de volta, "Tentar de novo" envia só aquela foto
        await Page.UnrouteAsync("**/api/v1/ads/*/photos").ConfigureAwait(false);
        await failedTile.GetByRole(AriaRole.Button, new() { Name = "Tentar de novo" }).ClickAsync().ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(2).ConfigureAwait(false);
        await Expect(failedTile).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Fotos (2 de 20)" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SemJavaScript_EnviarTrocarACapaERemover_PorFormulariosComuns()
    {
        await using IBrowserContext context = await Browser.NewContextAsync(new() { JavaScriptEnabled = false, IgnoreHTTPSErrors = true }).ConfigureAwait(false);
        IPage page = await context.NewPageAsync().ConfigureAwait(false);
        await SignInAsync(page).ConfigureAwait(false);
        await CreateDraftAsync(page).ConfigureAwait(false);

        // Uma foto por vez; a página recarrega com o resultado
        await ChooseAndSendAsync(page, Fixture("foto-1.jpg")).ConfigureAwait(false);
        await Expect(page.GetByText("Foto adicionada.")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Fotos (1 de 20)" })).ToBeVisibleAsync().ConfigureAwait(false);
        await ChooseAndSendAsync(page, Fixture("foto-2.jpg")).ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Fotos (2 de 20)" })).ToBeVisibleAsync().ConfigureAwait(false);
        string[] ids = await IdsAsync(page).ConfigureAwait(false);

        await page.GetByRole(AriaRole.Button, new() { Name = "Tornar capa da foto 2" }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByText("Capa alterada.")).ToBeVisibleAsync().ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { ids[1], ids[0] }, await IdsAsync(page).ConfigureAwait(false));

        // Remover leva a uma página de confirmação
        await page.GetByRole(AriaRole.Button, new() { Name = "Remover foto 2" }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Remover esta foto?" })).ToBeVisibleAsync().ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Remover foto" }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByText("Foto removida.")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Fotos (1 de 20)" })).ToBeVisibleAsync().ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { ids[1] }, await IdsAsync(page).ConfigureAwait(false));
    }

    [TestMethod]
    public async Task Acessibilidade_SecaoDeFotos_SemViolacoes_ENaoRolaNaHorizontalEm320px()
    {
        await SignInAsync(Page).ConfigureAwait(false);
        await CreateDraftAsync(Page).ConfigureAwait(false);
        await ChooseAndSendAsync(Page, ThreePhotos).ConfigureAwait(false);
        await Expect(Items(Page)).ToHaveCountAsync(3).ConfigureAwait(false);
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };

        AxeResult page = await Page.RunAxe(options).ConfigureAwait(false);
        Assert.AreEqual(0, page.Violations.Length, "página com 3 fotos: " + string.Join("; ", page.Violations.Select(Describe)));

        // Com a janela de confirmação aberta e com um quadro de recusa na lista
        await Page.GetByRole(AriaRole.Button, new() { Name = "Remover foto 2" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync().ConfigureAwait(false);
        AxeResult dialog = await Page.RunAxe(options).ConfigureAwait(false);
        Assert.AreEqual(0, dialog.Violations.Length, "com a confirmação aberta: " + string.Join("; ", dialog.Violations.Select(Describe)));
        await Page.GetByRole(AriaRole.Button, new() { Name = "Cancelar" }).ClickAsync().ConfigureAwait(false);

        await Page.GetByLabel("Escolher fotos").SetInputFilesAsync(new FilePayload { Name = "contrato.pdf", MimeType = "application/pdf", Buffer = File.ReadAllBytes(Fixture("contrato.pdf")) }).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar fotos" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByText(PdfMessage)).ToBeVisibleAsync().ConfigureAwait(false);
        // O mouse sai de cima do botão e a transição de cor do Bootstrap termina antes da medição (senão o axe mede uma cor no meio do caminho)
        await Page.Mouse.MoveAsync(0, 0).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Enviar fotos" })).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)").ConfigureAwait(false);
        AxeResult refused = await Page.RunAxe(options).ConfigureAwait(false);
        Assert.AreEqual(0, refused.Violations.Length, "com a recusa na lista: " + string.Join("; ", refused.Violations.Select(Describe)));

        await Page.SetViewportSizeAsync(320, 800).ConfigureAwait(false);
        bool overflows = await Page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth").ConfigureAwait(false);
        Assert.IsFalse(overflows, "a galeria não rola na horizontal em 320 px");
    }
}
