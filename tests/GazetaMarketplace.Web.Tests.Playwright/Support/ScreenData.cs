using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace GazetaMarketplace.Web.Tests.Playwright.Support;

/// <summary>
/// Os dados de que as telas precisam (um anúncio publicado com fotos, um rascunho completo, um anúncio em revisão, uma categoria vazia, um Redator) e o jeito de abrir cada tela. Os dados públicos são
/// criados de verdade pelas telas do painel (o axe mede o contraste do que o visitante vê); os do painel reaproveitam o que os outros testes já deixaram, e só o que falta é criado. Preparado uma vez por rodada.
/// </summary>
internal static class ScreenData
{
    internal const string EmptyCategoryName = "Telas sem anúncios";
    internal const string ProtectedCategoryName = "Livros e revistas";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static Task<Dictionary<string, string>> _build;

    public static string MainUrl => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/');

    public static string DevUrl => Environment.GetEnvironmentVariable("GAZETA_DEV_BASE_URL")?.TrimEnd('/');

    public static string BaseOf(Screen screen) => screen.IsDevelopmentSite ? DevUrl : MainUrl;

    /// <summary>As fichas que a lista de telas pode usar, na ordem em que são preenchidas.</summary>
    public static readonly string[] Tokens =
        ["adPath", "categorySlug", "emptyCategorySlug", "searchToken", "draftId", "photoId", "inReviewId", "publishedId", "emptyCategoryId", "protectedCategoryId", "writerId"];

