using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using GazetaMarketplace.Core.Showcase;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Favorites;

/// <summary>
/// Paridade da regra de ids dos favoritos (US-005): o <c>favorites.js</c> (<c>limpar</c>) e o <see cref="FavoriteIds"/> existem em duas linguagens e rodam sobre <b>a mesma tabela</b>
/// (<c>favorite-ids-parity.json</c>, também lida pelo E2E, que roda o JavaScript no navegador e confere a resposta do servidor). Aqui é o lado C#: cada entrada da tabela tem de dar o resultado
/// que a tabela diz, e o JavaScript tem de dar o mesmo no E2E; se uma das pontas mudar a regra, a outra fica vermelha.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class FavoriteIdsParityTests
#pragma warning restore CA1515
{
    private static JsonElement Table() => JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Favorites", "favorite-ids-parity.json"))).RootElement.Clone();

    [TestMethod]
    public void ATabela_TemPeloMenos40Entradas_ComNomesUnicos()
    {
        JsonElement table = Table();
        string[] names = [.. table.GetProperty("values").EnumerateArray().Concat(table.GetProperty("lists").EnumerateArray()).Select(e => e.GetProperty("name").GetString())];

        Assert.IsTrue(names.Length >= 40, "entradas: " + names.Length);
        Assert.AreEqual(names.Length, names.Distinct().Count(), "nomes únicos");
    }

    [TestMethod]
    public void Valores_ComTexto_OFavoriteIdsDaOMesmoVeredictoQueATabela()
    {
        foreach (JsonElement entry in Table().GetProperty("values").EnumerateArray())
        {
            if (entry.GetProperty("token").ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            string token = entry.GetProperty("token").GetString();
            bool valid = entry.GetProperty("valid").GetBoolean();

            bool actual = FavoriteIds.TryParse(token, out int[] ids);

            Assert.AreEqual(valid, actual, $"{entry.GetProperty("name").GetString()} (token \"{token}\")");
            if (valid)
            {
                CollectionAssert.AreEqual(new[] { int.Parse(token, System.Globalization.CultureInfo.InvariantCulture) }, ids, entry.GetProperty("name").GetString());
            }
            else
            {
                Assert.AreEqual(0, ids.Length, entry.GetProperty("name").GetString());
            }
        }
    }

    [TestMethod]
    public void Listas_ComEndereco_OFavoriteIdsDaOsMesmosIdsNaMesmaOrdemQueATabela()
    {
        int checkedLists = 0;
        foreach (JsonElement entry in Table().GetProperty("lists").EnumerateArray())
        {
            if (entry.GetProperty("query").ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            int[] expected = [.. entry.GetProperty("ids").EnumerateArray().Select(i => i.GetInt32())];
            string query = entry.GetProperty("query").GetString();

            bool actual = FavoriteIds.TryParse(query, out int[] ids);

            Assert.AreEqual(expected.Length > 0, actual, $"{entry.GetProperty("name").GetString()} (\"{query}\")");
            CollectionAssert.AreEqual(expected, ids, entry.GetProperty("name").GetString());
            checkedLists++;
        }

        Assert.IsTrue(checkedLists >= 9, "listas conferidas: " + checkedLists);
    }
}
