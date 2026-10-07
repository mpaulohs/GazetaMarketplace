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
/// Despublicar e arquivar (4.3) no navegador, no site publicado: o Administrador despublica (volta a Rascunho e a foto sai do ar), cancela a confirmação sem efeito,
/// arquiva (tela do anúncio arquivado sem ações de retirada, foto fora do ar), arquiva um anúncio ainda em revisão pela pré-visualização e o axe não acha violações.
/// O foco inicial das confirmações fica em "Cancelar". A conta do E2E é de Administrador e cadastra os anúncios; o Redator é provado nos testes HTTP.
/// Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class TakedownE2ETests : SitePage
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
        // Espera a página de confirmação: sem isso o segundo clique pode cair no botão da página que está saindo e reenviar o mesmo formulário
        await Microsoft.Playwright.Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Enviar para revisão?" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Anúncio enviado para revisão")).ToBeVisibleAsync().ConfigureAwait(false);
    }

    private static string Describe(AxeResultItem violation) =>
        violation.Id + ": " + violation.Help + " [" + string.Join(" | ", violation.Nodes.Take(3).Select(n => n.Html + " => " + string.Join("; ", n.Any.Select(a => a.Message)))) + "]";

    /// <summary>Da fila à pré-visualização do anúncio recém-enviado; devolve o id e o endereço da foto de destaque.</summary>
    private async Task<(int AdId, string Photo)> OpenPreviewAsync(string title)
    {
        await Page.GotoAsync(Url("/painel/anuncios/fila")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Row).Filter(new() { HasText = title }).GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/anuncios/\d+/pre-visualizacao$")).ConfigureAwait(false);
        int id = int.Parse(Regex.Match(Page.Url, @"/anuncios/(\d+)/").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        string photo = (await Page.Locator("[data-ad-photos] img").First.GetAttributeAsync("src").ConfigureAwait(false))!;
        return (id, photo);
    }

    /// <summary>Envia o anúncio para revisão e o publica pelas telas, como a equipe faria.</summary>
    private async Task<(int AdId, string Photo)> PublishedAdAsync(string title)
    {
        await SubmitAdAsync(title).ConfigureAwait(false);
        (int adId, string photo) = await OpenPreviewAsync(title).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio publicado" })).ToBeVisibleAsync().ConfigureAwait(false);
        return (adId, photo);
    }

    private async Task<IAPIRequestContext> VisitorAsync()
    {
        IBrowserContext visitor = await Browser.NewContextAsync(ContextOptions()).ConfigureAwait(false);
        return visitor.APIRequest;
    }

    [TestMethod]
    public async Task US011S01_Despublicar_VoltaARascunho_EAFotoSaiDoAr()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        string title = Unique("Livro a despublicar");
        (int adId, string photo) = await PublishedAdAsync(title).ConfigureAwait(false);
        IAPIRequestContext visitor = await VisitorAsync().ConfigureAwait(false);
        Assert.AreEqual(200, (await visitor.GetAsync(Url(photo)).ConfigureAwait(false)).Status, "publicado: o visitante recebe a foto");

        await Page.GotoAsync(Url($"/painel/anuncios/{adId}/editar")).ConfigureAwait(false);
        await Expect(Page.GetByText("Situação:")).ToContainTextAsync("Publicado").ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Despublicar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Despublicar este anúncio?", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Ele sai do site agora e volta a Rascunho.")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Cancelar" })).ToBeFocusedAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Despublicar" }).ClickAsync().ConfigureAwait(false);

        await Expect(Page).ToHaveURLAsync(new Regex($@"/painel/anuncios/{adId}/editar$")).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio despublicado" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Situação:")).ToContainTextAsync("Rascunho").ConfigureAwait(false);
        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Situação:")).ToContainTextAsync("Rascunho").ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Despublicar" })).ToHaveCountAsync(0).ConfigureAwait(false);
        Assert.AreEqual(404, (await visitor.GetAsync(Url(photo)).ConfigureAwait(false)).Status, "despublicado: a foto sai do ar");
    }

    [TestMethod]
    public async Task US011S03_S02_S06_Arquivar_CancelarNaoMudaNada_ConfirmarArquivaEDeixaSoLeitura()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        string title = Unique("Livro a arquivar");
        (int adId, string photo) = await PublishedAdAsync(title).ConfigureAwait(false);
        IAPIRequestContext visitor = await VisitorAsync().ConfigureAwait(false);

        // Cancelar: a situação continua Publicado e a foto continua no site
        await Page.GotoAsync(Url($"/painel/anuncios/{adId}/editar")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Arquivar este anúncio?", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("O anúncio sairá do site e não poderá ser reativado.")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Cancelar" })).ToBeFocusedAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Cancelar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex($@"/painel/anuncios/{adId}/editar$")).ConfigureAwait(false);
        await Expect(Page.GetByText("Situação:")).ToContainTextAsync("Publicado").ConfigureAwait(false);
        Assert.AreEqual(200, (await visitor.GetAsync(Url(photo)).ConfigureAwait(false)).Status, "cancelou: o anúncio continua no site");

        // Confirmar: arquivado, de volta à lista com o aviso; a tela do anúncio fica só para leitura e sem ações de retirada
        await Page.GetByRole(AriaRole.Link, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/anuncios$")).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio arquivado" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GotoAsync(Url($"/painel/anuncios/{adId}/editar")).ConfigureAwait(false);
        await Expect(Page.GetByText("Situação:")).ToContainTextAsync("Arquivado").ConfigureAwait(false);
        await Expect(Page.GetByText("Este anúncio não pode ser editado")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^(Despublicar|Arquivar)$") })).ToHaveCountAsync(0).ConfigureAwait(false);
        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Situação:")).ToContainTextAsync("Arquivado").ConfigureAwait(false);
        Assert.AreEqual(404, (await visitor.GetAsync(Url(photo)).ConfigureAwait(false)).Status, "arquivado: a foto sai do ar");
    }

    [TestMethod]
    public async Task US011S05_Arquivar_AnuncioEmRevisao_PelaPreVisualizacao_SaiDaFila()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        string title = Unique("Livro ainda em revisão");
        await SubmitAdAsync(title).ConfigureAwait(false);
        await OpenPreviewAsync(title).ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Despublicar" })).ToHaveCountAsync(0).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio arquivado" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GotoAsync(Url("/painel/anuncios/fila")).ConfigureAwait(false);
        await Expect(Page.GetByText(title)).ToHaveCountAsync(0).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Acessibilidade_BarraEConfirmacoes_SemViolacoes_SemRolagemHorizontal()
    {
        using FakeViaCep viaCep = new();
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };
        await SignInAdminAsync().ConfigureAwait(false);
        string title = Unique("Livro acessível");
        (int adId, _) = await PublishedAdAsync(title).ConfigureAwait(false);

        foreach (int width in new[] { 1280, 320 })
        {
            await Page.SetViewportSizeAsync(width, 900).ConfigureAwait(false);
            foreach (string path in new[] { $"/painel/anuncios/{adId}/editar", $"/painel/anuncios/{adId}/despublicar", $"/painel/anuncios/{adId}/arquivar" })
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

        // Em 320 px os botões da barra empilham, com "Arquivar" por último
        await Page.GotoAsync(Url($"/painel/anuncios/{adId}/editar")).ConfigureAwait(false);
        ILocator unpublish = Page.GetByRole(AriaRole.Link, new() { Name = "Despublicar" });
        ILocator archive = Page.GetByRole(AriaRole.Link, new() { Name = "Arquivar" });
        LocatorBoundingBoxResult first = await unpublish.BoundingBoxAsync().ConfigureAwait(false);
        LocatorBoundingBoxResult last = await archive.BoundingBoxAsync().ConfigureAwait(false);
        Assert.IsTrue(last.Y > first.Y, "Arquivar fica abaixo de Despublicar em 320 px");

        // O anúncio arquivado, só para leitura, também passa no axe
        await Page.GotoAsync(Url($"/painel/anuncios/{adId}/arquivar")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Page.GotoAsync(Url($"/painel/anuncios/{adId}/editar")).ConfigureAwait(false);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        AxeResult archived = await Page.RunAxe(options).ConfigureAwait(false);
        Assert.AreEqual(0, archived.Violations.Length, "anúncio arquivado: " + string.Join("; ", archived.Violations.Select(Describe)));
    }
}
