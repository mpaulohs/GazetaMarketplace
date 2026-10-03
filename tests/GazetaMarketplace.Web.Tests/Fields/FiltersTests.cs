using System.Linq;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Infrastructure.Data.Seeds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Fields;

/// <summary>Os filtros específicos da busca vêm do grupo da categoria escolhida (A7 c).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class FiltersTests
#pragma warning restore CA1515
{
    private static CategoryTreeSnapshot Tree() => CategoryTreeSnapshot.Build(
        InitialCategories.All.Select(c => new CategoryRow(c.Id, c.ParentId, c.Name, c.Slug, c.DisplayOrder, c.IsPostable, c.FieldGroup, c.IsSystem)));

    private static string[] FiltersOf(CategoryTreeSnapshot tree, int categoryId) =>
        [.. FieldGroupRegistry.Resolve(tree, categoryId)!.FilterableFieldsFor(categoryId).Select(f => f.Key)];

    [TestMethod]
    public void FiltrosEspecificos_VemDoGrupo()
    {
        CategoryTreeSnapshot tree = Tree();

        CollectionAssert.AreEqual(new[] { "brandId", "modelId", "modelYear", "km" }, FiltersOf(tree, 33), "Carros: marca, modelo, ano e km");
        CollectionAssert.AreEqual(new[] { "brandId", "modelId", "modelYear", "km" }, FiltersOf(tree, 36), "Motos: marca, modelo, ano e km");
        CollectionAssert.AreEqual(new[] { "modelYear", "km" }, FiltersOf(tree, 34), "Caminhões: ano e km");
        CollectionAssert.AreEqual(new[] { "modelYear", "km" }, FiltersOf(tree, 35), "Ônibus: ano e km");
        CollectionAssert.AreEqual(new[] { "areaM2" }, FiltersOf(tree, 26), "Imóveis: área");
        CollectionAssert.AreEqual(new[] { "areaM2" }, FiltersOf(tree, 30), "Terrenos: área");
    }

    [TestMethod]
    public void Barcos_Pecas_Servicos_Vagas_EProdutosEmGeral_NaoTemFiltroEspecifico()
    {
        CategoryTreeSnapshot tree = Tree();

        foreach (int id in new[] { 37, 38, 39, 40, 41, 42, 66, 96, 135, 117, 43, 68 })
        {
            Assert.IsEmpty(FiltersOf(tree, id), $"categoria {id}");
        }
    }

    [TestMethod]
    public void OsFiltros_SaoExatamenteOsDasColunasCalculadasDoAdr002()
    {
        // ADR-002: VehicleBrandId, VehicleModelId, ModelYear, Km e AreaM2 são as únicas colunas calculadas (as únicas com filtro)
        string[] filterable = [.. FieldGroupRegistry.All.SelectMany(g => g.Fields).Where(f => f.Filter != FieldFilter.None).Select(f => f.Key).Distinct().OrderBy(k => k)];

        CollectionAssert.AreEqual(new[] { "areaM2", "brandId", "km", "modelId", "modelYear" }, filterable);
    }

    [TestMethod]
    public void MarcaEModelo_FiltramPorIgualdade_AnoKmEAreaPorFaixa()
    {
        FieldGroup cars = FieldGroupRegistry.Get(FieldGroupKeys.Cars)!;

        Assert.AreEqual(FieldFilter.Exact, cars.Field("brandId")!.Filter);
        Assert.AreEqual(FieldFilter.Exact, cars.Field("modelId")!.Filter);
        Assert.AreEqual(FieldFilter.Range, cars.Field("modelYear")!.Filter);
        Assert.AreEqual(FieldFilter.Range, cars.Field("km")!.Filter);
        Assert.AreEqual(FieldFilter.None, cars.Field("versionId")!.Filter, "a versão não filtra");
        Assert.AreEqual(FieldFilter.Range, FieldGroupRegistry.Get(FieldGroupKeys.RealEstate)!.Field("areaM2")!.Filter);
    }

    [TestMethod]
    public void ChavesDosCamposComColunaCalculada_SaoAsMesmasEntreOsGrupos()
    {
        // modelYear existe em Carros, Motos, Caminhões e ônibus e Barcos: a mesma chave serve à mesma coluna calculada
        string[] withYear = [.. FieldGroupRegistry.All.Where(g => g.Field("modelYear") is not null).Select(g => g.Key).OrderBy(k => k)];

        CollectionAssert.AreEqual(new[] { "BoatsAndAircraft", "Cars", "Motorcycles", "TrucksAndBuses" }, withYear);
        string[] withKm = [.. FieldGroupRegistry.All.Where(g => g.Field("km") is not null).Select(g => g.Key).OrderBy(k => k)];
        CollectionAssert.AreEqual(new[] { "Cars", "Motorcycles", "TrucksAndBuses" }, withKm);
    }
}
