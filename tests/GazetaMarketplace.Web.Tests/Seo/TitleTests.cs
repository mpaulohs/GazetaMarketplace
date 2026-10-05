using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Seo;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Detail;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Seo;

/// <summary>
/// Título, descrição, endereço canônico, regra de robôs e Open Graph de cada página pública (NFR-21; tarefa 5.6). Indexáveis: início, categoria e anúncio publicado, cada um com título e descrição próprios.
/// Todas as outras (busca, favoritos, anúncio indisponível, categoria que não existe, páginas de erro) saem com <c>noindex</c>.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class TitleTests
#pragma warning restore CA1515
{
    private const string BaseUrl = "https://gazeta.exemplo.com.br";
    private const int Cars = 33;
    private const int Jobs = 96;

    private static Dictionary<string, string> Configuration => new() { ["Site:BaseUrl"] = BaseUrl };

    private static string Decode(string value) => WebUtility.HtmlDecode(value);

    private static string TitleOf(string html) => Decode(Regex.Match(html, @"<title>([^<]*)</title>").Groups[1].Value);

    private static string[] Metas(string html, string attribute, string name) =>
        [.. Regex.Matches(html, $@"<meta {attribute}=""{Regex.Escape(name)}"" content=""([^""]*)"" />").Select(m => Decode(m.Groups[1].Value))];

    private static string Description(string html) => Metas(html, "name", "description").SingleOrDefault();

    private static string Robots(string html) => Metas(html, "name", "robots").Single();

    private static string[] Canonicals(string html) => [.. Regex.Matches(html, @"<link rel=""canonical"" href=""([^""]*)"" />").Select(m => Decode(m.Groups[1].Value))];

    private static async Task<(HttpResponseMessage Response, string Html)> GetAsync(DraftSite site, string path)
    {
        using HttpClient visitor = site.Harness.Anonymous();
        HttpResponseMessage response = await visitor.GetAsync(path);
        return (response, await response.Content.ReadAsStringAsync());
    }

    private static StubShowcaseRepository Stub(DraftSite site) => site.Harness.Factory.Services.GetRequiredService<StubShowcaseRepository>();

    private static async Task<CategoryTreeSnapshot> TreeAsync(DraftSite site) => await site.Harness.Factory.Services.GetRequiredService<ICategoryTree>().GetAsync(default);

    private static ShowcaseRow Row(int id) => new(id, $"Anúncio {id:00}", Cars, 6_200_000, "{}", "Campinas", "SP", null, null, null);

    // ---------- Início ----------

    [TestMethod]
    public async Task Inicio_TemTituloEDescricaoProprios_CanonicoComOEnderecoDoSite_EEIndexavel()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);

        (_, string html) = await GetAsync(site, "/");

        Assert.AreEqual(SeoTexts.HomeTitle + " · GazetaMarketplace", TitleOf(html));
        Assert.AreEqual(SeoTexts.HomeDescription, Description(html));
        CollectionAssert.AreEqual(new[] { BaseUrl + "/" }, Canonicals(html));
        Assert.AreEqual(0, Metas(html, "name", "robots").Length, "indexável: sem regra de robôs");
        Assert.IsTrue(SeoTexts.HomeDescription.Length <= AdMetaDescription.MaxLength);
    }

    // ---------- Categoria ----------

    [TestMethod]
    public async Task Categoria_TemTituloEDescricaoDelaMesma_ECanonicoSemParametros()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        CategoryTreeSnapshot tree = await TreeAsync(site);
        CategoryNode cars = tree.Find(Cars);

        (_, string html) = await GetAsync(site, $"/categoria/{cars.Slug}?utm_source=jornal&ordem=preco");

        CategoryNode parent = tree.Find(cars.ParentId!.Value);
        Assert.AreEqual($"Anúncios de {cars.Name} · {parent.Name} · GazetaMarketplace", TitleOf(html), "subcategoria: o pai desfaz o empate entre nomes iguais");
        StringAssert.Contains(Description(html), $"{cars.Name} ({parent.Name})");
        CollectionAssert.AreEqual(new[] { $"{BaseUrl}/categoria/{cars.Slug}" }, Canonicals(html), "parâmetros de rastreio não entram no canônico");
        Assert.AreEqual(0, Metas(html, "name", "robots").Length);
    }

    [TestMethod]
    public async Task Categoria_Pagina2_TemOProprioTituloEOProprioCanonico()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        CategoryTreeSnapshot tree = await TreeAsync(site);
        CategoryNode cars = tree.Find(Cars);
        Stub(site).Rows.AddRange(Enumerable.Range(1, 60).Select(Row));

        (_, string html) = await GetAsync(site, $"/categoria/{cars.Slug}?pagina=2&utm_source=jornal");

        Assert.AreEqual($"Anúncios de {cars.Name} · {tree.Find(cars.ParentId!.Value).Name} — página 2 · GazetaMarketplace", TitleOf(html));
        CollectionAssert.AreEqual(new[] { $"{BaseUrl}/categoria/{cars.Slug}?pagina=2" }, Canonicals(html), "cada página da lista aponta para si mesma");
    }

    [TestMethod]
    public async Task Categoria_PaginaAlemDoFim_MostraAUltima_ECanonicoDaUltima()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        CategoryNode cars = (await TreeAsync(site)).Find(Cars);
        Stub(site).Rows.AddRange(Enumerable.Range(1, 30).Select(Row));

        (_, string html) = await GetAsync(site, $"/categoria/{cars.Slug}?pagina=99");

        CollectionAssert.AreEqual(new[] { $"{BaseUrl}/categoria/{cars.Slug}?pagina=2" }, Canonicals(html));
    }

    [TestMethod]
    public async Task TodasAsCategorias_TemDescricaoAte160Caracteres_ETituloEDescricaoDistintos()
    {
        using DraftSite site = await DraftSite.StartAsync();
        CategoryTreeSnapshot tree = await TreeAsync(site);

        List<string> titles = [];
        List<string> descriptions = [];
        foreach (CategoryNode category in tree.All)
        {
            string parent = category.ParentId is { } parentId ? tree.Find(parentId).Name : null;
            string description = SeoTexts.CategoryDescription(category.Name, parent);
            Assert.IsTrue(description.Length <= AdMetaDescription.MaxLength, $"{category.Name}: {description.Length} caracteres");
            StringAssert.Contains(description, category.Name.Length > 60 ? category.Name[..40] : category.Name);
            titles.Add(SeoTexts.CategoryTitle(category.Name, parent, 1));
            descriptions.Add(description);
        }

        Assert.IsTrue(tree.Count >= 100, "a varredura cobre a árvore real");
        Assert.AreEqual(titles.Count, titles.Distinct().Count(), "nenhum título de categoria se repete");
        Assert.AreEqual(descriptions.Count, descriptions.Distinct().Count(), "nenhuma descrição de categoria se repete");
    }

    [TestMethod]
    public async Task CategoriaQueNaoExiste_Da404_ENoindex_SemDescricaoNemCanonico()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);

        (HttpResponseMessage response, string html) = await GetAsync(site, "/categoria/nao-existe-mais");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreEqual("noindex, follow", Robots(html));
        Assert.AreEqual(0, Canonicals(html).Length);
        Assert.IsNull(Description(html));
    }

    // ---------- Anúncio ----------

    [TestMethod]
    public async Task Anuncio_TemTituloDescricaoCanonicoEOpenGraphMinimo_ComACapaEmMiniatura()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        int id = await AdDetailTests.AddAsync(site, "Honda Civic 2018", Cars, photos: 2);

        (_, string html) = await AdDetailTests.GetRawAsync(site, AdDetailTests.Url(id, "Honda Civic 2018"));

        Assert.AreEqual("Honda Civic 2018 · GazetaMarketplace", TitleOf(html));
        Assert.AreEqual("Único dono, revisões feitas na concessionária. Aceita troca.", Description(html));
        CollectionAssert.AreEqual(new[] { $"{BaseUrl}/anuncio/{id}/honda-civic-2018" }, Canonicals(html));
        Assert.AreEqual(0, Metas(html, "name", "robots").Length);
        CollectionAssert.AreEqual(new[] { "Honda Civic 2018" }, Metas(html, "property", "og:title"));
        CollectionAssert.AreEqual(new[] { Description(html) }, Metas(html, "property", "og:description"));
        string image = Metas(html, "property", "og:image").Single();
        StringAssert.Matches(image, new Regex($@"^{Regex.Escape(BaseUrl)}/fotos/{id}/\d+-480\.webp$"), "a capa em miniatura, com o endereço completo do site");
        Assert.AreEqual(3, Regex.Matches(html, @"<meta property=""og:").Count, "só o mínimo: título, descrição e imagem");
    }

    [TestMethod]
    public async Task Anuncio_SemFoto_FicaSemOgImage()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        int id = await AdDetailTests.AddAsync(site, "Violão Giannini", 86, photos: 0);

        (_, string html) = await AdDetailTests.GetRawAsync(site, AdDetailTests.Url(id, "Violão Giannini"));

        Assert.AreEqual(0, Metas(html, "property", "og:image").Length);
        Assert.AreEqual(1, Metas(html, "property", "og:title").Length);
    }

    [TestMethod]
    public async Task Anuncio_DeVaga_NaoTemOgImage_PoisVagaNaoMostraFoto()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        int id = await AdDetailTests.AddAsync(site, "Pizzaiolo", Jobs, photos: 1);

        (_, string html) = await AdDetailTests.GetRawAsync(site, AdDetailTests.Url(id, "Pizzaiolo"));

        Assert.AreEqual(0, Metas(html, "property", "og:image").Length);
    }

    [TestMethod]
    public async Task Anuncio_ComTextoPerigosoNoTitulo_SaiCodificadoNasMetas()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        const string title = "Casa \"boa\" <b>x</b> & cia";
        int id = await AdDetailTests.AddAsync(site, title, Cars, photos: 0);

        (_, string html) = await AdDetailTests.GetRawAsync(site, AdDetailTests.Url(id, title));

        Assert.IsFalse(html.Contains("<b>x</b>", StringComparison.Ordinal) && Regex.IsMatch(html, @"og:title"" content=""[^""]*<b>"), "o título não vira marcação");
        Assert.AreEqual(title, Metas(html, "property", "og:title").Single(), "lido de volta, o texto é o original");
    }

    [TestMethod]
    public async Task AnuncioIndisponivel_Da404_ENoindex_SemDescricaoCanonicoNemOpenGraph()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        int archived = await AdDetailTests.AddAsync(site, "Carro arquivado", Cars, status: AdStatus.Archived);

        foreach (string path in new[] { $"/anuncio/{archived}/carro-arquivado", "/anuncio/999999/qualquer" })
        {
            (HttpResponseMessage response, string html) = await GetAsync(site, path);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, path);
            Assert.AreEqual("noindex", Robots(html), path);
            Assert.AreEqual(0, Canonicals(html).Length, path);
            Assert.IsNull(Description(html), path);
            Assert.AreEqual(0, Regex.Matches(html, @"og:").Count, path);
        }
    }

    // ---------- As demais páginas ----------

    [TestMethod]
    public async Task BuscaEFavoritos_SaemComNoindexFollow_ESemCanonico()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);

        foreach (string path in new[] { "/busca", "/busca?q=civic&uf=SP", "/favoritos" })
        {
            (HttpResponseMessage response, string html) = await GetAsync(site, path);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, path);
            Assert.AreEqual("noindex, follow", Robots(html), path);
            Assert.AreEqual(0, Canonicals(html).Length, path);
        }
    }

    [TestMethod]
    public async Task PaginaDeErroDaVitrine_SaiComNoindex()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        Stub(site).Failure = new InvalidOperationException("falha");

        (HttpResponseMessage response, string html) = await GetAsync(site, "/");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.AreEqual("noindex, follow", Robots(html));
        Assert.AreEqual(0, Canonicals(html).Length);
    }

    // ---------- O conjunto ----------

    [TestMethod]
    public async Task CadaPaginaIndexavel_TemTituloEDescricaoProprios_ENenhumaRepeteOutra()
    {
        using DraftSite site = await DraftSite.StartAsync(extraConfiguration: Configuration);
        CategoryTreeSnapshot tree = await TreeAsync(site);
        int first = await AdDetailTests.AddAsync(site, "Honda Civic 2018", Cars);
        int second = await AdDetailTests.AddAsync(site, "Toyota Corolla 2020", Cars, configure: ad => ad.SetText("Corolla XEi, revisada.", "Revisada, pneus novos."));
        string[] paths =
        [
            "/",
            $"/categoria/{tree.Find(Cars).Slug}",
            $"/categoria/{tree.Find(36).Slug}",
            AdDetailTests.Url(first, "Honda Civic 2018"),
            AdDetailTests.Url(second, "Corolla XEi, revisada.")
        ];

        List<(string Title, string Description, string Canonical)> pages = [];
        foreach (string path in paths)
        {
            (HttpResponseMessage response, string html) = await GetAsync(site, path);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, path);
            Assert.IsTrue(TitleOf(html).Length > 0, path);
            Assert.IsTrue(Description(html)?.Length is > 0 and <= AdMetaDescription.MaxLength, $"{path}: descrição ausente ou acima de 160");
            Assert.AreEqual(1, Canonicals(html).Length, path);
            Assert.AreEqual(1, Regex.Matches(html, @"<title>").Count, path);
            pages.Add((TitleOf(html), Description(html), Canonicals(html)[0]));
        }

        Assert.AreEqual(paths.Length, pages.Select(p => p.Title).Distinct().Count(), "títulos todos diferentes");
        Assert.AreEqual(paths.Length, pages.Select(p => p.Description).Distinct().Count(), "descrições todas diferentes");
        Assert.AreEqual(paths.Length, pages.Select(p => p.Canonical).Distinct().Count(), "endereços canônicos todos diferentes");
    }

    [TestMethod]
    public void TextosFixos_CategoriaPaginadaEntraNoTitulo_SoDaSegundaEmDiante()
    {
        Assert.AreEqual("Anúncios de Imóveis", SeoTexts.CategoryTitle("Imóveis", null, 1));
        Assert.AreEqual("Anúncios de Casas · Imóveis", SeoTexts.CategoryTitle("Casas", "Imóveis", 0));
        Assert.AreEqual("Anúncios de Casas · Imóveis — página 3", SeoTexts.CategoryTitle("Casas", "Imóveis", 3));
        Assert.AreNotEqual(SeoTexts.CategoryTitle("Serviços", null, 1), SeoTexts.CategoryTitle("Serviços", "Serviços", 1), "mesmo nome em pai e filho: títulos diferentes");
        Assert.ThrowsExactly<ArgumentException>(() => SeoTexts.CategoryTitle(" ", null, 1));
        Assert.ThrowsExactly<ArgumentNullException>(() => SeoTexts.CategoryDescription(null, null));
    }

    [TestMethod]
    public void DescricaoDeCategoriaComNomeMuitoLongo_SeCortaEm160SemPartirPalavra()
    {
        string description = SeoTexts.CategoryDescription(string.Join(' ', Enumerable.Repeat("Automóveis", 30)), "Veículos");

        Assert.IsTrue(description.Length <= AdMetaDescription.MaxLength);
        Assert.IsTrue(description.EndsWith('…'));
    }
}
