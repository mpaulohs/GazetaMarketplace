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
/// Publicar e rejeitar (4.2) no navegador, no site publicado: o Administrador publica pela página de confirmação (a foto passa a chegar ao visitante), rejeita com motivo
/// (que aparece na tela de edição de quem cadastrou), é avisado quando esquece o motivo, perde a corrida contra outra janela (US-010-S07) e o axe não acha violações.
/// A conta do E2E é de Administrador e cadastra os anúncios, então ela também é a "autora" que lê o motivo. O telefone do site já está configurado no banco de teste;
/// a falta dele (US-010-S08) é provada nos testes HTTP. Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class ReviewDecisionE2ETests : SitePage
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
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).Last.ClickAsync().ConfigureAwait(false);
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

    /// <summary>Do cartão do anúncio na fila até a pré-visualização; devolve o id.</summary>
    private async Task<int> OpenPreviewAsync(string title)
    {
        await Page.GotoAsync(Url("/painel/anuncios/fila")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Row).Filter(new() { HasText = title }).GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/anuncios/\d+/pre-visualizacao$")).ConfigureAwait(false);
        return int.Parse(Regex.Match(Page.Url, @"/anuncios/(\d+)/").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Describe(AxeResultItem violation) =>
        violation.Id + ": " + violation.Help + " [" + string.Join(" | ", violation.Nodes.Take(3).Select(n => n.Html + " => " + string.Join("; ", n.Any.Select(a => a.Message)))) + "]";

    [TestMethod]
    public async Task US010S03_Publicar_PelaConfirmacao_SaiDaFila_EAFotoPassaAChegarAoVisitante()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        string title = Unique("Livro a publicar");
        await SubmitAdAsync(title).ConfigureAwait(false);
        int adId = await OpenPreviewAsync(title).ConfigureAwait(false);
        string photo = (await Page.Locator("[data-ad-photos] img").First.GetAttributeAsync("src").ConfigureAwait(false))!;

        IBrowserContext visitorContext = await Browser.NewContextAsync(ContextOptions()).ConfigureAwait(false);
        Assert.AreEqual(404, (await visitorContext.APIRequest.GetAsync(Url(photo)).ConfigureAwait(false)).Status, "antes de publicar, o visitante não recebe a foto");

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Publicar" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Rejeitar" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Editar" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Publicar este anúncio?", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Ele passa a aparecer no site para todos os visitantes.")).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);

        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/anuncios/fila$")).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio publicado" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText(title)).ToHaveCountAsync(0).ConfigureAwait(false);

        // Só vale o que o servidor guardou: recarregar a fila e olhar a pré-visualização (já sem a faixa de "não publicado" e sem botões)
        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Page.GetByText(title)).ToHaveCountAsync(0).ConfigureAwait(false);
        await Page.GotoAsync(Url($"/painel/anuncios/{adId}/pre-visualizacao")).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = title, Level = 2 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Pré-visualização — ainda não publicado")).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^(Publicar|Rejeitar)$") })).ToHaveCountAsync(0).ConfigureAwait(false);

        IAPIResponse delivered = await visitorContext.APIRequest.GetAsync(Url(photo)).ConfigureAwait(false);
        Assert.AreEqual(200, delivered.Status, "depois de publicar, o visitante recebe a foto");
        StringAssert.StartsWith(delivered.Headers["content-type"], "image/");
    }

    [TestMethod]
    public async Task US010S04_Rejeitar_ComMotivo_SaiDaFila_EOMotivoAparecePraQuemCadastrou()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        string title = Unique("Livro a rejeitar");
        const string Reason = "Fotos escuras; envie fotos com boa iluminação";
        await SubmitAdAsync(title).ConfigureAwait(false);
        int adId = await OpenPreviewAsync(title).ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Link, new() { Name = "Rejeitar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Rejeitar anúncio", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GetByLabel("Motivo da rejeição").FillAsync("  " + Reason + "  ").ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Rejeitar anúncio" }).ClickAsync().ConfigureAwait(false);

        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/anuncios/fila$")).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio rejeitado" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText(title)).ToHaveCountAsync(0).ConfigureAwait(false);

        // Quem cadastrou (aqui, a mesma conta) abre o anúncio para editar e lê o motivo, sem os espaços das pontas
        await Page.GotoAsync(Url($"/painel/anuncios/{adId}/editar")).ConfigureAwait(false);
        await Expect(Page.GetByText("Este anúncio foi rejeitado.")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Motivo: " + Reason)).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Motivo: " + Reason)).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US010S05_Rejeitar_SemMotivo_AvisaNaPropriaPagina_EOAnuncioContinuaEmRevisao()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        string title = Unique("Livro sem motivo");
        await SubmitAdAsync(title).ConfigureAwait(false);
        await OpenPreviewAsync(title).ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Link, new() { Name = "Rejeitar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByLabel("Motivo da rejeição").FillAsync("   ").ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Rejeitar anúncio" }).ClickAsync().ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Informe o motivo da rejeição" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Motivo da rejeição")).ToBeFocusedAsync().ConfigureAwait(false);
        await Expect(Page.GetByLabel("Motivo da rejeição")).ToHaveAttributeAsync("aria-invalid", "true").ConfigureAwait(false);
        // O botão volta a funcionar para a nova tentativa
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Rejeitar anúncio" })).ToBeEnabledAsync().ConfigureAwait(false);

        await Page.GotoAsync(Url("/painel/anuncios/fila")).ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = title })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US010S07_DuasJanelas_QuemChegaDepoisVeOAvisoDeQueJaFoiPublicado()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        string title = Unique("Livro disputado");
        await SubmitAdAsync(title).ConfigureAwait(false);
        int adId = await OpenPreviewAsync(title).ConfigureAwait(false);

        // A segunda janela (outra sessão da mesma conta) abre a confirmação enquanto o anúncio ainda está em revisão
        IBrowserContext separate = await Browser.NewContextAsync(ContextOptions()).ConfigureAwait(false);
        IPage second = await separate.NewPageAsync().ConfigureAwait(false);
        await SignInAsync(second, RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL"), RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await second.GotoAsync(Url($"/painel/anuncios/{adId}/publicar")).ConfigureAwait(false);
        await Expect(second.GetByRole(AriaRole.Heading, new() { Name = "Publicar este anúncio?" })).ToBeVisibleAsync().ConfigureAwait(false);

        // A primeira janela publica primeiro
        await Page.GetByRole(AriaRole.Link, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio publicado" })).ToBeVisibleAsync().ConfigureAwait(false);

        // A segunda confirma depois: o aviso aparece na pré-visualização e a situação continua Publicado
        await second.GetByRole(AriaRole.Button, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await Expect(second).ToHaveURLAsync(new Regex(@"/painel/anuncios/\d+/pre-visualizacao$")).ConfigureAwait(false);
        await Expect(second.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Este anúncio já foi publicado" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(second.GetByText("Pré-visualização — ainda não publicado")).ToHaveCountAsync(0).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Acessibilidade_ConfirmarEPaginaDeRejeicao_SemViolacoes_SemRolagemHorizontal()
    {
        using FakeViaCep viaCep = new();
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };
        await SignInAdminAsync().ConfigureAwait(false);
        string title = Unique("Livro acessível");
        await SubmitAdAsync(title).ConfigureAwait(false);
        int adId = await OpenPreviewAsync(title).ConfigureAwait(false);

        foreach (int width in new[] { 1280, 320 })
        {
            await Page.SetViewportSizeAsync(width, 900).ConfigureAwait(false);
            foreach (string path in new[] { $"/painel/anuncios/{adId}/publicar", $"/painel/anuncios/{adId}/rejeitar", $"/painel/anuncios/{adId}/pre-visualizacao" })
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

        // A página de rejeição com o erro de "motivo obrigatório" também passa no axe
        await Page.GotoAsync(Url($"/painel/anuncios/{adId}/rejeitar")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Rejeitar anúncio" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Informe o motivo da rejeição" })).ToBeVisibleAsync().ConfigureAwait(false);
        AxeResult withError = await Page.RunAxe(options).ConfigureAwait(false);
        Assert.AreEqual(0, withError.Violations.Length, "rejeição com erro: " + string.Join("; ", withError.Violations.Select(Describe)));
    }
}
