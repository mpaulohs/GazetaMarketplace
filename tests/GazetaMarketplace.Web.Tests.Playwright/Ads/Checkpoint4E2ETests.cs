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
/// Checkpoint 4 no navegador, no site publicado, com contas de verdade de dois papéis: o ciclo de vida inteiro do anúncio (enviar, rejeitar, corrigir e reenviar, publicar, despublicar,
/// reenviar, publicar, arquivar) visto pelo Redator (em "Meus anúncios") e pelo Administrador (fila, pré-visualização, edição e lista), com a foto entregue ao visitante só enquanto o
/// anúncio está Publicado; e dois Administradores decidindo o mesmo anúncio (quem confirma depois lê "por outro administrador"). Cada teste cria as contas pela tela de usuários
/// (um Redator e, no segundo teste, também um Administrador), que ficam no banco de teste.
/// Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class Checkpoint4E2ETests : SitePage
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

    private const string Reason = "Fotos escuras; envie fotos com boa iluminação";
    private const string Provisional = "Provis0ria!";
    private const string Final = "Senh@Final#2026";

    /// <summary>Cria uma conta (Redator ou Administrador) pela tela de usuários, entra com ela em outro contexto do navegador e passa pela troca da senha provisória.</summary>
    private async Task<IPage> NewAccountPageAsync(string role)
    {
        string unique = Guid.NewGuid().ToString("N")[..8];
        string email = $"{role.ToLowerInvariant()}-{unique}@exemplo.com.br";
        await Page.GotoAsync(Url("/painel/usuarios/novo")).ConfigureAwait(false);
        await Page.GetByLabel("Nome").FillAsync($"{role} {unique}").ConfigureAwait(false);
        await Page.GetByLabel("E-mail").FillAsync(email).ConfigureAwait(false);
        await Page.GetByLabel(role, new() { Exact = true }).CheckAsync().ConfigureAwait(false);
        await Page.GetByLabel("Senha provisória").FillAsync(Provisional).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("criado").ConfigureAwait(false);

        IBrowserContext separate = await Browser.NewContextAsync(ContextOptions()).ConfigureAwait(false);
        IPage page = await separate.NewPageAsync().ConfigureAwait(false);
        await SignInAsync(page, email, Provisional).ConfigureAwait(false);
        await page.Locator("#NewPassword").FillAsync(Final).ConfigureAwait(false);
        await page.Locator("#ConfirmPassword").FillAsync(Final).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar senha" }).ClickAsync().ConfigureAwait(false);
        await page.WaitForURLAsync(new Regex(@"/painel/anuncios")).ConfigureAwait(false);
        return page;
    }

    /// <summary>Cria um anúncio de "Livros e revistas" com uma foto e o envia para revisão pela tela, como o Redator faria.</summary>
    private async Task SubmitAdAsync(IPage page, string title)
    {
        await page.GotoAsync(Url("/painel/anuncios/novo")).ConfigureAwait(false);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        await page.GetByLabel("Título").FillAsync(title).ConfigureAwait(false);
        await page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = "Livros e revistas" }).ConfigureAwait(false);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        await page.GetByLabel("Descrição").FillAsync("Edição 2020, sem anotações").ConfigureAwait(false);
        await page.GetByLabel("Condição").SelectOptionAsync(new SelectOptionValue { Index = 1 }).ConfigureAwait(false);
        await page.GetByLabel("Preço").FillAsync("5000").ConfigureAwait(false);
        await page.GetByLabel("CEP").FillAsync("13015-100").ConfigureAwait(false);
        await Expect(page.GetByLabel("Cidade (automático)")).ToHaveValueAsync("Campinas").ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Rascunho salvo" })).ToBeVisibleAsync().ConfigureAwait(false);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        await page.WaitForFunctionAsync("() => document.getElementById('arquivo-foto')?.multiple === true").ConfigureAwait(false);
        await page.GetByLabel("Escolher fotos").SetInputFilesAsync(Fixture("foto-1.jpg")).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar fotos" }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Fotos (1 de" })).ToBeVisibleAsync().ConfigureAwait(false);
        await SendForReviewAsync(page).ConfigureAwait(false);
    }

    /// <summary>"Enviar para revisão" e, na página de confirmação, "Enviar para revisão" de novo.</summary>
    private async Task SendForReviewAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByText("Anúncio enviado para revisão")).ToBeVisibleAsync().ConfigureAwait(false);
    }

    private static ILocator Row(IPage page, string title) => page.GetByRole(AriaRole.Row).Filter(new() { HasText = title });

    private static async Task ListFilteredAsync(IPage page, string title, string situation = null)
    {
        string query = "?q=" + Uri.EscapeDataString(title) + (situation is null ? string.Empty : "&situacao=" + situation);
        await page.GotoAsync(Url("/painel/anuncios" + query)).ConfigureAwait(false);
    }

    /// <summary>Da fila à pré-visualização do anúncio; devolve o id e o endereço da foto de destaque.</summary>
    private async Task<(int AdId, string Photo)> OpenPreviewAsync(IPage page, string title)
    {
        await page.GotoAsync(Url("/painel/anuncios/fila")).ConfigureAwait(false);
        await Row(page, title).GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync().ConfigureAwait(false);
        await Expect(page).ToHaveURLAsync(new Regex(@"/painel/anuncios/\d+/pre-visualizacao$")).ConfigureAwait(false);
        int id = int.Parse(Regex.Match(page.Url, @"/anuncios/(\d+)/").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        string photo = (await page.Locator("[data-ad-photos] img").First.GetAttributeAsync("src").ConfigureAwait(false))!;
        return (id, photo);
    }

    private async Task PublishFromPreviewAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Link, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await Expect(page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio publicado" })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    private async Task<IAPIRequestContext> VisitorAsync()
    {
        IBrowserContext visitor = await Browser.NewContextAsync(ContextOptions()).ConfigureAwait(false);
        return visitor.APIRequest;
    }

    private static async Task AssertPhotoAsync(IAPIRequestContext visitor, string photo, int expected, string why) =>
        Assert.AreEqual(expected, (await visitor.GetAsync(Url(photo)).ConfigureAwait(false)).Status, why);

    [TestMethod]
    public async Task CicloDeVida_RedatorEAdministrador_EnviarRejeitarCorrigirPublicarDespublicarReenviarPublicarArquivar()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        IPage writer = await NewAccountPageAsync("Redator").ConfigureAwait(false);
        IAPIRequestContext visitor = await VisitorAsync().ConfigureAwait(false);
        string title = Unique("Livro do checkpoint");

        // O Redator cria e envia; vê o próprio anúncio "Em revisão" em "Meus anúncios", sem a coluna do autor
        await SubmitAdAsync(writer, title).ConfigureAwait(false);
        await Expect(writer.GetByRole(AriaRole.Heading, new() { Name = "Meus anúncios", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Row(writer, title)).ToContainTextAsync("Em revisão").ConfigureAwait(false);
        await Expect(writer.GetByRole(AriaRole.Columnheader, new() { Name = "Autor" })).ToHaveCountAsync(0).ConfigureAwait(false);

        // O Administrador acha na fila e rejeita com motivo; a foto não é pública
        (int adId, string photo) = await OpenPreviewAsync(Page, title).ConfigureAwait(false);
        await AssertPhotoAsync(visitor, photo, 404, "em revisão: sem foto pública").ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Rejeitar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByLabel("Motivo da rejeição").FillAsync(Reason).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Rejeitar anúncio" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio rejeitado" })).ToBeVisibleAsync().ConfigureAwait(false);

        // O Redator vê "Rejeitado" com o motivo na lista e na edição, corrige e reenvia (@US-009-S04 de ponta a ponta)
        await writer.GotoAsync(Url("/painel/anuncios")).ConfigureAwait(false);
        await Expect(Row(writer, title)).ToContainTextAsync("Rejeitado").ConfigureAwait(false);
        await Expect(Row(writer, title)).ToContainTextAsync("Motivo da rejeição: " + Reason).ConfigureAwait(false);
        await Row(writer, title).GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync().ConfigureAwait(false);
        await Expect(writer).ToHaveURLAsync(new Regex($@"/painel/anuncios/{adId}/editar$")).ConfigureAwait(false);
        await Expect(writer.GetByText("Este anúncio foi rejeitado.")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(writer.GetByText("Motivo: " + Reason)).ToBeVisibleAsync().ConfigureAwait(false);
        await writer.GetByLabel("Descrição").FillAsync("Edição 2020, sem anotações; fotos refeitas").ConfigureAwait(false);
        await SendForReviewAsync(writer).ConfigureAwait(false);
        await Expect(Row(writer, title)).ToContainTextAsync("Em revisão").ConfigureAwait(false);
        await Expect(Row(writer, title)).Not.ToContainTextAsync("Motivo da rejeição").ConfigureAwait(false);

        // O Administrador publica; a foto passa a ser pública; o Redator vê "Publicado" e não tem ações de retirada
        await OpenPreviewAsync(Page, title).ConfigureAwait(false);
        await PublishFromPreviewAsync(Page).ConfigureAwait(false);
        await AssertPhotoAsync(visitor, photo, 200, "publicado: a foto é pública").ConfigureAwait(false);
        await writer.GotoAsync(Url("/painel/anuncios")).ConfigureAwait(false);
        await Expect(Row(writer, title)).ToContainTextAsync("Publicado").ConfigureAwait(false);
        await Row(writer, title).GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync().ConfigureAwait(false);
        await Expect(writer.GetByText("Este anúncio não pode ser editado")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(writer.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^(Despublicar|Arquivar)$") })).ToHaveCountAsync(0).ConfigureAwait(false);

        // O Administrador despublica; a foto sai do ar; o Redator vê Rascunho, edita e reenvia; o Administrador publica de novo
        await Page.GotoAsync(Url($"/painel/anuncios/{adId}/editar")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Despublicar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Despublicar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio despublicado" })).ToBeVisibleAsync().ConfigureAwait(false);
        await AssertPhotoAsync(visitor, photo, 404, "despublicado: a foto sai do ar").ConfigureAwait(false);
        await writer.GotoAsync(Url("/painel/anuncios")).ConfigureAwait(false);
        await Expect(Row(writer, title)).ToContainTextAsync("Rascunho").ConfigureAwait(false);
        await Row(writer, title).GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync().ConfigureAwait(false);
        await Expect(writer.GetByRole(AriaRole.Heading, new() { Name = "Editar anúncio", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await SendForReviewAsync(writer).ConfigureAwait(false);
        await OpenPreviewAsync(Page, title).ConfigureAwait(false);
        await PublishFromPreviewAsync(Page).ConfigureAwait(false);
        await AssertPhotoAsync(visitor, photo, 200, "publicado de novo: a foto volta").ConfigureAwait(false);

        // O Administrador arquiva; a foto sai do ar; o arquivado some da lista padrão do Redator e aparece ao filtrar, só para leitura
        await Page.GotoAsync(Url($"/painel/anuncios/{adId}/editar")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio arquivado" })).ToBeVisibleAsync().ConfigureAwait(false);
        await AssertPhotoAsync(visitor, photo, 404, "arquivado: a foto sai do ar").ConfigureAwait(false);
        await ListFilteredAsync(writer, title).ConfigureAwait(false);
        await Expect(writer.GetByText("Nenhum anúncio encontrado")).ToBeVisibleAsync().ConfigureAwait(false);
        await ListFilteredAsync(writer, title, "arquivado").ConfigureAwait(false);
        await Expect(Row(writer, title)).ToContainTextAsync("Arquivado").ConfigureAwait(false);
        await Row(writer, title).GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync().ConfigureAwait(false);
        await Expect(writer.GetByText("Este anúncio não pode ser editado")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(writer.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^(Despublicar|Arquivar)$") })).ToHaveCountAsync(0).ConfigureAwait(false);

        // O Administrador acha o arquivado, com o nome do autor, ao filtrar por "Arquivado"
        await ListFilteredAsync(Page, title, "arquivado").ConfigureAwait(false);
        await Expect(Row(Page, title)).ToContainTextAsync("Arquivado").ConfigureAwait(false);
        await Expect(Row(Page, title)).ToContainTextAsync("Redator ").ConfigureAwait(false);

        // O Redator não abre a fila de revisão
        await writer.GotoAsync(Url("/painel/anuncios/fila")).ConfigureAwait(false);
        await Expect(writer).ToHaveURLAsync(new Regex(@"/painel/acesso-negado")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DoisAdministradores_QuemConfirmaDepoisLePorOutroAdministrador_ENadaFicaGravadoDuasVezes()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        IPage writer = await NewAccountPageAsync("Redator").ConfigureAwait(false);
        IPage secondAdmin = await NewAccountPageAsync("Administrador").ConfigureAwait(false);
        string title = Unique("Livro disputado");
        await SubmitAdAsync(writer, title).ConfigureAwait(false);
        (int adId, string photo) = await OpenPreviewAsync(Page, title).ConfigureAwait(false);

        // Os dois abrem a confirmação de publicar enquanto o anúncio ainda está em revisão
        await Page.GetByRole(AriaRole.Link, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await secondAdmin.GotoAsync(Url($"/painel/anuncios/{adId}/publicar")).ConfigureAwait(false);
        await Expect(secondAdmin.GetByRole(AriaRole.Heading, new() { Name = "Publicar este anúncio?" })).ToBeVisibleAsync().ConfigureAwait(false);

        // O primeiro confirma; o segundo confirma depois e lê a mensagem da SPEC na pré-visualização
        await Page.GetByRole(AriaRole.Button, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio publicado" })).ToBeVisibleAsync().ConfigureAwait(false);
        await secondAdmin.GetByRole(AriaRole.Button, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await Expect(secondAdmin).ToHaveURLAsync(new Regex(@"/painel/anuncios/\d+/pre-visualizacao$")).ConfigureAwait(false);
        await Expect(secondAdmin.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Este anúncio já foi publicado por outro administrador" })).ToBeVisibleAsync().ConfigureAwait(false);
        await AssertPhotoAsync(await VisitorAsync().ConfigureAwait(false), photo, 200, "continua publicado").ConfigureAwait(false);

        // Quem publicou, se confirmar de novo, lê "já foi publicado" sem o "por outro administrador"
        await Page.GotoAsync(Url($"/painel/anuncios/{adId}/pre-visualizacao")).ConfigureAwait(false);
        await Expect(Page.GetByText("Este anúncio não está em revisão. Situação:")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Page.GetByText("Publicado", new() { Exact = true })).ToBeVisibleAsync().ConfigureAwait(false);
    }
}
