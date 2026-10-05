using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace GazetaMarketplace.Web.Tests.Playwright;

/// <summary>
/// O caminho completo "cadastrar, enviar para revisão e publicar" pelas telas do painel, para os E2E que precisam de um anúncio publicado no site (vitrine, detalhe). Os E2E de revisão e de
/// ciclo de vida têm cada um a sua cópia, mais antiga (BACKLOG: extrair); as novas usam esta.
/// </summary>
internal static class PublishingFlow
{
    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Photos", "Fixtures", name);

    public static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";

    public static async Task SignInAdminAsync(IPage page)
    {
        await page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await page.GetByLabel("E-mail").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL")).ConfigureAwait(false);
        await page.GetByLabel("Senha").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
        await page.WaitForURLAsync(new Regex(@"/painel/(?!entrar)")).ConfigureAwait(false);
    }

    /// <summary>Cadastra um anúncio de <paramref name="category"/> (uma categoria de Produtos em geral) com <paramref name="photos"/> fotos, envia para revisão e publica; devolve o título. A conta já deve estar entrada.</summary>
    public static async Task<string> PublishAsync(IPage page, string prefix, int photos, string category = "Livros e revistas")
    {
        string title = Unique(prefix);
        await page.GotoAsync(Url("/painel/anuncios/novo")).ConfigureAwait(false);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        await page.GetByLabel("Título").FillAsync(title).ConfigureAwait(false);
        await page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = category }).ConfigureAwait(false);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false); // a troca de categoria busca os campos do grupo; o que se digita antes de a resposta chegar se perderia
        await page.GetByLabel("Descrição").FillAsync("Edição 2020, sem anotações").ConfigureAwait(false);
        await page.GetByLabel("Condição").SelectOptionAsync(new SelectOptionValue { Index = 1 }).ConfigureAwait(false);
        await page.GetByLabel("Preço").FillAsync("5000").ConfigureAwait(false);
        await page.GetByLabel("CEP").FillAsync("13015-100").ConfigureAwait(false);
        await Microsoft.Playwright.Assertions.Expect(page.GetByLabel("Cidade (automático)")).ToHaveValueAsync("Campinas").ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar rascunho" }).ClickAsync().ConfigureAwait(false);
        await Microsoft.Playwright.Assertions.Expect(page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Rascunho salvo" })).ToBeVisibleAsync().ConfigureAwait(false);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        await page.WaitForFunctionAsync("() => document.getElementById('arquivo-foto')?.multiple === true").ConfigureAwait(false);
        FilePayload[] files = [.. Enumerable.Range(1, photos).Select(i => new FilePayload { Name = $"foto-{i}.jpg", MimeType = "image/jpeg", Buffer = File.ReadAllBytes(Fixture($"foto-{(i - 1) % 3 + 1}.jpg")) })];
        await page.GetByLabel("Escolher fotos").SetInputFilesAsync(files).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar fotos" }).ClickAsync().ConfigureAwait(false);
        await Microsoft.Playwright.Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = $"Fotos ({photos} de" })).ToBeVisibleAsync(new() { Timeout = 120_000 }).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Enviar para revisão" }).ClickAsync().ConfigureAwait(false);
        await Microsoft.Playwright.Assertions.Expect(page.GetByText("Anúncio enviado para revisão")).ToBeVisibleAsync().ConfigureAwait(false);

        await page.GotoAsync(Url("/painel/anuncios/fila")).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Row).Filter(new() { HasText = title }).GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync().ConfigureAwait(false);
        await page.GetByRole(AriaRole.Link, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await Microsoft.Playwright.Assertions.Expect(page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio publicado" })).ToBeVisibleAsync().ConfigureAwait(false);
        return title;
    }
}
