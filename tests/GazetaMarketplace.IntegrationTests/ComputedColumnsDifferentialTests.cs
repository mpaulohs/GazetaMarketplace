using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// Teste diferencial do ADR-002 (paridade de dupla implementação): os caminhos do JSON dos campos filtráveis existem em duas representações,
/// na definição do grupo (C#) e na expressão da coluna calculada (SQL). Aqui a aplicação grava um anúncio de cada grupo com filtro com valores
/// de todas as classes de entrada e confere que cada coluna devolve exatamente o que o C# lê do mesmo JSON.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed partial class ComputedColumnsDifferentialTests
#pragma warning restore CA1515
{
    /// <summary>Chave do JSON → coluna calculada. A segunda representação, escrita à mão de propósito.</summary>
    private static readonly Dictionary<string, string> ColumnByKey = new()
    {
        ["brandId"] = "VehicleBrandId",
        ["modelId"] = "VehicleModelId",
        ["modelYear"] = "ModelYear",
        ["km"] = "Km",
        ["areaM2"] = "AreaM2"
    };

    /// <summary>Uma classe de entrada: o JSON do valor, o que a coluna deve devolver e o que o C# lê do mesmo JSON.</summary>
    private sealed record Case(string Name, string Literal, string Column, string CSharp);

    private static readonly Case[] IntCases =
    [
        new("presente", "12345", "12345", "12345"),
        new("zero", "0", "0", "0"),
        new("máximo do int", "2147483647", "2147483647", "2147483647"),
        new("negativo", "-5", "-5", "-5"),
        new("texto que não é número", "\"abc\"", null, null),
        new("decimal em campo inteiro", "45000.5", null, null),
        new("estouro do int", "99999999999", null, null),
        new("nulo explícito", "null", null, null),
        new("objeto no lugar do número", "{\"x\":1}", null, null),
        new("lista no lugar do número", "[1,2]", null, null),
        new("booleano", "true", null, null)
    ];

    private static readonly Case[] DecimalCases =
    [
        new("presente", "450.75", "450.75", "450.75"),
        new("zero", "0", "0.00", "0"),
        new("mínimo", "0.01", "0.01", "0.01"),
        new("máximo", "99999999.99", "99999999.99", "99999999.99"),
        new("inteiro", "300", "300.00", "300"),
        new("texto que não é número", "\"abc\"", null, null),
        new("acima da precisão do banco", "999999999999", null, "999999999999"),
        new("nulo explícito", "null", null, null)
    ];

    [GeneratedRegex(@"'\$\.(\w+)'")]
    private static partial Regex JsonPath();

    private static IEnumerable<(string Key, FieldDefinition Field, int Category)> FilterableKeys() =>
        FieldGroupRegistry.All
            .SelectMany(g => g.Fields.Where(f => f.Filter != FieldFilter.None).Select(f => (g, f)))
            .SelectMany(x => CategoriesOf(x.g).Select(category => (x.f.Key, x.f, category)))
            .Where(x => x.f.AppliesTo(x.category));

    // Uma categoria de cada grupo com filtro (as que a carga inicial grava)
    private static IEnumerable<int> CategoriesOf(FieldGroup group) => group.Key switch
    {
        FieldGroupKeys.Cars => [33],
        FieldGroupKeys.Motorcycles => [36],
        FieldGroupKeys.TrucksAndBuses => [34, 35],
        FieldGroupKeys.RealEstate => [26, 27, 30, 31],
        _ => []
    };

    [TestMethod]
    [TestCategory("Integration")]
    public async Task OsCaminhosDasColunasCalculadas_SaoExatamenteOsCamposFiltraveisDosGrupos()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();

        List<string> definitions = await AdData.QueryAsync(connection, "SELECT name + '|' + definition FROM sys.computed_columns WHERE object_id = OBJECT_ID('Ads')");
        Dictionary<string, string> fromDatabase = definitions.ToDictionary(
            d => JsonPath().Match(d).Groups[1].Value, // chave no JSON (sensível a maiúsculas: o JSON_VALUE também é)
            d => d.Split('|')[0]);

        string[] filterable = [.. FilterableKeys().Select(x => x.Key).Distinct().OrderBy(k => k, System.StringComparer.Ordinal)];

        CollectionAssert.AreEqual(filterable, fromDatabase.Keys.OrderBy(k => k, System.StringComparer.Ordinal).ToArray(), "um filtro novo exige coluna calculada (e o contrário)");
        foreach ((string key, string column) in fromDatabase)
        {
            Assert.AreEqual(ColumnByKey[key], column, $"coluna do caminho $.{key}");
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CadaGrupoComFiltro_ClassesDeEntrada()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        int checkedCases = 0;

        foreach (IGrouping<int, (string Key, FieldDefinition Field, int Category)> byCategory in FilterableKeys().GroupBy(x => x.Category))
        {
            (string Key, FieldDefinition Field, int Category)[] keys = [.. byCategory];

            // Cada classe de entrada vale para todas as chaves filtráveis do grupo ao mesmo tempo, com o tipo certo de cada uma
            for (int i = 0; i < System.Math.Max(IntCases.Length, DecimalCases.Length); i++)
            {
                Dictionary<string, Case> chosen = keys.ToDictionary(k => k.Key, k => k.Field.Type == FieldType.Decimal
                    ? DecimalCases[i % DecimalCases.Length]
                    : IntCases[i % IntCases.Length]);

                string json = "{" + string.Join(",", chosen.Select(c => $"\"{c.Key}\":{c.Value.Literal}")) + ",\"outroCampo\":99}";
                int id = await AdData.AddDraftAsync(connection, author, byCategory.Key, json);
                Ad stored = await AdData.LoadAsync(connection, id);
                Assert.IsTrue(AdAttributes.TryParse(stored.Attributes, out AdAttributes attributes));

                foreach ((string key, Case c) in chosen)
                {
                    string column = ColumnByKey[key];
                    string actual = ColumnValue(stored, column);

                    Assert.AreEqual(c.Column, actual, $"categoria {byCategory.Key}, {key}, {c.Name}: valor da coluna {column}");
                    Assert.AreEqual(c.CSharp, ReadFromCSharp(attributes, key, keys.First(k => k.Key == key).Field.Type == FieldType.Decimal), $"categoria {byCategory.Key}, {key}, {c.Name}: leitura do C#");
                    checkedCases++;
                }
            }

            // Campo ausente: nenhuma coluna do grupo é preenchida
            int emptyId = await AdData.AddDraftAsync(connection, author, byCategory.Key, """{"outroCampo":1}""");
            Ad empty = await AdData.LoadAsync(connection, emptyId);
            foreach ((string key, _, _) in keys)
            {
                Assert.IsNull(ColumnValue(empty, ColumnByKey[key]), $"categoria {byCategory.Key}, {key}, ausente");
                checkedCases++;
            }
        }

        Assert.IsGreaterThan(100, checkedCases, "o teste realmente percorreu as classes de entrada");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task NumeroEntreAspas_NaColuna_EhNumero_MasOCSharpLeComoAusente_ValidarNoFormulario()
    {
        // Diferença conhecida e registrada (BACKLOG): JSON_VALUE devolve o texto de "12" e o TRY_CAST o converte; o C# estrito não.
        // A validação do formulário (3.3) precisa recusar texto em campo numérico para que isso nunca chegue ao banco.
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);

        int id = await AdData.AddDraftAsync(connection, author, 33, """{"km":"12","brandId":" 7 "}""");

        Ad stored = await AdData.LoadAsync(connection, id);
        Assert.AreEqual(12, stored.Km);
        Assert.IsTrue(AdAttributes.TryParse(stored.Attributes, out AdAttributes attributes));
        Assert.IsFalse(attributes.TryGetInt("km", out _));
        Assert.AreEqual(7, stored.VehicleBrandId, "o TRY_CAST ignora os espaços; também não deve chegar ao banco");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task OutrosGruposEChavesComOutraCaixa_NaoPreenchemColunasDeFiltro()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);

        int id = await AdData.AddDraftAsync(connection, author, 135, """{"conditionId":1,"productType":"Livro","KM":5,"Km":6,"brandid":9,"km2":7}""");

        Ad stored = await AdData.LoadAsync(connection, id);
        Assert.IsNull(stored.VehicleBrandId, "o caminho do JSON diferencia maiúsculas de minúsculas");
        Assert.IsNull(stored.VehicleModelId);
        Assert.IsNull(stored.ModelYear);
        Assert.IsNull(stored.Km, "'KM' e 'Km' não são $.km");
        Assert.IsNull(stored.AreaM2);
    }

    private static string ColumnValue(Ad ad, string column) => column switch
    {
        "VehicleBrandId" => ad.VehicleBrandId?.ToString(CultureInfo.InvariantCulture),
        "VehicleModelId" => ad.VehicleModelId?.ToString(CultureInfo.InvariantCulture),
        "ModelYear" => ad.ModelYear?.ToString(CultureInfo.InvariantCulture),
        "Km" => ad.Km?.ToString(CultureInfo.InvariantCulture),
        "AreaM2" => ad.AreaM2?.ToString(CultureInfo.InvariantCulture),
        _ => throw new System.ArgumentOutOfRangeException(nameof(column))
    };

    private static string ReadFromCSharp(AdAttributes attributes, string key, bool isDecimal)
    {
        if (isDecimal)
        {
            return attributes.TryGetDecimal(key, out decimal d) ? d.ToString(CultureInfo.InvariantCulture) : null;
        }

        return attributes.TryGetInt(key, out int i) ? i.ToString(CultureInfo.InvariantCulture) : null;
    }
}
