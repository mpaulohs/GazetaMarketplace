using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Core.Search;
using GazetaMarketplace.Web.Tests.Ads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Search;

/// <summary>
/// O que a busca entende do endereço (US-002; tarefa 5.4), sem passar pela tela: texto normalizado em palavras, categoria pelo slug com as descendentes, UF e cidade, preço em reais,
/// características só quando o grupo da categoria as oferece, ordenação e página. Parâmetro inválido volta ao padrão; só o valor ilegível, a faixa invertida e o texto longo geram erro.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SearchRulesTests
#pragma warning restore CA1515
{
    private const int Cars = 33;
    private const int Motorcycles = 36;
    private const int Trucks = 34;
    private const int Land = 30;
    private const int Apartments = 26;
    private const int General = 86;

    private static SearchInput Input(
        string q = null, string categoria = null, string uf = null, string cidade = null, string precoMin = null, string precoMax = null, string marca = null, string modelo = null,
        string anoDe = null, string anoAte = null, string kmMax = null, string areaMin = null, string areaMax = null, string ordem = null, string pagina = null) =>
        new(q, categoria, uf, cidade, precoMin, precoMax, marca, modelo, anoDe, anoAte, kmMax, areaMin, areaMax, ordem, pagina);

    private static async Task<string> SlugOfAsync(DraftSite site, int id) =>
        (await site.Harness.Factory.Services.GetRequiredService<ICategoryTree>().GetAsync(CancellationToken.None)).Find(id).Slug;

    private static async Task<SearchForm> PrepareAsync(DraftSite site, SearchInput input)
    {
        using IServiceScope scope = site.Harness.Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISearch>().PrepareAsync(input, CancellationToken.None);
    }

    private static Task<SearchForm> PrepareAsync(DraftSite site, Func<SearchInput> input) => PrepareAsync(site, input());

    private static string Query(SearchForm form) => string.Join("&", form.Query.Select(p => p.Key + "=" + p.Value));

    // ---------- Texto ----------

    [TestMethod]
    public async Task Texto_ViraPalavrasSemAcentoEMinusculas_ComAcentoEMaiusculaNaoImportam()
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm form = await PrepareAsync(site, Input(q: "  CÍVIC   Sítio  "));

        CollectionAssert.AreEqual(new[] { "civic", "sitio" }, form.Criteria.Words.ToArray());
        Assert.AreEqual("CÍVIC   Sítio", form.Text, "o campo mostra o que a pessoa digitou (sem as pontas)");
        Assert.AreEqual("CÍVIC   Sítio", form.Query.Single(p => p.Key == "q").Value);
    }

    [TestMethod]
    public async Task Texto_ValeNoMaximoCincoPalavras_RepetidasContamUmaVez()
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm form = await PrepareAsync(site, Input(q: "um dois dois tres quatro cinco seis sete"));

        CollectionAssert.AreEqual(new[] { "um", "dois", "tres", "quatro", "cinco" }, form.Criteria.Words.ToArray());
    }

    [TestMethod]
    [DataRow("100%")]
    [DataRow("a_b")]
    [DataRow("[abc]")]
    [DataRow("o'brien")]
    [DataRow("x; DROP TABLE Ads;--")]
    public async Task Texto_PorcentoSublinhadoColcheteEAspas_ValemComoTextoComum(string text)
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm form = await PrepareAsync(site, Input(q: text));

        Assert.IsFalse(form.HasErrors);
        CollectionAssert.AreEqual(GazetaMarketplace.Core.Search.Normalizer.Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(5).ToArray(), form.Criteria.Words.ToArray(), "o texto vira palavras literais, sem escape");
    }

    [TestMethod]
    public async Task Texto_Com100Caracteres_Vale_Com101_MostraErroJuntoDoCampoESaiSemOTexto()
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm ok = await PrepareAsync(site, Input(q: new string('a', 100)));
        SearchForm tooLong = await PrepareAsync(site, Input(q: new string('a', 101) + " civic"));

        Assert.IsFalse(ok.HasErrors);
        Assert.AreEqual(1, ok.Criteria.Words.Count);
        Assert.AreEqual(SearchService.TermTooLongMessage, tooLong.Errors["q"]);
        Assert.AreEqual(0, tooLong.Criteria.Words.Count, "a lista sai sem o texto");
        Assert.IsFalse(tooLong.Query.Any(p => p.Key == "q"), "o texto longo não vai para os links");
    }

    // ---------- Categoria, local e ordem ----------

    [TestMethod]
    public async Task Categoria_PeloSlug_IncluiTodasAsDescendentes()
    {
        using DraftSite site = await DraftSite.StartAsync();
        CategoryTreeSnapshot tree = await site.Harness.Factory.Services.GetRequiredService<ICategoryTree>().GetAsync(CancellationToken.None);
        CategoryNode root = tree.Roots.First(r => tree.DescendantsOf(r.Id).Count > 3);

        SearchForm form = await PrepareAsync(site, Input(categoria: root.Slug));

        Assert.AreEqual(root.Id, form.Category.Id);
        CollectionAssert.AreEquivalent(new[] { root.Id }.Concat(tree.DescendantsOf(root.Id).Select(c => c.Id)).ToArray(), form.Criteria.CategoryIds.ToArray());
        Assert.AreEqual("categoria=" + root.Slug, Query(form));
    }

    [TestMethod]
    [DataRow("naoexiste")]
    [DataRow("33")]
    [DataRow("<script>")]
    [DataRow("")]
    public async Task Categoria_QueNaoExiste_VoltaAoPadraoSemErro(string slug)
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm form = await PrepareAsync(site, Input(categoria: slug));

        Assert.IsNull(form.Category);
        Assert.AreEqual(0, form.Criteria.CategoryIds.Count);
        Assert.IsFalse(form.HasErrors);
    }

    [TestMethod]
    public async Task Cidade_SoDepoisDaUf_ESempreDaUf()
    {
        using DraftSite site = await DraftSite.StartAsync();
        await site.Harness.AddCitiesAsync(new City { IbgeCode = 3304557, Name = "Rio de Janeiro", Uf = "RJ", NameSearch = "rio de janeiro" }); // o banco de teste já tem Campinas e São Paulo

        SearchForm withUf = await PrepareAsync(site, Input(uf: "sp", cidade: "CAMPINAS"));
        SearchForm withoutUf = await PrepareAsync(site, Input(cidade: "Campinas"));
        SearchForm otherUf = await PrepareAsync(site, Input(uf: "RJ", cidade: "Campinas"));
        SearchForm invalidUf = await PrepareAsync(site, Input(uf: "XX", cidade: "Campinas"));

        Assert.AreEqual("SP", withUf.Criteria.Uf);
        Assert.AreEqual("Campinas", withUf.Criteria.City, "o nome oficial, com acento e maiúscula certos");
        Assert.AreEqual("uf=SP&cidade=Campinas", Query(withUf));
        Assert.IsNull(withoutUf.Criteria.City, "sem UF a cidade não vale");
        Assert.AreEqual("RJ", otherUf.Criteria.Uf);
        Assert.IsNull(otherUf.Criteria.City, "Campinas não é do Rio de Janeiro");
        Assert.IsTrue(otherUf.Cities.Count > 0 && otherUf.Cities.Any(c => c.Name == "Rio de Janeiro") && otherUf.Cities.All(c => c.IbgeCode / 100_000 == 33), "a lista é só das cidades da UF escolhida (código do IBGE 33xxxxx)");
        Assert.IsNull(invalidUf.Criteria.Uf);
        Assert.AreEqual(0, invalidUf.Cities.Count);
    }

    [TestMethod]
    [DataRow("menor-preco", SearchOrder.PriceAscending)]
    [DataRow("maior-preco", SearchOrder.PriceDescending)]
    [DataRow("recentes", SearchOrder.Recent)]
    [DataRow("xyz", SearchOrder.Recent)]
    [DataRow("", SearchOrder.Recent)]
    [DataRow("a.PublishedAt; DROP TABLE Ads", SearchOrder.Recent)]
    public async Task Ordem_SoAsTresDaLista_QualquerOutraCoisaViraRecentes(string slug, SearchOrder expected)
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm form = await PrepareAsync(site, Input(ordem: slug));

        Assert.AreEqual(expected, form.Criteria.Order);
        Assert.AreEqual(expected == SearchOrder.Recent ? string.Empty : "ordem=" + slug, Query(form), "só a ordem diferente do padrão vai para o endereço");
    }

    [TestMethod]
    [DataRow("2", 2)]
    [DataRow("abc", 1)]
    [DataRow("-3", 1)]
    [DataRow("0", 1)]
    [DataRow("1.5", 1)]
    [DataRow("99999999999999999999", 1)]
    [DataRow("100001", 100_000)]
    [DataRow(null, 1)]
    public async Task Pagina_InvalidaVoltaAUm_EnormeVaiAoLimite(string page, int expected)
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm form = await PrepareAsync(site, Input(pagina: page));

        Assert.AreEqual(expected, form.Criteria.Page);
        Assert.IsFalse(form.HasErrors);
    }

    // ---------- Preço ----------

    [TestMethod]
    [DataRow("50000", 5_000_000L)]
    [DataRow("50.000", 5_000_000L)]
    [DataRow("50.000,00", 5_000_000L)]
    [DataRow("50000,5", 5_000_050L)]
    [DataRow("50000.50", 5_000_050L)]
    [DataRow("R$ 1.234,56", 123_456L)]
    [DataRow("0", 0L)]
    [DataRow("99999999,99", 9_999_999_999L)]
    public async Task Preco_EmReaisInteiroOuComVirgula_ViraCentavos(string typed, long cents)
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm form = await PrepareAsync(site, Input(precoMax: typed));

        Assert.AreEqual(cents, form.Criteria.PriceMaxCents);
        Assert.IsFalse(form.HasErrors);
    }

    [TestMethod]
    [DataRow("abc")]
    [DataRow("-5")]
    [DataRow("1,2,3")]
    [DataRow("50.00.0")]
    [DataRow("50000,123")]
    [DataRow("100000000")]
    [DataRow("1e3")]
    [DataRow("5 000")]
    public async Task Preco_Ilegivel_MostraErroNoCampoEFicaSemEleNaLista(string typed)
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm form = await PrepareAsync(site, Input(precoMin: typed, precoMax: "100"));

        Assert.AreEqual(SearchService.PriceFormatMessage, form.Errors["precoMin"]);
        Assert.IsNull(form.Criteria.PriceMinCents);
        Assert.AreEqual(10_000L, form.Criteria.PriceMaxCents, "o outro lado da faixa continua valendo");
        Assert.AreEqual(typed.Trim(), form.PriceMin, "o campo mostra o que foi digitado");
    }

    [TestMethod]
    public async Task Preco_Invertido_MostraAMensagemDaSpecESaiSemAFaixa()
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm form = await PrepareAsync(site, Input(q: "civic", precoMin: "5000", precoMax: "1000"));

        Assert.AreEqual("O preço mínimo não pode ser maior que o máximo", form.Errors["preco"]);
        Assert.IsNull(form.Criteria.PriceMinCents);
        Assert.IsNull(form.Criteria.PriceMaxCents);
        Assert.AreEqual("q=civic", Query(form), "a faixa errada não vai para os links; o resto vale");
        Assert.AreEqual("5000", form.PriceMin);
        Assert.AreEqual("1000", form.PriceMax);
    }

    [TestMethod]
    public async Task Preco_MinimoIgualAoMaximo_Vale()
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm form = await PrepareAsync(site, Input(precoMin: "1000", precoMax: "1000"));

        Assert.IsFalse(form.HasErrors);
        Assert.AreEqual(100_000L, form.Criteria.PriceMinCents);
        Assert.AreEqual(100_000L, form.Criteria.PriceMaxCents);
    }

    // ---------- Características do bem (só as do grupo da categoria) ----------

    [TestMethod]
    public async Task Veiculo_MarcaAnoEQuilometragem_SoComCategoriaDeVeiculos()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string cars = await SlugOfAsync(site, Cars);
        string general = await SlugOfAsync(site, General);

        SearchForm withCars = await PrepareAsync(site, Input(categoria: cars, marca: "1", anoDe: "2015", anoAte: "2020", kmMax: "100000"));
        SearchForm withGeneral = await PrepareAsync(site, Input(categoria: general, marca: "1", anoDe: "2015", anoAte: "2020", kmMax: "100000"));
        SearchForm without = await PrepareAsync(site, Input(marca: "1", anoDe: "2015", kmMax: "100000"));

        Assert.AreEqual(1, withCars.Criteria.BrandId);
        Assert.AreEqual(2015, withCars.Criteria.YearFrom);
        Assert.AreEqual(2020, withCars.Criteria.YearTo);
        Assert.AreEqual(100_000, withCars.Criteria.KmMax);
        Assert.AreEqual(SearchFilter.Brand | SearchFilter.Model | SearchFilter.Year | SearchFilter.Km, withCars.Filters);
        Assert.AreEqual("car", withCars.Kind);
        Assert.AreEqual($"categoria={cars}&marca=1&anoDe=2015&anoAte=2020&kmMax=100000", Query(withCars));
        foreach (SearchForm form in new[] { withGeneral, without })
        {
            Assert.AreEqual(SearchFilter.None, form.Filters, "os filtros de veículo só aparecem para categoria de veículos");
            Assert.IsNull(form.Criteria.BrandId);
            Assert.IsNull(form.Criteria.YearFrom);
            Assert.IsNull(form.Criteria.KmMax);
            Assert.IsFalse(form.HasErrors, "valor de um filtro que não existe nesta categoria é ignorado, sem erro");
        }
    }

    [TestMethod]
    public async Task Marca_EModelo_SaoDoCatalogoDoTipoDaCategoria_ModeloDeOutraMarcaCai()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string cars = await SlugOfAsync(site, Cars);
        string motos = await SlugOfAsync(site, Motorcycles);

        SearchForm civic = await PrepareAsync(site, Input(categoria: cars, marca: "1", modelo: "11"));
        SearchForm unknownBrand = await PrepareAsync(site, Input(categoria: cars, marca: "999", modelo: "11"));
        SearchForm otherBrandModel = await PrepareAsync(site, Input(categoria: cars, marca: "2", modelo: "11"));
        SearchForm noBrand = await PrepareAsync(site, Input(categoria: cars, modelo: "11"));
        SearchForm bmwMoto = await PrepareAsync(site, Input(categoria: motos, marca: "2"));
        SearchForm audiOnMotos = await PrepareAsync(site, Input(categoria: motos, marca: "3"));

        Assert.AreEqual((1, 11), (civic.Criteria.BrandId, civic.Criteria.ModelId));
        CollectionAssert.AreEquivalent(new[] { "Fit", "Civic", "City" }, civic.Models.Select(m => m.Name).ToArray(), "os modelos da marca escolhida");
        CollectionAssert.IsSubsetOf(new[] { "Honda", "Toyota", "Audi" }, civic.Brands.Select(b => b.Name).ToArray());
        Assert.IsNull(unknownBrand.Criteria.BrandId);
        Assert.IsNull(unknownBrand.Criteria.ModelId);
        Assert.AreEqual(2, otherBrandModel.Criteria.BrandId);
        Assert.IsNull(otherBrandModel.Criteria.ModelId, "o modelo 11 não é da Toyota");
        Assert.IsNull(noBrand.Criteria.ModelId, "modelo sem marca não vale");
        Assert.AreEqual("moto", bmwMoto.Kind);
        Assert.AreEqual(2, bmwMoto.Criteria.BrandId, "a BMW existe no catálogo de motos");
        Assert.IsNull(audiOnMotos.Criteria.BrandId, "a Audi só existe no catálogo de carros");
    }

    [TestMethod]
    public async Task CaminhoesEOnibus_SoAnoEQuilometragem_SemMarca()
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm form = await PrepareAsync(site, Input(categoria: await SlugOfAsync(site, Trucks), marca: "1", anoDe: "2010", kmMax: "500000"));

        Assert.AreEqual(SearchFilter.Year | SearchFilter.Km, form.Filters);
        Assert.IsNull(form.Kind);
        Assert.IsNull(form.Criteria.BrandId);
        Assert.AreEqual(2010, form.Criteria.YearFrom);
        Assert.AreEqual(500_000, form.Criteria.KmMax);
    }

    [TestMethod]
    public async Task Area_SoDeImoveis_ComVirgulaOuPonto()
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm land = await PrepareAsync(site, Input(categoria: await SlugOfAsync(site, Land), areaMin: "300", areaMax: "600,5"));
        SearchForm general = await PrepareAsync(site, Input(categoria: await SlugOfAsync(site, General), areaMin: "300"));

        Assert.AreEqual(SearchFilter.Area, land.Filters);
        Assert.AreEqual(300m, land.Criteria.AreaMin);
        Assert.AreEqual(600.5m, land.Criteria.AreaMax);
        Assert.IsNull(general.Criteria.AreaMin);
    }

    [TestMethod]
    public async Task Ano_ForaDoIntervaloOuInvertido_MostraErro()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string cars = await SlugOfAsync(site, Cars);
        int max = ModelYearRules.MaxYear(ModelYearRules.CurrentYear(TimeProvider.System));

        SearchForm tooOld = await PrepareAsync(site, Input(categoria: cars, anoDe: "1949"));
        SearchForm future = await PrepareAsync(site, Input(categoria: cars, anoAte: (max + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)));
        SearchForm inverted = await PrepareAsync(site, Input(categoria: cars, anoDe: "2020", anoAte: "2015"));
        SearchForm boundary = await PrepareAsync(site, Input(categoria: cars, anoDe: "1950", anoAte: max.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        Assert.IsTrue(tooOld.Errors.ContainsKey("anoDe"));
        Assert.IsNull(tooOld.Criteria.YearFrom);
        Assert.IsTrue(future.Errors.ContainsKey("anoAte"));
        Assert.AreEqual(SearchService.YearRangeMessage, inverted.Errors["ano"]);
        Assert.IsNull(inverted.Criteria.YearFrom);
        Assert.IsNull(inverted.Criteria.YearTo);
        Assert.IsFalse(boundary.HasErrors);
    }

    [TestMethod]
    [DataRow("-1")]
    [DataRow("abc")]
    [DataRow("10000000")]
    [DataRow("1,5")]
    public async Task Quilometragem_Ilegivel_MostraErro(string typed)
    {
        using DraftSite site = await DraftSite.StartAsync();

        SearchForm form = await PrepareAsync(site, Input(categoria: await SlugOfAsync(site, Cars), kmMax: typed));

        Assert.AreEqual(SearchService.KmMessage, form.Errors["kmMax"]);
        Assert.IsNull(form.Criteria.KmMax);
    }

    [TestMethod]
    public async Task Area_InvertidaOuIlegivel_MostraErro()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string land = await SlugOfAsync(site, Land);

        SearchForm inverted = await PrepareAsync(site, Input(categoria: land, areaMin: "600", areaMax: "300"));
        SearchForm illegible = await PrepareAsync(site, Input(categoria: land, areaMin: "muito"));

        Assert.AreEqual(SearchService.AreaRangeMessage, inverted.Errors["area"]);
        Assert.IsNull(inverted.Criteria.AreaMin);
        Assert.AreEqual(SearchService.AreaFormatMessage, illegible.Errors["areaMin"]);
    }

    // ---------- Quais categorias oferecem cada filtro ----------

    [TestMethod]
    public async Task FiltrosPorCategoria_ListaExata_SoCarrosMotosCaminhoesOnibusEImoveis()
    {
        using DraftSite site = await DraftSite.StartAsync();
        SearchForm form = await PrepareAsync(site, Input());

        Dictionary<string, string> withFilters = form.CategoryOptions.Where(o => o.Filters != SearchFilter.None).ToDictionary(o => o.Name, o => SearchFilters.Names(o.Filters));

        CollectionAssert.AreEquivalent(
            new Dictionary<string, string>
            {
                ["Carros, vans e utilitários"] = "brand model year km",
                ["Motos"] = "brand model year km",
                ["Caminhões"] = "year km",
                ["Ônibus"] = "year km",
                ["Apartamentos"] = "area",
                ["Casas"] = "area",
                ["Terrenos, sítios e fazendas"] = "area",
                ["Comércio e indústria"] = "area"
            },
            withFilters);
    }

    [TestMethod]
    public void TodoCampoFiltravelDoRegistro_ETemFiltroNaBusca()
    {
        string[] unsupported = [.. FieldGroupRegistry.All
            .SelectMany(group => group.Fields)
            .Where(field => field.Filter != FieldFilter.None && !SearchFilters.ByFieldKey.ContainsKey(field.Key))
            .Select(field => field.Key)
            .Distinct()];

        Assert.AreEqual(0, unsupported.Length, "campo filtrável sem filtro na busca: " + string.Join(", ", unsupported));
    }

    [TestMethod]
    public async Task CategoriaPrincipalEPecas_NaoOferecemFiltroEspecifico()
    {
        using DraftSite site = await DraftSite.StartAsync();
        SearchForm form = await PrepareAsync(site, Input());
        CategoryTreeSnapshot tree = await site.Harness.Factory.Services.GetRequiredService<ICategoryTree>().GetAsync(CancellationToken.None);

        foreach (CategoryNode root in tree.Roots)
        {
            SearchCategoryOption option = form.CategoryOptions.Single(o => o.Slug == root.Slug);
            Assert.AreEqual(SearchFilter.None, option.Filters, $"a principal \"{root.Name}\" mistura grupos: sem filtro específico");
        }

        foreach (CategoryNode part in tree.All.Where(c => FieldGroupRegistry.Resolve(tree, c.Id).Key == FieldGroupKeys.Parts))
        {
            Assert.AreEqual(SearchFilter.None, form.CategoryOptions.Single(o => o.Slug == part.Slug).Filters, part.Name);
        }

        Assert.AreEqual(SearchFilter.Area, form.CategoryOptions.Single(o => o.Slug == tree.Find(Apartments).Slug).Filters);
    }

    [TestMethod]
    public async Task ListaDeCategorias_NaOrdemDaArvore_ComRecuoPorNivel()
    {
        using DraftSite site = await DraftSite.StartAsync();
        SearchForm form = await PrepareAsync(site, Input());
        CategoryTreeSnapshot tree = await site.Harness.Factory.Services.GetRequiredService<ICategoryTree>().GetAsync(CancellationToken.None);

        CollectionAssert.AreEqual(tree.All.Select(c => c.Slug).ToArray(), form.CategoryOptions.Select(o => o.Slug).ToArray());
        CollectionAssert.AreEqual(tree.All.Select(c => c.Depth).ToArray(), form.CategoryOptions.Select(o => o.Depth).ToArray());
    }
}
