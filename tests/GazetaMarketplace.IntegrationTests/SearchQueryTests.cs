using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Search;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Core.VehicleCatalog;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Infrastructure.Search;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// A busca pública (US-002; ADR-006) no T-SQL de verdade: só publicados (um anúncio em cada situação), texto sem acento nem maiúscula em título e descrição, todas as palavras, categoria com
/// descendentes, UF e cidade, preço, marca, ano, quilometragem e área, Serviços fora da faixa de preço e no fim das ordenações por preço (A6), ordem estável, paginação, texto com aspas, ponto,
/// <c>%</c>, <c>_</c> e <c>[</c> sem quebrar nem injetar, tempo limite com 503 e desempenho (NFR-04).
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SearchQueryTests
#pragma warning restore CA1515
{
    private const int Cars = 33;
    private const int Motorcycles = 36;
    private const int Trucks = 34;
    private const int Houses = 27;
    private const int Land = 30;
    private const int Services = 66;
    private const int General = 86;

    private static readonly DateTime Base = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
    private static int _users;
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static string _shared;
    private static Dictionary<string, int> _ids;

    private static async Task<int> AddUserAsync(string connection)
    {
        int n = Interlocked.Increment(ref _users);
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        AppUser user = new() { UserName = $"busca{n}@exemplo.com.br", NormalizedUserName = $"BUSCA{n}@EXEMPLO.COM.BR", Email = $"busca{n}@exemplo.com.br", FullName = $"Autora {n}" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    private static Ad Build(int author, string title, string description, int category, long? price, string city, string uf, byte status, DateTime when, Action<AdAttributes> attributes = null)
    {
        Ad ad = Ad.CreateDraft(title, author);
        ad.SetCategory(category);
        ad.SetText(title, description);
        ad.SetPrice(price);
        ad.SetLocation("13015100", city, uf, false);
        if (attributes is not null)
        {
            AdAttributes values = new();
            attributes(values);
            ad.SetAttributes(values);
        }

        switch (status)
        {
            case AdStatus.InReview:
                ad.ApplyTransition(AdStatus.InReview, author, when, null);
                break;
            case AdStatus.Published:
                ad.ApplyTransition(AdStatus.InReview, author, when, null);
                ad.ApplyTransition(AdStatus.Published, author, when, null);
                break;
            case AdStatus.Rejected:
                ad.ApplyTransition(AdStatus.InReview, author, when, null);
                ad.ApplyTransition(AdStatus.Rejected, author, when, "Fotos escuras");
                break;
            case AdStatus.Archived:
                ad.ApplyTransition(AdStatus.Archived, author, when, null);
                break;
        }

        return ad;
    }

    private static SearchReadRepository Repository(string connection) => new(new SqlConnection(connection));

    private static SearchCriteria Criteria(
        string[] words = null, int[] categories = null, string uf = null, string city = null, long? priceMin = null, long? priceMax = null, int? brand = null, int? model = null,
        int? yearFrom = null, int? yearTo = null, int? kmMax = null, decimal? areaMin = null, decimal? areaMax = null, SearchOrder order = SearchOrder.Recent, int page = 1, int pageSize = 24) =>
        new()
        {
            Words = words ?? [],
            CategoryIds = categories ?? [],
            Uf = uf,
            City = city,
            PriceMinCents = priceMin,
            PriceMaxCents = priceMax,
            BrandId = brand,
            ModelId = model,
            YearFrom = yearFrom,
            YearTo = yearTo,
            KmMax = kmMax,
            AreaMin = areaMin,
            AreaMax = areaMax,
            Order = order,
            Page = page,
            PageSize = pageSize
        };

    // Um conjunto de anúncios que serve a quase todos os testes (somente leitura): criado uma vez
    private static async Task<(string Connection, Dictionary<string, int> Ids)> SharedAsync()
    {
        await Gate.WaitAsync();
        try
        {
            if (_shared is null)
            {
                string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
                int author = await AddUserAsync(connection);
                await using (AppDbContext catalog = SqlServerFixture.NewContext(connection))
                {
                    catalog.VehicleBrands.AddRange(
                        new VehicleBrand { Id = 1, Kind = "car", Name = "Honda", Source = "teste" }, new VehicleBrand { Id = 2, Kind = "car", Name = "Toyota", Source = "teste" },
                        new VehicleBrand { Id = 1, Kind = "moto", Name = "Honda", Source = "teste" });
                    catalog.VehicleModels.AddRange(
                        new VehicleModel { Id = 10, Kind = "car", BrandId = 1, Name = "Fit", Source = "teste" }, new VehicleModel { Id = 11, Kind = "car", BrandId = 1, Name = "Civic", Source = "teste" },
                        new VehicleModel { Id = 12, Kind = "car", BrandId = 2, Name = "Corolla", Source = "teste" }, new VehicleModel { Id = 10, Kind = "moto", BrandId = 1, Name = "CG 160", Source = "teste" });
                    await catalog.SaveChangesAsync();
                }

                DateTime day(int n) => Base.AddDays(n);
                (string Key, Ad Ad)[] ads =
                [
                    ("civic", Build(author, "Honda Civic 2018", "Único dono, revisões feitas na concessionária.", Cars, 6_200_000, "Campinas", "SP", AdStatus.Published, day(2), a => a.Set("brandId", 1).Set("modelId", 11).Set("modelYear", 2018).Set("km", 45000))),
                    ("fit", Build(author, "Honda Fit 2015", "Ótimo estado, aceito troca.", Cars, 3_500_000, "Campinas", "SP", AdStatus.Published, day(3), a => a.Set("brandId", 1).Set("modelId", 10).Set("modelYear", 2015).Set("km", 90000))),
                    ("corolla", Build(author, "Toyota Corolla 2020", "Parece um civic de tão bem cuidado.", Cars, 9_000_000, "Curitiba", "PR", AdStatus.Published, day(4), a => a.Set("brandId", 2).Set("modelId", 12).Set("modelYear", 2020).Set("km", 20000))),
                    ("casa", Build(author, "Casa com quintal", "Quintal grande e churrasqueira.", Houses, 45_000_000, "Campinas", "SP", AdStatus.Published, day(5), a => a.Set("areaM2", 200m))),
                    ("terreno", Build(author, "Terreno plano", "Documentação em dia.", Land, 15_000_000, "Campinas", "SP", AdStatus.Published, day(6), a => a.Set("areaM2", 450m))),
                    ("sitio", Build(author, "Sítio \"Boa Vista\" & Cia", "Porteira fechada: 100% regularizado, lote_1 [bloco A].", Land, 30_000_000, "Atibaia", "SP", AdStatus.Published, day(7), a => a.Set("areaM2", 700m))),
                    ("pintura", Build(author, "Serviço de pintura", "Pintura residencial e comercial.", Services, null, "Campinas", "SP", AdStatus.Published, day(8))),
                    ("moto", Build(author, "Moto Honda CG 160", "Revisada.", Motorcycles, 1_200_000, "Campinas", "SP", AdStatus.Published, day(9), a => a.Set("brandId", 1).Set("modelId", 10).Set("modelYear", 2022).Set("km", 5000))),
                    ("livro", Build(author, "Ação e Reação", "Edição com capa dura, ótima conservação.", General, 5_000, "Curitiba", "PR", AdStatus.Published, day(10))),
                    ("caminhao", Build(author, "Caminhão Volvo", "Pronto para trabalhar.", Trucks, 25_000_000, "Curitiba", "PR", AdStatus.Published, day(11), a => a.Set("modelYear", 2014).Set("km", 300000))),
                    ("rascunho", Build(author, "Civic rascunho", "civic civic", Cars, 100, "Campinas", "SP", AdStatus.Draft, day(20))),
                    ("revisao", Build(author, "Civic em revisão", "civic", Cars, 100, "Campinas", "SP", AdStatus.InReview, day(21))),
                    ("rejeitado", Build(author, "Civic rejeitado", "civic", Cars, 100, "Campinas", "SP", AdStatus.Rejected, day(22))),
                    ("arquivado", Build(author, "Civic arquivado", "civic", Cars, 100, "Campinas", "SP", AdStatus.Archived, day(23)))
                ];
                await using (AppDbContext context = SqlServerFixture.NewContext(connection))
                {
                    context.Ads.AddRange(ads.Select(a => a.Ad));
                    await context.SaveChangesAsync();
                }

                _ids = ads.ToDictionary(a => a.Key, a => a.Ad.Id);
                _shared = connection;
            }

            return (_shared, _ids);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task<int[]> IdsAsync(string connection, SearchCriteria criteria) =>
        [.. (await Repository(connection).SearchAsync(criteria, CancellationToken.None)).Rows.Select(r => r.Id)];

    private static string[] Keys(Dictionary<string, int> ids, IEnumerable<int> found) => [.. found.Select(id => ids.Single(p => p.Value == id).Key)];

    private static string[] Keys(Dictionary<string, int> ids, int[] found) => [.. found.Select(id => ids.Single(p => p.Value == id).Key)];

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US002S01_SoAnunciosPublicados_RascunhoRevisaoRejeitadoEArquivadoNuncaAparecem()
    {
        (string connection, Dictionary<string, int> ids) = await SharedAsync();

        ShowcaseRows civic = await Repository(connection).SearchAsync(Criteria(words: ["civic"]), CancellationToken.None);
        ShowcaseRows all = await Repository(connection).SearchAsync(Criteria(), CancellationToken.None);

        CollectionAssert.AreEquivalent(new[] { "civic", "corolla" }, Keys(ids, civic.Rows.Select(r => r.Id)), "título (Civic) ou descrição (Corolla: \"parece um civic\"); os quatro Civic não publicados ficam de fora");
        Assert.AreEqual(2, civic.Total);
        Assert.AreEqual(10, all.Total, "dez publicados; os quatro de outras situações não contam");
        Assert.IsFalse(all.Rows.Any(r => new[] { ids["rascunho"], ids["revisao"], ids["rejeitado"], ids["arquivado"] }.Contains(r.Id)));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US002S01_TextoSemAcentoESemMaiusculas_NoTituloENaDescricao()
    {
        (string connection, Dictionary<string, int> ids) = await SharedAsync();

        // As colunas de busca são normalizadas pelo C#; o termo é normalizado igual: "ACAO", "acao", "ação" e "Ação" acham "Ação e Reação"
        foreach (string typed in new[] { "ACAO", "acao", "ação", "Ação", "REAÇÃO" })
        {
            int[] found = await IdsAsync(connection, Criteria(words: [GazetaMarketplace.Core.Search.Normalizer.Normalize(typed)]));
            // A palavra casa onde estiver: "acao" também está em "documentação" e "conservação"; "reacao" só no título do livro
            CollectionAssert.IsSubsetOf(new[] { ids["livro"] }, found, typed);
            if (typed.StartsWith("REA", StringComparison.Ordinal))
            {
                CollectionAssert.AreEqual(new[] { ids["livro"] }, found, typed);
            }
        }

        Assert.AreEqual("livro,terreno", string.Join(",", Keys(ids, await IdsAsync(connection, Criteria(words: ["acao"]))).Order()), "o texto casa em qualquer pedaço do título ou da descrição");

        // Só na descrição ("Ótimo estado" no Fit e "ótima conservação" no livro)
        CollectionAssert.AreEquivalent(new[] { "fit", "livro" }, Keys(ids, await IdsAsync(connection, Criteria(words: ["otim"]))));
        CollectionAssert.AreEqual(new int[0], await IdsAsync(connection, Criteria(words: ["xyzabc"])), "nenhum anúncio contém xyzabc (S07)");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US002S01_TodasAsPalavras_EmQualquerOrdem_NoTituloOuNaDescricao()
    {
        (string connection, Dictionary<string, int> ids) = await SharedAsync();

        CollectionAssert.AreEqual(new[] { ids["civic"] }, await IdsAsync(connection, Criteria(words: ["civic", "2018"])), "título com as duas palavras");
        CollectionAssert.AreEqual(new[] { ids["civic"] }, await IdsAsync(connection, Criteria(words: ["2018", "civic"])), "a ordem das palavras não importa");
        CollectionAssert.AreEqual(new[] { ids["corolla"] }, await IdsAsync(connection, Criteria(words: ["corolla", "cuidado"])), "uma no título e outra na descrição");
        CollectionAssert.AreEqual(new int[0], await IdsAsync(connection, Criteria(words: ["civic", "casa"])), "cada palavra é obrigatória");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US002S02_CategoriaComDescendentes_Uf_Cidade_EPreco_SeCombinam()
    {
        (string connection, Dictionary<string, int> ids) = await SharedAsync();

        int[] carsSpCampinas50k = await IdsAsync(connection, Criteria(categories: [Cars], uf: "SP", city: "Campinas", priceMax: 5_000_000));
        int[] priceBand = await IdsAsync(connection, Criteria(categories: [Cars], priceMin: 4_000_000, priceMax: 9_000_000));
        int[] curitiba = await IdsAsync(connection, Criteria(categories: [Cars], uf: "PR", city: "Curitiba"));
        int[] sp = await IdsAsync(connection, Criteria(uf: "SP"));

        CollectionAssert.AreEqual(new[] { ids["fit"] }, carsSpCampinas50k, "Carros em Campinas/SP até R$ 50.000: só o Fit (o Civic custa R$ 62.000)");
        CollectionAssert.AreEquivalent(new[] { "civic", "corolla" }, Keys(ids, priceBand), "faixa por mínimo e máximo, extremos inclusive");
        CollectionAssert.AreEqual(new[] { ids["corolla"] }, curitiba);
        Assert.IsFalse(sp.Contains(ids["corolla"]) || sp.Contains(ids["livro"]), "UF por igualdade");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US002S02_CategoriaPrincipal_IncluiAsSubcategorias_DeTodosOsNiveis()
    {
        (string connection, Dictionary<string, int> ids) = await SharedAsync();
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        var rows = await context.Categories.Select(c => new { c.Id, c.ParentId, c.Name }).ToListAsync();
        int automobiles = rows.Single(c => c.Name == "Automóveis, Peças e Acessórios").Id;
        int realEstate = rows.Single(c => c.Id == Houses).ParentId.Value;
        int[] DescendantsAndSelf(int root) => [root, .. rows.Where(c => c.ParentId == root).SelectMany(c => new[] { c.Id }.Concat(rows.Where(g => g.ParentId == c.Id).Select(g => g.Id)))];
        int[] automobileIds = DescendantsAndSelf(automobiles);
        int[] realEstateIds = DescendantsAndSelf(realEstate);

        string[] automobile = Keys(ids, await IdsAsync(connection, Criteria(categories: automobileIds)));
        string[] estate = Keys(ids, await IdsAsync(connection, Criteria(categories: realEstateIds)));

        CollectionAssert.AreEquivalent(new[] { "civic", "fit", "corolla", "moto", "caminhao" }, automobile);
        CollectionAssert.AreEquivalent(new[] { "casa", "terreno", "sitio" }, estate);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US002S03_MarcaModeloAnoEQuilometragem()
    {
        (string connection, Dictionary<string, int> ids) = await SharedAsync();

        string[] hondaCars = Keys(ids, await IdsAsync(connection, Criteria(categories: [Cars], brand: 1)));
        string[] hondaFiltered = Keys(ids, await IdsAsync(connection, Criteria(categories: [Cars], brand: 1, yearFrom: 2015, yearTo: 2020, kmMax: 100_000)));
        string[] civicOnly = Keys(ids, await IdsAsync(connection, Criteria(categories: [Cars], brand: 1, model: 11)));
        string[] tight = Keys(ids, await IdsAsync(connection, Criteria(categories: [Cars], brand: 1, yearFrom: 2016, kmMax: 50_000)));
        string[] trucks = Keys(ids, await IdsAsync(connection, Criteria(categories: [Trucks], yearFrom: 2010, yearTo: 2015, kmMax: 400_000)));

        CollectionAssert.AreEquivalent(new[] { "civic", "fit" }, hondaCars, "só Honda de carros (a Honda de motos é de outra categoria)");
        CollectionAssert.AreEquivalent(new[] { "civic", "fit" }, hondaFiltered, "2015 a 2020 e até 100.000 km, extremos inclusive");
        CollectionAssert.AreEqual(new[] { "civic" }, civicOnly);
        CollectionAssert.AreEqual(new[] { "civic" }, tight);
        CollectionAssert.AreEqual(new[] { "caminhao" }, trucks);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US002S04_AreaMinimaEMaxima_SoImoveisComArea()
    {
        (string connection, Dictionary<string, int> ids) = await SharedAsync();

        string[] between = Keys(ids, await IdsAsync(connection, Criteria(categories: [Land], areaMin: 300m, areaMax: 600m)));
        string[] atLeast = Keys(ids, await IdsAsync(connection, Criteria(categories: [Land], areaMin: 450m)));
        string[] edges = Keys(ids, await IdsAsync(connection, Criteria(categories: [Land], areaMin: 450m, areaMax: 450m)));
        string[] anyCategory = Keys(ids, await IdsAsync(connection, Criteria(areaMax: 250m)));

        CollectionAssert.AreEqual(new[] { "terreno" }, between);
        CollectionAssert.AreEquivalent(new[] { "terreno", "sitio" }, atLeast);
        CollectionAssert.AreEqual(new[] { "terreno" }, edges, "extremos inclusive");
        CollectionAssert.AreEqual(new[] { "casa" }, anyCategory, "anúncio sem área nunca passa numa faixa de área");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US002S05_Ordenar_PorDataMenorEMaiorPreco_ComDesempateEstavel()
    {
        (string connection, Dictionary<string, int> ids) = await SharedAsync();
        int[] cars = [Cars];

        string[] recent = Keys(ids, await IdsAsync(connection, Criteria(categories: cars)));
        string[] cheapest = Keys(ids, await IdsAsync(connection, Criteria(categories: cars, order: SearchOrder.PriceAscending)));
        string[] priciest = Keys(ids, await IdsAsync(connection, Criteria(categories: cars, order: SearchOrder.PriceDescending)));

        CollectionAssert.AreEqual(new[] { "corolla", "fit", "civic" }, recent, "mais recentes primeiro (data de publicação)");
        CollectionAssert.AreEqual(new[] { "fit", "civic", "corolla" }, cheapest);
        CollectionAssert.AreEqual(new[] { "corolla", "civic", "fit" }, priciest);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task A6_Servicos_ForaDaFaixaDePreco_ENoFimDasOrdenacoesPorPreco()
    {
        (string connection, Dictionary<string, int> ids) = await SharedAsync();

        string[] withBand = Keys(ids, await IdsAsync(connection, Criteria(priceMin: 1)));
        string[] ascending = Keys(ids, await IdsAsync(connection, Criteria(order: SearchOrder.PriceAscending)));
        string[] descending = Keys(ids, await IdsAsync(connection, Criteria(order: SearchOrder.PriceDescending)));
        string[] noBand = Keys(ids, await IdsAsync(connection, Criteria(categories: [Services])));

        CollectionAssert.DoesNotContain(withBand, "pintura", "com faixa de preço, o serviço sem preço sai");
        CollectionAssert.AreEqual(new[] { "pintura" }, noBand, "sem faixa, o serviço aparece");
        Assert.AreEqual("pintura", ascending[^1], "em Menor preço o serviço vai para o fim");
        Assert.AreEqual("pintura", descending[^1], "em Maior preço também");
        Assert.AreEqual("livro", ascending[0], "o mais barato com preço vem primeiro");
        Assert.AreEqual("casa", descending[0]);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US002S06_Paginar_ComTotalIgualEmTodasAsPaginas_EPaginaAlemDoFimVazia()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AddUserAsync(connection);
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            context.Ads.AddRange(Enumerable.Range(1, 30).Select(n => Build(author, $"Item {n:00}", null, General, 1000 + n, "Campinas", "SP", AdStatus.Published, Base.AddMinutes(n))));
            await context.SaveChangesAsync();
        }

        ShowcaseRows first = await Repository(connection).SearchAsync(Criteria(page: 1), CancellationToken.None);
        ShowcaseRows second = await Repository(connection).SearchAsync(Criteria(page: 2), CancellationToken.None);
        ShowcaseRows beyond = await Repository(connection).SearchAsync(Criteria(page: 3), CancellationToken.None);

        Assert.AreEqual(24, first.Rows.Count);
        Assert.AreEqual(6, second.Rows.Count);
        Assert.AreEqual(0, beyond.Rows.Count);
        Assert.AreEqual(30, first.Total);
        Assert.AreEqual(30, second.Total);
        Assert.AreEqual(30, beyond.Total, "o total continua certo numa página vazia");
        Assert.AreEqual(30, first.Rows.Concat(second.Rows).Select(r => r.Id).Distinct().Count(), "nenhum anúncio repetido nem perdido entre as páginas");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task OrdemEstavel_PrecosIguaisEPublicacaoIgual_DesempatamPeloIdMaisNovo_EmTodasAsPaginas()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AddUserAsync(connection);
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            context.Ads.AddRange(Enumerable.Range(1, 30).Select(n => Build(author, $"Igual {n:00}", null, General, 5000, "Campinas", "SP", AdStatus.Published, Base)));
            await context.SaveChangesAsync();
        }

        foreach (SearchOrder order in SearchOrders.All)
        {
            int[] firstPage = await IdsAsync(connection, Criteria(order: order, page: 1));
            int[] secondPage = await IdsAsync(connection, Criteria(order: order, page: 2));
            int[] all = [.. firstPage, .. secondPage];
            CollectionAssert.AreEqual(all.OrderByDescending(i => i).ToArray(), all, $"{order}: tudo igual, vale o id decrescente");
            Assert.AreEqual(30, all.Distinct().Count());
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task OrdenacaoForaDaLista_EIgnorada_OValorDoEnumNuncaViraTextoDoSql()
    {
        (string connection, Dictionary<string, int> ids) = await SharedAsync();

        // Um valor de enum que não existe cai na ordem padrão (mais recentes); nada de texto livre chega ao ORDER BY
        int[] weird = await IdsAsync(connection, Criteria(order: (SearchOrder)99, categories: [Cars]));
        int[] recent = await IdsAsync(connection, Criteria(categories: [Cars]));

        CollectionAssert.AreEqual(recent, weird);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task TermoComAspasPontoPorcentoSublinhadoEColchete_NaoQuebraNemInjeta()
    {
        (string connection, Dictionary<string, int> ids) = await SharedAsync();

        foreach ((string term, string expected) in new[]
        {
            ("100%", "sitio"),        // o % do termo é um caractere comum (não "qualquer coisa")
            ("lote_1", "sitio"),      // o _ é comum (não "um caractere qualquer")
            ("[bloco", "sitio"),      // o [ é comum (não abre um conjunto)
            ("a]", "sitio"),
            ("fechada:", "sitio"),    // pontuação
            ("boa", "sitio")
        })
        {
            CollectionAssert.AreEqual(new[] { ids[expected] }, await IdsAsync(connection, Criteria(words: [GazetaMarketplace.Core.Search.Normalizer.Normalize(term)])), term);
        }

        // "%" e "_" só casam onde existe um "%" ou um "_" de verdade (a descrição do sítio tem os dois); os outros curingas de LIKE não casam nada
        CollectionAssert.AreEqual(new[] { ids["sitio"] }, await IdsAsync(connection, Criteria(words: ["%"])));
        CollectionAssert.AreEqual(new[] { ids["sitio"] }, await IdsAsync(connection, Criteria(words: ["_"])));
        foreach (string wildcard in new[] { "[a-z]", "[^a]", "%civic%", "ci_ic", "c%c", "[c]ivic" })
        {
            Assert.AreEqual(0, (await IdsAsync(connection, Criteria(words: [wildcard]))).Length, wildcard);
        }

        // Tentativas de injeção: texto comum, sem resultado, sem erro e sem estrago
        foreach (string attack in new[] { "'; DROP TABLE Ads; --", "\" OR 1=1 --", "civic' OR '1'='1", "x'); DELETE FROM Ads; --", "\\", "'", "\"" })
        {
            Assert.AreEqual(0, (await IdsAsync(connection, Criteria(words: [attack, "zzz"]))).Length, attack);
            Assert.AreEqual(0, (await IdsAsync(connection, Criteria(uf: attack))).Length, attack);
            Assert.AreEqual(0, (await IdsAsync(connection, Criteria(city: attack))).Length, attack);
        }

        Assert.AreEqual(10, (await Repository(connection).SearchAsync(Criteria(), CancellationToken.None)).Total, "a tabela está intacta");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task LinhaDaBusca_NaoTemAutorNemEmail_SoOQueOCardPrecisa()
    {
        (string connection, Dictionary<string, int> ids) = await SharedAsync();

        ShowcaseRows result = await Repository(connection).SearchAsync(Criteria(words: ["civic", "2018"]), CancellationToken.None);

        string[] columns = [.. typeof(ShowcaseRow).GetProperties().Select(p => p.Name).Where(n => n != "EqualityContract").Order()];
        CollectionAssert.AreEqual(new[] { "Attributes", "CategoryId", "City", "CoverHeight", "CoverPhotoId", "CoverWidth", "Id", "PriceCents", "Title", "Uf" }, columns, "a linha não tem autor, e-mail nem outra coluna além das do card");
        Assert.AreEqual(1, result.Rows.Count);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Capa_UmaSoLinhaPorAnuncio_ComAFotoDeMenorPosicao()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AddUserAsync(connection);
        int id;
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            Ad ad = Build(author, "Com fotos", null, General, 1000, "Campinas", "SP", AdStatus.Published, Base);
            context.Ads.Add(ad);
            await context.SaveChangesAsync();
            id = ad.Id;
            foreach (int position in new[] { 2, 0, 0, 1 })
            {
                context.AdPhotos.Add(new AdPhoto { AdId = ad.Id, SortOrder = position, StorageKey = $"busca-{Guid.NewGuid():N}.webp", Width = 1600, Height = 1200, SizeBytes = 1000, CreatedAt = Base });
            }

            await context.SaveChangesAsync();
        }

        ShowcaseRows result = await Repository(connection).SearchAsync(Criteria(), CancellationToken.None);

        Assert.AreEqual(1, result.Rows.Count, "posições repetidas não duplicam o anúncio");
        Assert.AreEqual(1, result.Total);
        await using AppDbContext check = SqlServerFixture.NewContext(connection);
        int expected = await check.AdPhotos.Where(p => p.AdId == id && p.SortOrder == 0).OrderBy(p => p.Id).Select(p => p.Id).FirstAsync();
        Assert.AreEqual(expected, result.Rows[0].CoverPhotoId);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task PaginasInteiras_Visitante_BuscaComFiltrosNaTela_ComOEstadoVazio()
    {
        (string connection, Dictionary<string, int> ids) = await SharedAsync();
        using IntegrationWebFactory factory = new(connection);
        using HttpClient visitor = factory.CreateBrowser();

        string civic = WebUtility.HtmlDecode(await visitor.GetStringAsync("/busca?q=civic"));
        string combo = WebUtility.HtmlDecode(await visitor.GetStringAsync("/busca?categoria=cars&uf=SP&cidade=Campinas&precoMax=50000&ordem=menor-preco"));
        string honda = WebUtility.HtmlDecode(await visitor.GetStringAsync("/busca?categoria=cars&marca=1&anoDe=2015&anoAte=2020&kmMax=100000"));
        string empty = WebUtility.HtmlDecode(await visitor.GetStringAsync("/busca?q=xyzabc"));
        string inverted = WebUtility.HtmlDecode(await visitor.GetStringAsync("/busca?precoMin=5000&precoMax=1000"));

        StringAssert.Contains(civic, "2 anúncios encontrados");
        StringAssert.Contains(civic, "Honda Civic 2018");
        StringAssert.Contains(civic, "Toyota Corolla 2020");
        Assert.IsFalse(civic.Contains("Civic rascunho", StringComparison.Ordinal) || civic.Contains("Civic arquivado", StringComparison.Ordinal), "só publicados");
        StringAssert.Contains(combo, "1 anúncio encontrado");
        StringAssert.Contains(combo, "Honda Fit 2015");
        StringAssert.Contains(honda, "2 anúncios encontrados");
        StringAssert.Contains(empty, "Nenhum anúncio encontrado para esses filtros");
        StringAssert.Contains(empty, "Limpar filtros");
        StringAssert.Contains(inverted, "O preço mínimo não pode ser maior que o máximo");
        StringAssert.Contains(inverted, "10 anúncios encontrados", "a lista sai sem a faixa de preço");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ConsultaQueEstouraOTempo_Devolve503SemPilha_ComOsFiltrosPreservados()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        using HttpClient visitor = factory.CreateBrowser();
        Assert.AreEqual(HttpStatusCode.OK, (await visitor.GetAsync("/busca")).StatusCode, "esquenta as listas em cache antes de trancar a tabela");

        // Outra conexão segura a tabela inteira trancada; a leitura espera, passa dos 10 s do tempo limite e o site responde com o erro da tela
        await using SqlConnection blocker = new(connection);
        await blocker.OpenAsync();
        await using SqlTransaction transaction = blocker.BeginTransaction();
        await using (SqlCommand hold = new("SELECT COUNT(*) FROM Ads WITH (TABLOCKX, HOLDLOCK)", blocker, transaction))
        {
            await hold.ExecuteScalarAsync();
        }

        Stopwatch clock = Stopwatch.StartNew();
        HttpResponseMessage response = await visitor.GetAsync("/busca?q=civic&uf=SP");
        clock.Stop();
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        await transaction.RollbackAsync();

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.IsGreaterThanOrEqualTo(9, (int)clock.Elapsed.TotalSeconds, $"esperou o tempo limite de {AdCardTimeout} s (levou {clock.Elapsed.TotalSeconds:0.0} s)");
        Assert.IsLessThan(20, (int)clock.Elapsed.TotalSeconds);
        StringAssert.Contains(html, "Não foi possível buscar agora. Tente novamente.");
        StringAssert.Contains(html, "Tentar novamente");
        StringAssert.Matches(html, new Regex(@"<input[^>]*id=""busca-q""[^>]*value=""civic"""), "o texto continua na caixa");
        StringAssert.Matches(html, new Regex(@"<option value=""SP""[^>]*selected"), "a UF continua escolhida");
        foreach (string leak in new[] { "SqlException", "Timeout expired", "Microsoft.Data.SqlClient", "   at " })
        {
            Assert.IsFalse(html.Contains(leak, StringComparison.Ordinal), "sem detalhe técnico: " + leak);
        }

        Assert.AreEqual(HttpStatusCode.OK, (await visitor.GetAsync("/busca?q=civic")).StatusCode, "solto o bloqueio, a busca volta");
    }

    private const int AdCardTimeout = 10;

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Desempenho_Com200Anuncios_RespondeAbaixoDe500ms_NoP95()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AddUserAsync(connection);
        await SeedVolumeAsync(connection, author, 200);

        double p95 = await P95Async(connection, 60);

        Assert.IsLessThan(500, p95, $"p95 da busca com 200 anúncios: {p95:0.0} ms (NFR-04: abaixo de 500 ms)");
        LogPerformance(200, p95);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Desempenho_ComVolumeMaior_6000Anuncios_MedidaEPlano()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AddUserAsync(connection);
        await SeedVolumeAsync(connection, author, 6000);

        double p95 = await P95Async(connection, 40);
        LogPerformance(6000, p95);

        // Com 2.000 publicados (um terço) a varredura do texto ainda cabe com folga; o gatilho da ADR-006 (p95 acima de 500 ms ou 1.000 anúncios ativos) vale para revisar a decisão
        Assert.IsLessThan(1500, p95, $"p95 da busca com 6.000 anúncios (2.000 publicados): {p95:0.0} ms");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task PlanoDeExecucao_CategoriaEOrdemPorData_UsaOIndiceDeSituacaoECategoria_SemVarreduraDaTabela()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AddUserAsync(connection);
        await SeedVolumeAsync(connection, author, 6000);

        await Repository(connection).SearchAsync(Criteria(categories: [Cars], uf: "SP"), CancellationToken.None);
        string plan = await PlanAsync(connection, "%OUTER APPLY%a.CategoryId IN%a.Uf = @Uf%OFFSET%");

        Assert.IsFalse(string.IsNullOrEmpty(plan), "o plano da consulta foi encontrado");
        string operators = string.Join(" ", Regex.Matches(plan, @"PhysicalOp=""([^""]+)""[^>]*>(?:(?!</RelOp>)[\s\S])*?Index=""\[([^\]]+)\]""").Select(m => m.Groups[1].Value + ":" + m.Groups[2].Value));
        Assert.IsFalse(plan.Contains("PhysicalOp=\"Table Scan\"", StringComparison.Ordinal), "categoria + UF: sem varredura de tabela. " + operators);
        Assert.IsFalse(Regex.IsMatch(plan, @"PhysicalOp=""Clustered Index Scan""[^>]*>(?:(?!</RelOp>)[\s\S])*?Table=""\[Ads\]"""), "categoria + UF: sem varredura do índice agrupado de Ads. " + operators);
    }

    private static async Task SeedVolumeAsync(string connection, int author, int count)
    {
        string[] words = ["civic", "fit", "casa", "terreno", "moto", "livro", "pintura", "sitio", "corolla", "caminhao"];
        int[] categories = [Cars, Cars, Houses, Land, Motorcycles, General, Services, Land, Cars, Trucks];
        string[] cities = ["Campinas", "Curitiba", "Atibaia", "Santos"];
        string[] ufs = ["SP", "PR", "SP", "SP"];
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        context.Ads.AddRange(Enumerable.Range(1, count).Select(n =>
        {
            int w = n % words.Length;
            int c = n % cities.Length;
            return Build(author, $"{words[w]} modelo {n} anuncio", $"Descrição do {words[w]} número {n} com texto razoável para a busca varrer, bem conservado.", categories[w], categories[w] == Services ? null : 1_000_000 + n * 100L, cities[c], ufs[c],
                n % 3 == 0 ? AdStatus.Published : AdStatus.Draft, Base.AddMinutes(n));
        }));
        await context.SaveChangesAsync();
    }

    private static async Task<double> P95Async(string connection, int runs)
    {
        SearchCriteria[] shapes =
        [
            Criteria(words: ["civic"]),
            Criteria(words: ["modelo", "anuncio"], order: SearchOrder.PriceAscending),
            Criteria(categories: [Cars], uf: "SP", city: "Campinas", priceMax: 5_000_000),
            Criteria(words: ["conservado"], categories: [Cars, Land, Houses], order: SearchOrder.PriceDescending, page: 2)
        ];
        SearchReadRepository repository = Repository(connection);
        await repository.SearchAsync(shapes[0], CancellationToken.None); // esquenta o plano e a conexão
        List<double> times = [];
        for (int i = 0; i < runs; i++)
        {
            Stopwatch clock = Stopwatch.StartNew();
            await repository.SearchAsync(shapes[i % shapes.Length], CancellationToken.None);
            clock.Stop();
            times.Add(clock.Elapsed.TotalMilliseconds);
        }

        times.Sort();
        return times[(int)Math.Ceiling(times.Count * 0.95) - 1];
    }

    private static void LogPerformance(int ads, double p95)
    {
        if (Environment.GetEnvironmentVariable("GAZETA_PERF_LOG") is { Length: > 0 } path)
        {
            System.IO.File.AppendAllText(path, $"{DateTime.UtcNow:O} busca com {ads} anúncios cadastrados: p95 {p95:0.0} ms\n");
        }
    }

    private static async Task<string> PlanAsync(string connection, string textLike)
    {
        string sql = $@"
            SELECT TOP (1) CAST(p.query_plan AS NVARCHAR(MAX))
            FROM sys.dm_exec_query_stats s
            CROSS APPLY sys.dm_exec_sql_text(s.sql_handle) t
            CROSS APPLY sys.dm_exec_query_plan(s.plan_handle) p
            WHERE t.text LIKE '{textLike}' AND t.text NOT LIKE '%dm_exec_query_stats%' AND EXISTS (SELECT 1 FROM sys.dm_exec_plan_attributes(s.plan_handle) pa WHERE pa.attribute = 'dbid' AND pa.value = DB_ID())
            ORDER BY s.last_execution_time DESC";
        await using SqlConnection sqlConnection = new(connection);
        await sqlConnection.OpenAsync();
        await using SqlCommand command = new(sql, sqlConnection);
        return (await command.ExecuteScalarAsync()) as string ?? string.Empty;
    }
}
