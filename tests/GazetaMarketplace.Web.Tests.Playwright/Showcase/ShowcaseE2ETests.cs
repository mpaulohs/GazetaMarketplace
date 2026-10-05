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

namespace GazetaMarketplace.Web.Tests.Playwright.Showcase;

/// <summary>
/// A página inicial e as páginas de categoria (US-001) no navegador, no site publicado. A conta do E2E (Administrador) cadastra e publica um livro pela tela; o visitante
/// (outro contexto, sem login) o encontra pela página inicial, entra na categoria e na subcategoria, volta pelo caminho de navegação (S03), usa o site em 320 px sem rolagem
/// horizontal (S08), sem JavaScript e com o axe sem violações. Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class ShowcaseE2ETests : SitePage
#pragma warning restore CA1515
{
    private const string Root = "Música e hobbies";
    private const string Subcategory = "Livros e revistas";

    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static string Unique(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";

    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Photos", "Fixtures", name);

    private static string Describe(AxeResultItem violation) =>
        violation.Id + ": " + violation.Help + " [" + string.Join(" | ", violation.Nodes.Take(3).Select(n => n.Html + " => " + string.Join("; ", n.Any.Select(a => a.Message)))) + "]";

    private async Task SignInAdminAsync()
    {
        await Page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await Page.GetByLabel("E-mail").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL")).ConfigureAwait(false);
        await Page.GetByLabel("Senha").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
        await Page.WaitForURLAsync(new Regex(@"/painel/(?!entrar)")).ConfigureAwait(false);
    }

    /// <summary>Cadastra um livro com uma foto, envia para revisão e publica, tudo pelas telas; devolve o título.</summary>
    private async Task<string> PublishBookAsync()
    {
        string title = Unique("Livro da vitrine");
        await Page.GotoAsync(Url("/painel/anuncios/novo")).ConfigureAwait(false);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        await Page.GetByLabel("Título").FillAsync(title).ConfigureAwait(false);
        await Page.GetByLabel("Categoria").SelectOptionAsync(new SelectOptionValue { Label = Subcategory }).ConfigureAwait(false);
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

        await Page.GotoAsync(Url("/painel/anuncios/fila")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Row).Filter(new() { HasText = title }).GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Publicar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio publicado" })).ToBeVisibleAsync().ConfigureAwait(false);
        return title;
    }

    private async Task<IPage> VisitorAsync(int? width = null, bool javaScript = true)
    {
        IBrowserContext context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true, JavaScriptEnabled = javaScript }).ConfigureAwait(false);
        IPage visitor = await context.NewPageAsync().ConfigureAwait(false);
        if (width is { } w)
        {
            await visitor.SetViewportSizeAsync(w, 800).ConfigureAwait(false);
        }

        return visitor;
    }

    [TestMethod]
    public async Task US001S03_DaPaginaInicialAteASubcategoria_EVoltarPeloCaminhoDeNavegacao()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        string title = await PublishBookAsync().ConfigureAwait(false);
        IPage visitor = await VisitorAsync().ConfigureAwait(false);

        await visitor.GotoAsync(Url("/")).ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = "Anúncios mais recentes" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^" + Regex.Escape(title)) })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Searchbox, new() { Name = "Buscar anúncios" })).ToBeVisibleAsync().ConfigureAwait(false);

        await visitor.GetByRole(AriaRole.Navigation, new() { Name = "Categorias principais" }).GetByRole(AriaRole.Link, new() { Name = Root }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"/categoria/[a-z0-9-]+$")).ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = Root, Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^" + Regex.Escape(title)) })).ToBeVisibleAsync().ConfigureAwait(false);
        string rootUrl = visitor.Url;

        await visitor.GetByRole(AriaRole.Navigation, new() { Name = "Subcategorias" }).GetByRole(AriaRole.Link, new() { Name = Subcategory }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = Subcategory, Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        IReadOnlyList<string> steps = await visitor.GetByRole(AriaRole.Navigation, new() { Name = "Caminho de navegação" }).GetByRole(AriaRole.Listitem).AllInnerTextsAsync().ConfigureAwait(false);
        CollectionAssert.AreEqual(new[] { "Início", Root, Subcategory }, steps.Select(s => s.Trim()).ToArray(), "o caminho: Início > categoria > subcategoria");
        await Expect(visitor.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^" + Regex.Escape(title)) })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Navigation, new() { Name = "Subcategorias" })).ToHaveCountAsync(0).ConfigureAwait(false);

        await visitor.GetByRole(AriaRole.Navigation, new() { Name = "Caminho de navegação" }).GetByRole(AriaRole.Link, new() { Name = Root }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(rootUrl).ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Navigation, new() { Name = "Subcategorias" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^" + Regex.Escape(title)) })).ToBeVisibleAsync().ConfigureAwait(false);

        await visitor.GetByRole(AriaRole.Navigation, new() { Name = "Caminho de navegação" }).GetByRole(AriaRole.Link, new() { Name = "Início" }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(Url("/")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US001S03_SemJavaScript_OsMesmosLinksLevamAsMesmasPaginas()
    {
        IPage visitor = await VisitorAsync(javaScript: false).ConfigureAwait(false);

        await visitor.GotoAsync(Url("/")).ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Link, new() { Name = Root }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = Root, Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Link, new() { Name = Subcategory }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = Subcategory, Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Link, new() { Name = "Início" }).First.ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = "Categorias", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US001S07_EnderecoDeCategoriaQueNaoExiste_MostraAMensagemEOsCaminhosDeVolta()
    {
        IPage visitor = await VisitorAsync().ConfigureAwait(false);

        IResponse response = (await visitor.GotoAsync(Url("/categoria/barcos-e-aeronaves-antigo")).ConfigureAwait(false))!;

        Assert.AreEqual(404, response.Status);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = "Categoria não encontrada", Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Link, new() { Name = "Ir para a página inicial" })).ToBeVisibleAsync().ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Navigation, new() { Name = "Categorias principais" }).GetByRole(AriaRole.Link, new() { Name = Root }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = Root, Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US001S08_PaginaInicialEmTelaDe320px_SemRolagemHorizontal_BuscaCategoriasEListaSoRolandoParaBaixo()
    {
        IPage visitor = await VisitorAsync(320).ConfigureAwait(false);

        await visitor.GotoAsync(Url("/")).ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = "Anúncios mais recentes" })).ToBeAttachedAsync().ConfigureAwait(false);

        Assert.IsFalse(await visitor.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth").ConfigureAwait(false), "a página inicial não rola na horizontal em 320 px");
        foreach (string selector in new[] { "[role=search] input", "[data-category-tile] >> nth=0", "[data-category-tile] >> nth=-1" })
        {
            await visitor.Locator(selector).ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
            LocatorBoundingBoxResult box = (await visitor.Locator(selector).BoundingBoxAsync().ConfigureAwait(false))!;
            Assert.IsTrue(box.X >= 0 && box.X + box.Width <= 320.5, $"{selector} cabe na largura de 320 px (x={box.X}, largura={box.Width})");
        }

        int cards = await visitor.Locator("[data-ad-card]").CountAsync().ConfigureAwait(false);
        if (cards > 0)
        {
            LocatorBoundingBoxResult card = (await visitor.Locator("[data-ad-card]").First.BoundingBoxAsync().ConfigureAwait(false))!;
            Assert.IsTrue(card.X >= 0 && card.X + card.Width <= 320.5, "o card cabe na largura de 320 px");
        }

        await visitor.GetByRole(AriaRole.Link, new() { Name = Root }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = Root, Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        Assert.IsFalse(await visitor.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth").ConfigureAwait(false), "a categoria não rola na horizontal em 320 px");
    }

    [TestMethod]
    public async Task Acessibilidade_PaginaInicialECategoria_SemViolacoes_EmDesktopECelular()
    {
        using FakeViaCep viaCep = new();
        await SignInAdminAsync().ConfigureAwait(false);
        await PublishBookAsync().ConfigureAwait(false);
        AxeRunOptions options = new() { RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] } };

        foreach (int width in new[] { 1280, 320 })
        {
            IPage visitor = await VisitorAsync(width).ConfigureAwait(false);
            foreach ((string path, string heading) in new[] { ("/", "Anúncios mais recentes"), (null, Root), (null, Subcategory), ("/categoria/nao-existe", "Categoria não encontrada") })
            {
                if (path is not null)
                {
                    await visitor.GotoAsync(Url(path)).ConfigureAwait(false);
                }
                else if (heading == Root)
                {
                    await visitor.GotoAsync(Url("/")).ConfigureAwait(false);
                    await visitor.GetByRole(AriaRole.Navigation, new() { Name = "Categorias principais" }).GetByRole(AriaRole.Link, new() { Name = Root }).ClickAsync().ConfigureAwait(false);
                }
                else
                {
                    await visitor.GetByRole(AriaRole.Navigation, new() { Name = "Subcategorias" }).GetByRole(AriaRole.Link, new() { Name = Subcategory }).ClickAsync().ConfigureAwait(false);
                }

                await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = heading })).ToBeVisibleAsync().ConfigureAwait(false);
                await visitor.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
                AxeResult result = await visitor.RunAxe(options).ConfigureAwait(false);
                Assert.AreEqual(0, result.Violations.Length, $"{heading} em {width}px: " + string.Join("; ", result.Violations.Select(Describe)));
            }
        }
    }
}
