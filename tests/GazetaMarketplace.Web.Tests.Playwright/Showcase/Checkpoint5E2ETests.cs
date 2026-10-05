using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Showcase;

/// <summary>
/// Checkpoint 5 no navegador, no site publicado, com um visitante <b>sem login</b>: a jornada início → anúncio → contato → favoritar → "Meus favoritos" → busca sem nenhuma volta à entrada do painel e sem
/// chamada do site com erro; e o Serviço (tipo, sem preço) e a Vaga (salário, sem foto) em cards, detalhe e busca. Depois o Administrador arquiva o Serviço e despublica a Vaga pelo painel e os dois saem
/// da página inicial, da busca, dos favoritos e do mapa. Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD e GAZETA_E2E_VIACEP_PORT.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class Checkpoint5E2ETests : SitePage
#pragma warning restore CA1515
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private static string BaseUrl => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/');

    private static string Url(string path) => BaseUrl + path;

    private async Task<IPage> VisitorAsync(List<string> failures)
    {
        IBrowserContext context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true }).ConfigureAwait(false);
        IPage page = await context.NewPageAsync().ConfigureAwait(false);
        page.Response += (_, response) =>
        {
            // Rede de segurança: nenhum pedido ao próprio site pode voltar com erro (404 só quando o teste pede um endereço que não existe)
            if (response.Url.StartsWith(BaseUrl, StringComparison.Ordinal) && response.Status >= 400)
            {
                failures.Add($"{response.Status} {response.Url}");
            }
        };
        return page;
    }

    private async Task<string[]> SitemapLocsAsync()
    {
        IAPIResponse response = await Context.APIRequest.GetAsync(Url("/sitemap.xml")).ConfigureAwait(false);
        return [.. XDocument.Parse(await response.TextAsync().ConfigureAwait(false)).Root!.Elements(Ns + "url").Select(u => u.Element(Ns + "loc")!.Value)];
    }

    private static ILocator CardLink(IPage page, string title) => page.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^" + Regex.Escape(title)) });

    private static ILocator CardOf(IPage page, string title) => page.Locator("[data-ad-card]").Filter(new() { Has = CardLink(page, title) });

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

    // Um Livro (R$ 1.000), um Serviço e uma Vaga (R$ 2.800), todos com o mesmo texto único no título
    private async Task<(string Token, string Book, string Service, string Job)> PublishThreeAsync()
    {
        using FakeViaCep viaCep = new();
        await EnsureAdminAsync().ConfigureAwait(false);
        string token = "cp" + Guid.NewGuid().ToString("N")[..6];
        string book = await PublishingFlow.PublishAsync(Page, token + " livro", 2, price: "100000").ConfigureAwait(false);
        string service = await PublishingFlow.PublishServiceAsync(Page, token + " diarista", 1).ConfigureAwait(false);
        string job = await PublishingFlow.PublishJobAsync(Page, token + " pizzaiolo").ConfigureAwait(false);
        return (token, book, service, job);
    }

    private async Task<string> IdOfAsync(string title)
    {
        string path = await PublishingFlow.PublicPathAsync(Browser, title).ConfigureAwait(false);
        return Regex.Match(path, @"/anuncio/(\d+)/").Groups[1].Value;
    }

    [TestMethod]
    public async Task Jornada_SemLogin_InicioAnuncioContatoFavoritarMeusFavoritosEBusca_SemVoltarParaAEntrada_ESemErroDeRede()
    {
        (string token, string book, _, _) = await PublishThreeAsync().ConfigureAwait(false);
        await SetPhoneAsync().ConfigureAwait(false);
        List<string> failures = [];
        IPage visitor = await VisitorAsync(failures).ConfigureAwait(false);

        // Início: o anúncio recém-publicado está entre os mais recentes
        await visitor.GotoAsync(Url("/")).ConfigureAwait(false);
        await Expect(CardLink(visitor, book)).ToBeVisibleAsync().ConfigureAwait(false);

        // Detalhe, a partir do card
        await CardLink(visitor, book).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = book, Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);

        // Contato: o número e os dois botões, para quem não entrou
        await Expect(visitor.GetByRole(AriaRole.Link, new() { Name = "Ligar" })).ToHaveAttributeAsync("href", "tel:+5511912345678").ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Link, new() { Name = "Chamar no WhatsApp" })).ToBeVisibleAsync().ConfigureAwait(false);

        // Favoritar e abrir "Meus favoritos" pelo link do topo
        await visitor.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Favoritar") }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favoritos-contagem]")).ToHaveTextAsync("1").ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Link, new() { NameRegex = new Regex("^Favoritos") }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(Url("/favoritos")).ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-count]")).ToHaveTextAsync("1 anúncio").ConfigureAwait(false);
        await Expect(CardLink(visitor, book)).ToBeVisibleAsync().ConfigureAwait(false);

        // Categoria do anúncio, pelo caminho de navegação da própria página
        await CardLink(visitor, book).ClickAsync().ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Navigation, new() { Name = "Caminho de navegação" }).GetByRole(AriaRole.Link, new() { Name = "Livros e revistas" }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"/categoria/")).ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Livros e revistas").ConfigureAwait(false);

        // Busca pela caixa do topo
        await visitor.GetByRole(AriaRole.Searchbox, new() { Name = "Buscar anúncios" }).FillAsync(token).ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Button, new() { Name = "Buscar" }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("[data-search-total]")).ToHaveTextAsync("3 anúncios encontrados").ConfigureAwait(false);

        Assert.IsFalse(visitor.Url.Contains("/painel", StringComparison.Ordinal), "o visitante nunca foi levado ao painel: " + visitor.Url);
        Assert.AreEqual(0, failures.Count, "chamadas ao site com erro: " + string.Join("; ", failures));
        await visitor.Context.CloseAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ServicosEVagas_EmCardsDetalheEBusca_EDepoisDeArquivarEDespublicar_SaemDoInicioDaBuscaDosFavoritosEDoMapa()
    {
        (string token, string book, string service, string job) = await PublishThreeAsync().ConfigureAwait(false);
        string serviceId = await IdOfAsync(service).ConfigureAwait(false);
        string jobId = await IdOfAsync(job).ConfigureAwait(false);
        string serviceCardPath = await PublishingFlow.PublicPathAsync(Browser, service).ConfigureAwait(false);
        string jobPath = await PublishingFlow.PublicPathAsync(Browser, job).ConfigureAwait(false);
        List<string> failures = [];
        IPage visitor = await VisitorAsync(failures).ConfigureAwait(false);

        // Cards na busca: Serviço com tipo e sem preço (com capa); Vaga com salário e sem foto; Livro com preço
        await visitor.GotoAsync(Url($"/busca?q={token}")).ConfigureAwait(false);
        await Expect(visitor.Locator("[data-search-total]")).ToHaveTextAsync("3 anúncios encontrados").ConfigureAwait(false);
        ILocator serviceCard = CardOf(visitor, service);
        ILocator jobCard = CardOf(visitor, job);
        await Expect(serviceCard).ToContainTextAsync("Serviços domésticos").ConfigureAwait(false);
        await Expect(serviceCard).Not.ToContainTextAsync("R$").ConfigureAwait(false);
        await Expect(serviceCard.Locator("img")).ToHaveCountAsync(1).ConfigureAwait(false);
        await Expect(jobCard).ToContainTextAsync("Salário R$ 2.800").ConfigureAwait(false);
        await Expect(jobCard.Locator("img")).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(jobCard.Locator("[data-ad-placeholder]")).ToHaveCountAsync(1).ConfigureAwait(false);
        await Expect(CardOf(visitor, book)).ToContainTextAsync("R$ 1.000").ConfigureAwait(false);

        // O Serviço fica fora da faixa de preço e no fim das duas ordens por preço
        await visitor.GotoAsync(Url($"/busca?q={token}&precoMin=1")).ConfigureAwait(false);
        await Expect(visitor.Locator("[data-search-total]")).ToHaveTextAsync("2 anúncios encontrados").ConfigureAwait(false);
        await Expect(CardLink(visitor, service)).ToHaveCountAsync(0).ConfigureAwait(false);
        await visitor.GotoAsync(Url($"/busca?q={token}&ordem=menor-preco")).ConfigureAwait(false);
        string[] cheap = [.. (await visitor.Locator("[data-ad-card] a.ad-card__link").AllInnerTextsAsync().ConfigureAwait(false)).Select(t => t.Trim())];
        Assert.IsTrue(cheap[0].StartsWith(book, StringComparison.Ordinal) && cheap[1].StartsWith(job, StringComparison.Ordinal) && cheap[2].StartsWith(service, StringComparison.Ordinal), "Menor preço: " + string.Join(" | ", cheap));
        await visitor.GotoAsync(Url($"/busca?q={token}&ordem=maior-preco")).ConfigureAwait(false);
        string[] dear = [.. (await visitor.Locator("[data-ad-card] a.ad-card__link").AllInnerTextsAsync().ConfigureAwait(false)).Select(t => t.Trim())];
        Assert.IsTrue(dear[0].StartsWith(job, StringComparison.Ordinal) && dear[1].StartsWith(book, StringComparison.Ordinal) && dear[2].StartsWith(service, StringComparison.Ordinal), "Maior preço: " + string.Join(" | ", dear));

        // Detalhe do Serviço (sem preço) e da Vaga (salário, sem galeria)
        await visitor.GotoAsync(Url(serviceCardPath)).ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = service, Level = 1 })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.Locator("main")).Not.ToContainTextAsync("R$").ConfigureAwait(false);
        await Expect(visitor.Locator("main")).ToContainTextAsync("Serviços domésticos").ConfigureAwait(false);
        await visitor.GotoAsync(Url(jobPath)).ConfigureAwait(false);
        await Expect(visitor.Locator("main")).ToContainTextAsync("Salário").ConfigureAwait(false);
        await Expect(visitor.Locator("main")).ToContainTextAsync("2.800").ConfigureAwait(false);
        await Expect(visitor.Locator("main img[src^='/fotos/']")).ToHaveCountAsync(0).ConfigureAwait(false);

        // Os dois nos favoritos, no mapa e na página inicial
        await visitor.EvaluateAsync($"() => localStorage.setItem('gazeta:favoritos:v1', JSON.stringify([{serviceId}, {jobId}]))").ConfigureAwait(false);
        await visitor.GotoAsync(Url("/favoritos")).ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-count]")).ToHaveTextAsync("2 anúncios").ConfigureAwait(false);
        string[] locs = await SitemapLocsAsync().ConfigureAwait(false);
        Assert.IsTrue(locs.Any(l => l.Contains($"/anuncio/{serviceId}/", StringComparison.Ordinal)) && locs.Any(l => l.Contains($"/anuncio/{jobId}/", StringComparison.Ordinal)));
        await visitor.GotoAsync(Url("/")).ConfigureAwait(false);
        await Expect(CardLink(visitor, service)).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(CardLink(visitor, job)).ToBeVisibleAsync().ConfigureAwait(false);

        // O Administrador arquiva o Serviço e despublica a Vaga pelo painel
        await Page.GotoAsync(Url($"/painel/anuncios/{serviceId}/editar")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Arquivar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio arquivado" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GotoAsync(Url($"/painel/anuncios/{jobId}/editar")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Despublicar" }).ClickAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Despublicar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status).Filter(new() { HasText = "Anúncio despublicado" })).ToBeVisibleAsync().ConfigureAwait(false);

        // Saem da página inicial, da busca, dos favoritos (com o aviso) e do mapa
        await visitor.GotoAsync(Url("/")).ConfigureAwait(false);
        await Expect(CardLink(visitor, book)).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(CardLink(visitor, service)).ToHaveCountAsync(0).ConfigureAwait(false);
        await Expect(CardLink(visitor, job)).ToHaveCountAsync(0).ConfigureAwait(false);
        await visitor.GotoAsync(Url($"/busca?q={token}")).ConfigureAwait(false);
        await Expect(visitor.Locator("[data-search-total]")).ToHaveTextAsync("1 anúncio encontrado").ConfigureAwait(false);
        await visitor.GotoAsync(Url("/favoritos")).ConfigureAwait(false);
        await Expect(visitor.Locator("[data-favorites-unavailable]")).ToHaveTextAsync("2 anúncios favoritados deixaram de estar disponíveis e foram removidos da sua lista.").ConfigureAwait(false);
        await Expect(visitor.GetByText("Você ainda não favoritou nenhum anúncio")).ToBeVisibleAsync().ConfigureAwait(false);
        string[] after = await SitemapLocsAsync().ConfigureAwait(false);
        Assert.IsFalse(after.Any(l => l.Contains($"/anuncio/{serviceId}/", StringComparison.Ordinal) || l.Contains($"/anuncio/{jobId}/", StringComparison.Ordinal)), "o mapa também");
        foreach (string path in new[] { serviceCardPath, jobPath })
        {
            IResponse gone = (await visitor.GotoAsync(Url(path)).ConfigureAwait(false))!;
            Assert.AreEqual(404, gone.Status, path);
        }

        // Só os 404 pedidos de propósito acima são erros; nada mais falhou
        Assert.IsTrue(failures.All(f => f.StartsWith("404 ", StringComparison.Ordinal)), string.Join("; ", failures));
        await visitor.Context.CloseAsync().ConfigureAwait(false);
    }
}