    /// <summary>Prepara os dados na primeira chamada (entrando como Administrador na <paramref name="page"/>) e devolve as fichas.</summary>
    public static async Task<IReadOnlyDictionary<string, string>> EnsureAsync(IPage page)
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            // Guarda a própria tarefa: se o preparo falhar, os demais casos falham na hora com o mesmo erro, em vez de repetir o preparo (minutos) um por um
            _build ??= BuildAsync(page);
            return await _build.ConfigureAwait(false);
        }
        finally
        {
            Gate.Release();
        }
    }

    public static string Resolve(Screen screen, IReadOnlyDictionary<string, string> values) =>
        Regex.Replace(screen.Path, @"\{(\w+)\}", m => values[m.Groups[1].Value]);

    public static async Task SignInAdminAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync(baseUrl + "/painel/entrar").ConfigureAwait(false);
        await page.GetByLabel("E-mail").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL")).ConfigureAwait(false);
        await page.GetByLabel("Senha").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
        await page.WaitForURLAsync(new Regex(@"/painel/(?!entrar)")).ConfigureAwait(false);
    }

    /// <summary>Abre a tela (e entra antes, se for do painel), confere o status esperado e faz o passo de preparo (abrir o painel de filtros, a galeria...).</summary>
    public static async Task OpenAsync(IPage page, Screen screen, IReadOnlyDictionary<string, string> values)
    {
        string url = BaseOf(screen) + Resolve(screen, values);
        if (screen.Prepare == "seed-favorites")
        {
            await page.Context.AddInitScriptAsync($"window.localStorage.setItem('gazeta:favoritos:v1', JSON.stringify([{values["publishedId"]}]));").ConfigureAwait(false);
        }

        IResponse response = await page.GotoAsync(url).ConfigureAwait(false);
        if (response is null || response.Status != screen.Status)
        {
            throw new InvalidOperationException($"{screen.Id}: {url} respondeu {response?.Status} (esperado {screen.Status})");
        }

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        switch (screen.Prepare)
        {
            case "open-filters":
                ILocator toggle = page.Locator("[data-filters-toggle]");
                if (await toggle.IsVisibleAsync().ConfigureAwait(false) && await toggle.GetAttributeAsync("aria-expanded").ConfigureAwait(false) != "true")
                {
                    await toggle.ClickAsync().ConfigureAwait(false);
                }

                await Microsoft.Playwright.Assertions.Expect(page.Locator("#filtros")).ToBeVisibleAsync().ConfigureAwait(false);
                await page.WaitForTimeoutAsync(400).ConfigureAwait(false); // a animação do painel recolhível
                break;
            case "open-gallery":
                await page.Locator("[data-gallery-open]").First.ClickAsync().ConfigureAwait(false);
                await Microsoft.Playwright.Assertions.Expect(page.Locator("dialog[open]")).ToBeVisibleAsync().ConfigureAwait(false);
                break;
            case "seed-favorites":
                await Microsoft.Playwright.Assertions.Expect(page.Locator("[data-ad-card]").First).ToBeVisibleAsync().ConfigureAwait(false);
                break;
        }
    }

    private static async Task<Dictionary<string, string>> BuildAsync(IPage page)
    {
        using FakeViaCep viaCep = new();
        Dictionary<string, string> values = [];
        await SignInAdminAsync(page, MainUrl).ConfigureAwait(false);

        // O contato do anúncio só aparece com o telefone do site configurado
        await page.GotoAsync(MainUrl + "/painel/configuracoes").ConfigureAwait(false);
        await page.GetByLabel(new Regex("^Telefone/WhatsApp do site")).FillAsync("11912345678").ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
        await Microsoft.Playwright.Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("Configurações salvas").ConfigureAwait(false);

        // Um anúncio publicado com duas fotos: início, categoria, busca, detalhe, galeria e favoritos
        string title = await PublishingFlow.PublishAsync(page, "Telas livro", 2).ConfigureAwait(false);
        values["searchToken"] = title.Split(' ')[^1];
        values["adPath"] = await PublishingFlow.PublicPathAsync(page.Context.Browser!, title).ConfigureAwait(false);
        values["publishedId"] = Regex.Match(values["adPath"], @"/anuncio/(\d+)").Groups[1].Value;
        await page.GotoAsync(MainUrl + values["adPath"]).ConfigureAwait(false);
        string categoryHref = (await page.Locator("nav[data-breadcrumb] a[href^='/categoria/']").Last.GetAttributeAsync("href").ConfigureAwait(false))!;
        values["categorySlug"] = categoryHref["/categoria/".Length..];

        // Um rascunho completo (com foto) e um anúncio em revisão
        await PublishingFlow.DraftAsync(page, "Telas rascunho").ConfigureAwait(false);
        values["draftId"] = Regex.Match(page.Url, @"/painel/anuncios/(\d+)/editar").Groups[1].Value;
        await page.ReloadAsync().ConfigureAwait(false); // depois do envio a galeria é montada pelo JavaScript, sem o formulário de remover; recarregar traz a página do servidor
        string removeHref = (await page.Locator("form[action$='/remover']").First.GetAttributeAsync("action").ConfigureAwait(false))!;
        values["photoId"] = Regex.Match(removeHref, @"/fotos/(\d+)/remover").Groups[1].Value;
        string review = await PublishingFlow.SubmitAsync(page, "Telas em revisão").ConfigureAwait(false);
        await page.GotoAsync(MainUrl + "/painel/anuncios/fila").ConfigureAwait(false);
        string previewHref = (await page.GetByRole(AriaRole.Row).Filter(new() { HasText = review }).GetByRole(AriaRole.Link, new() { Name = review }).GetAttributeAsync("href").ConfigureAwait(false))!;
        values["inReviewId"] = Regex.Match(previewHref, @"/painel/anuncios/(\d+)/").Groups[1].Value;

        // Categorias: uma sem anúncios (criada uma vez só, reaproveitada nas rodadas seguintes) e uma protegida (com anúncios)
        await page.GotoAsync(MainUrl + "/painel/categorias").ConfigureAwait(false);
        if (await page.GetByRole(AriaRole.Link, new() { Name = "Editar " + EmptyCategoryName, Exact = true }).CountAsync().ConfigureAwait(false) == 0)
        {
            await page.GetByRole(AriaRole.Link, new() { Name = "Nova categoria" }).ClickAsync().ConfigureAwait(false);
            await page.GetByLabel(new Regex("^Nome")).FillAsync(EmptyCategoryName).ConfigureAwait(false);
            await page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
            await page.WaitForURLAsync(new Regex(@"/painel/categorias$")).ConfigureAwait(false);
        }

        values["emptyCategoryId"] = await IdOfAsync(page, "Editar " + EmptyCategoryName, @"/categorias/(\d+)/editar").ConfigureAwait(false);
        values["protectedCategoryId"] = await IdOfAsync(page, "Editar " + ProtectedCategoryName, @"/categorias/(\d+)/editar").ConfigureAwait(false);
        await page.GotoAsync(MainUrl + "/").ConfigureAwait(false);
        string emptyHref = (await page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex(Regex.Escape(EmptyCategoryName)) }).First.GetAttributeAsync("href").ConfigureAwait(false))!;
        values["emptyCategorySlug"] = emptyHref["/categoria/".Length..];

        // Um Redator: o primeiro que já exista, ou um novo
        await page.GotoAsync(MainUrl + "/painel/usuarios").ConfigureAwait(false);
        ILocator deactivate = page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^Desativar ") });
        if (await deactivate.CountAsync().ConfigureAwait(false) == 0)
        {
            string unique = Guid.NewGuid().ToString("N")[..8];
            await page.GotoAsync(MainUrl + "/painel/usuarios/novo").ConfigureAwait(false);
            await page.GetByLabel("Nome").FillAsync("Telas " + unique).ConfigureAwait(false);
            await page.GetByLabel("E-mail").FillAsync($"telas-{unique}@exemplo.com.br").ConfigureAwait(false);
            await page.GetByLabel("Redator").CheckAsync().ConfigureAwait(false);
            await page.GetByLabel("Senha provisória").FillAsync("Provis0ria@telas").ConfigureAwait(false);
            await page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
            await Microsoft.Playwright.Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("criado").ConfigureAwait(false);
        }

        string deactivateHref = (await deactivate.First.GetAttributeAsync("href").ConfigureAwait(false))!;
        values["writerId"] = Regex.Match(deactivateHref, @"/usuarios/(\d+)/desativar").Groups[1].Value;

        string[] missing = [.. Tokens.Where(t => !values.ContainsKey(t))];
        if (missing.Length > 0)
        {
            throw new InvalidOperationException("fichas sem valor: " + string.Join(", ", missing));
        }

        return values;
    }

    private static async Task<string> IdOfAsync(IPage page, string linkName, string pattern)
    {
        string href = (await page.GetByRole(AriaRole.Link, new() { Name = linkName, Exact = true }).First.GetAttributeAsync("href").ConfigureAwait(false))!;
        return Regex.Match(href, pattern).Groups[1].Value;
    }
}
