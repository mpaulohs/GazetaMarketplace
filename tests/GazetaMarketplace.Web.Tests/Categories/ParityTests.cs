using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Infrastructure.Data.Seeds;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Categories;

/// <summary>
/// A árvore inicial existe em duas representações: <c>specs/categories.md</c> (a fonte do Product Owner) e <c>InitialCategories</c> (o que a
/// migration grava). Este teste lê o arquivo e confere linha a linha (<c>rules/testing.md</c>, paridade de dupla implementação).
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ParityTests
#pragma warning restore CA1515
{
    private sealed record SourceRow(int Id, string Name, int? ParentId, int DisplayOrder, bool IsPostable, string ExplicitSlug);

    /// <summary>
    /// O grupo de campos que cada categoria define, lido da tabela do Apêndice B da SPEC (escrito aqui à mão, de propósito: é a segunda
    /// representação). Cada tarefa 2.x acrescentou os seus grupos nesta lista junto com a migration de dados; a 2.4 fechou os 18 do Apêndice B.
    /// </summary>
    private static readonly Dictionary<int, string> ExpectedFieldGroups = new()
    {
        [3] = "Parts",
        [26] = "RealEstate",
        [27] = "RealEstate",
        [30] = "RealEstate",
        [31] = "RealEstate",
        [33] = "Cars",
        [34] = "TrucksAndBuses",
        [35] = "TrucksAndBuses",
        [36] = "Motorcycles",
        [37] = "BoatsAndAircraft",
        [66] = "Services",
        [96] = "Jobs",
        [28] = "RoomRental",
        [29] = "Seasonal",
        [43] = "Phones",
        [44] = "TelephonyProducts",
        [45] = "TelephonyProducts",
        [46] = "Smartwatches",
        [47] = "TelephonyProducts",
        [48] = "TelephonyProducts",
        [64] = "ClothingAndShoes",
        [65] = "ClothingAndShoes",
        [68] = "ClothingAndShoes",
        [69] = "ClothingAndShoes",
        [72] = "ClothingAndShoes",
        [75] = "ClothingAndShoes",
        [76] = "ClothingAndShoes",
        [77] = "ClothingAndShoes",
        [89] = "Machinery",
        [92] = "Machinery",
        [93] = "Machinery",
        [97] = "Machinery",
        [102] = "ElectronicsAndComputers",
        [103] = "ElectronicsAndComputers",
        [104] = "ElectronicsAndComputers",
        [105] = "ElectronicsAndComputers",
        [106] = "ElectronicsAndComputers",
        [107] = "ElectronicsAndComputers",
        [108] = "ElectronicsAndComputers",
        [109] = "ElectronicsAndComputers",
        [110] = "ElectronicsAndComputers",
        [111] = "ElectronicsAndComputers",
        [112] = "ElectronicsAndComputers",
        [113] = "ElectronicsAndComputers",
        [114] = "ElectronicsAndComputers",
        [115] = "ElectronicsAndComputers",
        [116] = "ElectronicsAndComputers",
        [117] = "ElectronicsAndComputers",
        [118] = "ElectronicsAndComputers",
        [119] = "ElectronicsAndComputers",
        [120] = "ElectronicsAndComputers",
        [121] = "ElectronicsAndComputers",
        [122] = "ElectronicsAndComputers",
        [123] = "ElectronicsAndComputers",
        [124] = "ElectronicsAndComputers",
        [125] = "ElectronicsAndComputers",
        [126] = "ElectronicsAndComputers",
        [127] = "ElectronicsAndComputers",
        [128] = "Appliances",
        [129] = "Appliances",
        [130] = "Appliances",
        [131] = "Appliances",
        [132] = "Appliances",
        [133] = "Appliances",
        [134] = "Appliances"
    };

    private static List<SourceRow> ReadSource()
    {
        List<SourceRow> rows = [];
        foreach (string line in File.ReadAllLines(RepositoryHelper.Project("specs/categories.md")))
        {
            string[] cells = [.. line.Trim().Trim('|').Split('|').Select(c => c.Trim())];
            if (cells.Length == 6 && int.TryParse(cells[0], out int id))
            {
                rows.Add(new SourceRow(
                    id,
                    cells[1],
                    cells[2] == "NULL" ? null : int.Parse(cells[2], System.Globalization.CultureInfo.InvariantCulture),
                    int.Parse(cells[3], System.Globalization.CultureInfo.InvariantCulture),
                    cells[4] == "1",
                    cells[5] == "NULL" ? null : cells[5]));
            }
        }

        return rows;
    }

    private static List<SourceRow> LoadedFromSource() =>
        [.. ReadSource().Where(r => !InitialCategories.ExcludedFromV1.Contains(r.Id))];

    [TestMethod]
    public void ArquivoDeOrigem_TemAsLinhasQueSePrometem()
    {
        List<SourceRow> source = ReadSource();

        Assert.HasCount(152, source);
        Assert.AreEqual(129, source.Count(r => r.IsPostable));
        Assert.AreEqual(155, source.Max(r => r.Id));
        CollectionAssert.AreEquivalent(new[] { 24, 25, 32 }, Enumerable.Range(1, 155).Except(source.Select(r => r.Id)).ToArray(), "os buracos de ids continuam os mesmos");
    }

    [TestMethod]
    public void Carga_TemTodasAsLinhasDoArquivo_MenosOsAnimaisVivos_ComOsMesmosValores()
    {
        List<SourceRow> expected = LoadedFromSource();
        IReadOnlyList<Core.Categories.Category> seed = InitialCategories.All;

        Assert.HasCount(147, seed);
        CollectionAssert.AreEqual(expected.Select(r => r.Id).OrderBy(i => i).ToArray(), seed.Select(c => c.Id).OrderBy(i => i).ToArray());
        foreach (SourceRow row in expected)
        {
            Core.Categories.Category loaded = seed.Single(c => c.Id == row.Id);
            Assert.AreEqual(row.Name, loaded.Name, $"nome da categoria {row.Id}");
            Assert.AreEqual(row.ParentId, loaded.ParentId, $"pai da categoria {row.Id}");
            Assert.AreEqual(row.DisplayOrder, loaded.DisplayOrder, $"ordem da categoria {row.Id}");
            Assert.AreEqual(row.IsPostable, loaded.IsPostable, $"IsPostable da categoria {row.Id}");
            Assert.IsTrue(loaded.IsSystem, $"IsSystem da categoria {row.Id}");
            Assert.AreEqual(ExpectedFieldGroups.GetValueOrDefault(row.Id), loaded.FieldGroup, $"FieldGroup da categoria {row.Id}");
        }
    }

    [TestMethod]
    public void Carga_NaoTemOsCincoAnimaisVivos_MasTemAcessoriosParaPets()
    {
        HashSet<int> ids = [.. InitialCategories.All.Select(c => c.Id)];

        foreach (int animal in new[] { 79, 80, 82, 83, 91 })
        {
            Assert.DoesNotContain(animal, ids, $"categoria {animal} (animal vivo) fica fora da v1");
        }

        Assert.Contains(81, ids, "Acessórios para pets continua");
    }

    [TestMethod]
    public void Slugs_SaoOsExplicitosDoArquivo_ENosDemais_OsGeradosPelaRegra()
    {
        List<SourceRow> source = LoadedFromSource();
        IReadOnlyDictionary<int, string> expected = SlugGenerator.AssignInitial(source.Select(r => new SlugCandidate(r.Id, r.Name, r.IsPostable, r.ExplicitSlug)));

        foreach (Core.Categories.Category loaded in InitialCategories.All)
        {
            Assert.AreEqual(expected[loaded.Id], loaded.Slug, $"slug da categoria {loaded.Id} ({loaded.Name})");
        }

        Assert.AreEqual(29, source.Count(r => r.ExplicitSlug is not null), "29 slugs explícitos no arquivo");
        foreach (SourceRow explicitRow in source.Where(r => r.ExplicitSlug is not null))
        {
            Assert.AreEqual(explicitRow.ExplicitSlug, InitialCategories.All.Single(c => c.Id == explicitRow.Id).Slug, "slug explícito vale como está");
        }

        Assert.AreEqual("cars", InitialCategories.All.Single(c => c.Id == 33).Slug);
    }

    [TestMethod]
    public void Slugs_QuatroCasosDeNomeRepetido_PostavelGanhaOLimpo()
    {
        string Slug(int id) => InitialCategories.All.Single(c => c.Id == id).Slug;

        Assert.AreEqual("servicos-grupo", Slug(7));
        Assert.AreEqual("servicos", Slug(66));
        Assert.AreEqual("vagas-de-emprego-grupo", Slug(13));
        Assert.AreEqual("vagas-de-emprego", Slug(96));
    }

    [TestMethod]
    public void Slugs_SaoUnicosNoSiteInteiro_ENuncaVazios()
    {
        string[] slugs = [.. InitialCategories.All.Select(c => c.Slug)];

        Assert.AreEqual(slugs.Length, slugs.Distinct(StringComparer.Ordinal).Count());
        Assert.IsTrue(slugs.All(s => s.Length is > 0 and <= SlugGenerator.MaxLength));
        Assert.IsTrue(slugs.All(s => s.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')), "só letras minúsculas, números e hífen");
    }

    [TestMethod]
    public void AutopecasEAAgrupamentoDoTerceiroNivel_Nao_Postavel_ComCincoFilhasPostaveis()
    {
        Core.Categories.Category autoparts = InitialCategories.All.Single(c => c.Id == 3);
        Core.Categories.Category[] children = [.. InitialCategories.All.Where(c => c.ParentId == 3)];

        Assert.AreEqual("Autopeças", autoparts.Name);
        Assert.AreEqual(2, autoparts.ParentId);
        Assert.IsFalse(autoparts.IsPostable);
        CollectionAssert.AreEquivalent(new[] { 38, 39, 40, 41, 42 }, children.Select(c => c.Id).ToArray());
        Assert.IsTrue(children.All(c => c.IsPostable));
    }

    [TestMethod]
    public void Carga_22MaesMaisAutopecasMais124Postaveis()
    {
        IReadOnlyList<Core.Categories.Category> seed = InitialCategories.All;

        Assert.AreEqual(22, seed.Count(c => c.ParentId is null));
        Assert.IsTrue(seed.Where(c => c.ParentId is null).All(c => !c.IsPostable), "as de primeiro nível só agrupam");
        Assert.AreEqual(124, seed.Count(c => c.IsPostable));
        Assert.AreEqual(1, seed.Count(c => c.ParentId is not null && !c.IsPostable));
    }
}
