using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Infrastructure.Data.Seeds;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Fields;

/// <summary>
/// O Apêndice B da SPEC (tabela "Grupo | Categorias | Campos específicos") contra o código: o conjunto de categorias de cada um dos 18 grupos e,
/// nos nove grupos da tarefa 2.4, os rótulos, a ordem e a obrigatoriedade dos campos (paridade de dupla implementação).
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed partial class AppendixBTests
#pragma warning restore CA1515
{
    private sealed record SpecRow(string Group, string Key, string Categories, string Fields);

    private static readonly Dictionary<string, string> KeyBySpecName = new()
    {
        ["Carros"] = FieldGroupKeys.Cars,
        ["Motos"] = FieldGroupKeys.Motorcycles,
        ["Caminhões, Ônibus"] = FieldGroupKeys.TrucksAndBuses,
        ["Barcos e aeronaves"] = FieldGroupKeys.BoatsAndAircraft,
        ["Peças"] = FieldGroupKeys.Parts,
        ["Imóveis"] = FieldGroupKeys.RealEstate,
        ["Aluguel de quartos"] = FieldGroupKeys.RoomRental,
        ["Temporada"] = FieldGroupKeys.Seasonal,
        ["Celulares"] = FieldGroupKeys.Phones,
        ["Smartwatches"] = FieldGroupKeys.Smartwatches,
        ["Produtos de telefonia"] = FieldGroupKeys.TelephonyProducts,
        ["Eletro"] = FieldGroupKeys.Appliances,
        ["Eletrônicos e informática"] = FieldGroupKeys.ElectronicsAndComputers,
        ["Roupas e calçados"] = FieldGroupKeys.ClothingAndShoes,
        ["Máquinas"] = FieldGroupKeys.Machinery,
        ["Serviços"] = FieldGroupKeys.Services,
        ["Vagas de emprego"] = FieldGroupKeys.Jobs,
        ["Produtos em geral"] = FieldGroupKeys.GeneralProducts
    };

    private static readonly string[] NewGroups =
    [
        FieldGroupKeys.RoomRental, FieldGroupKeys.Seasonal, FieldGroupKeys.Phones, FieldGroupKeys.Smartwatches, FieldGroupKeys.TelephonyProducts,
        FieldGroupKeys.Appliances, FieldGroupKeys.ElectronicsAndComputers, FieldGroupKeys.ClothingAndShoes, FieldGroupKeys.Machinery
    ];

    [GeneratedRegex(@"^\| ([^|]+) \| ([^|]+) \| ([^|]+) \|\s*$", RegexOptions.Multiline)]
    private static partial Regex TableRow();

    private static List<SpecRow> ReadAppendix()
    {
        string spec = File.ReadAllText(RepositoryHelper.Project("specs/SPEC.md"));
        int start = spec.IndexOf("### B. Categorias e campos por categoria", System.StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, "Apêndice B ausente da SPEC");
        int end = spec.IndexOf("As listas de opções das 29 categorias", start, System.StringComparison.Ordinal);
        string section = spec[start..end];

        List<SpecRow> rows = [];
        foreach (Match m in TableRow().Matches(section))
        {
            string name = m.Groups[1].Value.Trim();
            if (KeyBySpecName.TryGetValue(name, out string key))
            {
                rows.Add(new SpecRow(name, key, m.Groups[2].Value.Trim(), m.Groups[3].Value.Trim()));
            }
        }

        return rows;
    }

    /// <summary>"33", "34, 35", "38–42", "128–134", "as demais categorias ativas" (vazio).</summary>
    private static List<int> ParseCategories(string cell)
    {
        List<int> ids = [];
        foreach (string part in cell.Split(','))
        {
            Match range = Regex.Match(part.Trim(), @"^(\d+)[–-](\d+)$");
            if (range.Success)
            {
                int from = int.Parse(range.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                int to = int.Parse(range.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
                ids.AddRange(Enumerable.Range(from, to - from + 1));
            }
            else if (int.TryParse(part.Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int single))
            {
                ids.Add(single);
            }
        }

        return ids;
    }

    private static CategoryTreeSnapshot SeedTree() =>
        CategoryTreeSnapshot.Build([.. InitialCategories.All.Select(c => new CategoryRow(c.Id, c.ParentId, c.Name, c.Slug, c.DisplayOrder, c.IsPostable, c.FieldGroup, c.IsSystem))]);

    [TestMethod]
    public void Apendice_Tem18Grupos_TodosNoRegistro()
    {
        List<SpecRow> rows = ReadAppendix();

        Assert.HasCount(18, rows);
        CollectionAssert.AreEquivalent(rows.Select(r => r.Key).ToArray(), FieldGroupRegistry.All.Select(g => g.Key).ToArray());
    }

    [TestMethod]
    public void CadaCategoriaPostavel_ResolveParaOGrupoDoApendice_ETodasAs124TemUm()
    {
        CategoryTreeSnapshot tree = SeedTree();
        List<SpecRow> rows = ReadAppendix();
        Dictionary<int, string> expected = [];
        foreach (SpecRow row in rows.Where(r => r.Key != FieldGroupKeys.GeneralProducts))
        {
            foreach (int id in ParseCategories(row.Categories))
            {
                Assert.IsTrue(expected.TryAdd(id, row.Key), $"categoria {id} aparece em dois grupos do Apêndice B");
            }
        }

        CategoryNode[] postable = [.. tree.All.Where(n => n.IsPostable)];

        Assert.HasCount(124, postable);
        foreach (CategoryNode node in postable)
        {
            string wanted = expected.GetValueOrDefault(node.Id, FieldGroupKeys.GeneralProducts);
            Assert.AreEqual(wanted, FieldGroupRegistry.Resolve(tree, node.Id).Key, $"grupo da categoria {node.Id} ({node.Name})");
        }

        Assert.IsEmpty(expected.Keys.Except(postable.Select(n => n.Id)), "toda categoria citada no Apêndice B existe e é postável");
    }

    [TestMethod]
    public void ConjuntoDeCategoriasDeCadaGrupo_EhOMesmoDoApendice()
    {
        CategoryTreeSnapshot tree = SeedTree();
        int[] postable = [.. tree.All.Where(n => n.IsPostable).Select(n => n.Id)];

        foreach (SpecRow row in ReadAppendix().Where(r => r.Key != FieldGroupKeys.GeneralProducts))
        {
            int[] actual = [.. postable.Where(id => FieldGroupRegistry.Resolve(tree, id).Key == row.Key).OrderBy(id => id)];
            CollectionAssert.AreEqual(ParseCategories(row.Categories).OrderBy(id => id).ToArray(), actual, $"categorias do grupo {row.Group}");
        }

        int general = postable.Count(id => FieldGroupRegistry.Resolve(tree, id).Key == FieldGroupKeys.GeneralProducts);
        Assert.AreEqual(124 - ReadAppendix().Where(r => r.Key != FieldGroupKeys.GeneralProducts).Sum(r => ParseCategories(r.Categories).Count), general, "as demais caem em Produtos em geral");
    }

    [TestMethod]
    public void NoveGruposDa2_4_TemOsCamposNaOrdemEComAObrigatoriedadeDoApendice()
    {
        foreach (SpecRow row in ReadAppendix().Where(r => NewGroups.Contains(r.Key)))
        {
            FieldGroup group = FieldGroupRegistry.Get(row.Key)!;
            string[] cells = [.. row.Fields.Split(" · ")];

            string[] expectedLabels = [.. cells.Select(c => Regex.Replace(c.Replace("✅", string.Empty), @"\s*\([^)]*\)\s*$", string.Empty).Trim())];
            bool[] expectedRequired = [.. cells.Select(c => c.Contains('✅'))];

            CollectionAssert.AreEqual(expectedLabels, group.Fields.Select(f => f.Label).ToArray(), $"rótulos e ordem de {row.Group}");
            CollectionAssert.AreEqual(expectedRequired, group.Fields.Select(f => f.Required).ToArray(), $"obrigatórios de {row.Group}");
        }
    }

    [TestMethod]
    public void CampoComRestricaoDeCategoria_NoApendice_SoApareceNasCategoriasCitadas()
    {
        // "(44, 45)", "(128)": a nota entre parênteses limita o campo às categorias citadas
        foreach (SpecRow row in ReadAppendix().Where(r => NewGroups.Contains(r.Key)))
        {
            FieldGroup group = FieldGroupRegistry.Get(row.Key)!;
            string[] cells = [.. row.Fields.Split(" · ")];
            for (int i = 0; i < cells.Length; i++)
            {
                Match note = Regex.Match(cells[i], @"\(([\d, ]+)\)\s*$");
                int[] groupCategories = [.. ParseCategories(row.Categories)];
                int[] applies = note.Success ? [.. ParseCategories(note.Groups[1].Value)] : groupCategories;

                foreach (int id in groupCategories)
                {
                    Assert.AreEqual(applies.Contains(id), group.Fields[i].AppliesTo(id), $"{row.Group}: '{group.Fields[i].Label}' na categoria {id}");
                }
            }
        }
    }
}
