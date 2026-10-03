using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Fields;

/// <summary>
/// As listas de opções existem em duas representações: nos documentos (<c>gazetaonline-lookups.md</c> e o Apêndice B da SPEC) e em
/// <see cref="FieldLists"/>. Estes testes relêem os documentos e conferem cada opção (paridade de dupla implementação).
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed partial class ListsTests
#pragma warning restore CA1515
{
    private static readonly string[] FromGazetaOnline =
    [
        nameof(FieldLists.ProductCondition),
        nameof(FieldLists.ApartmentType),
        nameof(FieldLists.HouseType),
        nameof(FieldLists.LandType),
        nameof(FieldLists.CommercialType),
        nameof(FieldLists.PropertyTransactionType),
        nameof(FieldLists.ApartmentFeature),
        nameof(FieldLists.LandFeature),
        nameof(FieldLists.CommercialFeature),
        nameof(FieldLists.ApartmentCondoFeature),
        nameof(FieldLists.HouseCondoFeature),
        nameof(FieldLists.CarFuel),
        nameof(FieldLists.CarColor),
        nameof(FieldLists.CarTransmission),
        nameof(FieldLists.CarSteeringGear),
        nameof(FieldLists.VehicleSteeringGear),
        nameof(FieldLists.CarDoor),
        nameof(FieldLists.CarEnginePower),
        nameof(FieldLists.VehicleType),
        nameof(FieldLists.TruckType),
        nameof(FieldLists.BusType),
        nameof(FieldLists.BoatType),
        nameof(FieldLists.MotorcycleDisplacement),
        nameof(FieldLists.OptionalItem),
        nameof(FieldLists.TruckOptionalItem),
        nameof(FieldLists.BusOptionalItem),
        nameof(FieldLists.MotorcycleOptionalItem),
        nameof(FieldLists.AdditionalInfo),
        nameof(FieldLists.VehicleAdditionalInfo),
        nameof(FieldLists.MotorcycleAdditionalInfo),
        nameof(FieldLists.BoatAdditionalInfo),
        nameof(FieldLists.PartCondition),
        nameof(FieldLists.PartColor),
        nameof(FieldLists.AutoPartType),
        nameof(FieldLists.MotorcyclePartType),
        nameof(FieldLists.BoatPartType),
        nameof(FieldLists.SeasonalType),
        nameof(FieldLists.SeasonalFeature),
        nameof(FieldLists.RoomFeature),
        nameof(FieldLists.SeasonalPaymentType),
        nameof(FieldLists.PhoneStorage),
        nameof(FieldLists.PhoneColor),
        nameof(FieldLists.PhoneBrand),
        nameof(FieldLists.SmartwatchBrand),
        nameof(FieldLists.PhoneBatteryHealth),
        nameof(FieldLists.PhoneAccessoryType),
        nameof(FieldLists.PhonePartType),
        nameof(FieldLists.SmartwatchAccessoryType),
        nameof(FieldLists.LandlinePhoneType),
        nameof(FieldLists.AirConditionerCapacity),
        nameof(FieldLists.AirConditionerType),
        nameof(FieldLists.FanType),
        nameof(FieldLists.RefrigeratorType),
        nameof(FieldLists.StoveType),
        nameof(FieldLists.WasherType),
        nameof(FieldLists.KitchenApplianceType),
        nameof(FieldLists.PersonalCareApplianceType),
        nameof(FieldLists.ApplianceVoltage)
    ];

    private static readonly string[] FromSpec =
    [
        nameof(FieldLists.ServiceType),
        nameof(FieldLists.JobArea),
        nameof(FieldLists.ClothingSize),
        nameof(FieldLists.AdultShoeSize),
        nameof(FieldLists.ChildShoeSize),
        nameof(FieldLists.Gender)
    ];

    [GeneratedRegex(@"^\| (\d+) \| (.+?) \|\s*$", RegexOptions.Multiline)]
    private static partial Regex RowPattern();

    [GeneratedRegex(@"^(\d+) opções: (.+)$", RegexOptions.Multiline)]
    private static partial Regex InlinePattern();

    private static FieldList Implemented(string name) =>
        (FieldList)typeof(FieldLists).GetField(name, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

    private static List<(int Id, string Label)> ReadLookup(string name)
    {
        string text = File.ReadAllText(RepositoryHelper.Project("specs/discovery/gazetaonline-lookups.md"));
        Match section = Regex.Match(text, $@"### `{name}`[^\n]*\n(.*?)(?=\n### |\n## |\Z)", RegexOptions.Singleline);
        Assert.IsTrue(section.Success, $"lista {name} ausente de gazetaonline-lookups.md");

        // Formato 1: tabela "| Id | Opção |". Formato 2: uma linha "N opções: rótulo (id) · rótulo (id) · ..."
        List<(int, string)> rows = [.. RowPattern().Matches(section.Groups[1].Value).Select(m => (int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), m.Groups[2].Value.Trim()))];
        if (rows.Count > 0)
        {
            return rows;
        }

        Match inline = InlinePattern().Match(section.Groups[1].Value);
        Assert.IsTrue(inline.Success, $"lista {name} não está em tabela nem em linha única");
        List<(int, string)> parsed = [];
        foreach (string part in inline.Groups[2].Value.Split(" · "))
        {
            Match item = Regex.Match(part.Trim(), @"^(.*) \((\d+)\)$");
            Assert.IsTrue(item.Success, $"opção ilegível em {name}: {part}");
            parsed.Add((int.Parse(item.Groups[2].Value, CultureInfo.InvariantCulture), item.Groups[1].Value.Trim()));
        }

        Assert.AreEqual(int.Parse(inline.Groups[1].Value, CultureInfo.InvariantCulture), parsed.Count, $"a contagem declarada de {name} não bate com as opções");
        return parsed;
    }

    /// <summary>As opções de uma linha da tabela do Apêndice B da SPEC: "a · b · c (nota)".</summary>
    private static List<string> ReadSpecOptions(string rowStart)
    {
        string spec = File.ReadAllText(RepositoryHelper.Project("specs/SPEC.md"));
        int start = spec.IndexOf(rowStart, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"linha '{rowStart}' ausente da SPEC");
        string line = spec[start..].Split('\n')[0];
        string cell = line.Trim().Trim('|').Split('|').Select(c => c.Trim()).Last();
        cell = Regex.Replace(cell, @"\s*\([^)]*\)\s*$", string.Empty);
        return [.. cell.Split(" · ").Select(o => o.Trim())];
    }

    [TestMethod]
    public void IdsDasListas_SaoOsDoGazetaOnline()
    {
        foreach (string name in FromGazetaOnline)
        {
            List<(int Id, string Label)> expected = ReadLookup(name);
            FieldList implemented = Implemented(name);

            Assert.AreEqual(name, implemented.Name);
            CollectionAssert.AreEqual(expected.Select(e => e.Id).ToArray(), implemented.Options.Select(o => o.Id).ToArray(), $"ids de {name}");
            CollectionAssert.AreEqual(expected.Select(e => e.Label).ToArray(), implemented.Options.Select(o => o.Label).ToArray(), $"rótulos de {name}");
        }
    }

    [TestMethod]
    public void Servicos_Tipo_TemAs11OpcoesNaOrdemDaSpec_ComIds1A11()
    {
        List<string> expected = ReadSpecOptions("| 3 | Tipo | Lista");

        Assert.HasCount(11, expected);
        CollectionAssert.AreEqual(expected, FieldLists.ServiceType.Options.Select(o => o.Label).ToList());
        CollectionAssert.AreEqual(Enumerable.Range(1, 11).ToArray(), FieldLists.ServiceType.Options.Select(o => o.Id).ToArray());
        Assert.AreEqual("Serviços domésticos", FieldLists.ServiceType.Options[0].Label);
        Assert.AreEqual("Turismo", FieldLists.ServiceType.Options[^1].Label);
    }

    [TestMethod]
    public void Vagas_Area_Tem14OpcoesNaOrdemDaSpec_ComIds1A14()
    {
        List<string> expected = ReadSpecOptions("| 2 | Área | Várias opções");

        Assert.HasCount(14, expected);
        CollectionAssert.AreEqual(expected, FieldLists.JobArea.Options.Select(o => o.Label).ToList());
        CollectionAssert.AreEqual(Enumerable.Range(1, 14).ToArray(), FieldLists.JobArea.Options.Select(o => o.Id).ToArray());
    }

    [TestMethod]
    public void TodaListaDeFieldLists_TemUmaFonteConferidaAqui()
    {
        string[] declared = [.. typeof(FieldLists).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(FieldList)).Select(f => f.Name)];

        CollectionAssert.AreEquivalent(FromGazetaOnline.Concat(FromSpec).ToArray(), declared, "lista nova exige conferência contra a sua fonte");
    }

    [TestMethod]
    public void Listas_TemIdsUnicos_RotulosPreenchidosESemEspacosSobrando()
    {
        foreach (string name in FromGazetaOnline.Concat(FromSpec))
        {
            FieldList list = Implemented(name);
            Assert.AreEqual(list.Options.Count, list.Options.Select(o => o.Id).Distinct().Count(), $"ids repetidos em {name}");
            Assert.IsTrue(list.Options.All(o => !string.IsNullOrWhiteSpace(o.Label) && o.Label == o.Label.Trim()), $"rótulo ruim em {name}");
            Assert.IsTrue(list.Options.All(o => o.Id > 0));
        }
    }

    [TestMethod]
    public void ProdutosEmGeral_Condicao_Tem5Opcoes()
    {
        CollectionAssert.AreEqual(
            new[] { "Novo", "Usado - Excelente", "Usado - Bom", "Recondicionado", "Com defeito ou avarias" },
            FieldLists.ProductCondition.Options.Select(o => o.Label).ToArray());
    }

    [TestMethod]
    public void ListasEmLinhaUnica_TemAContagemDeclarada()
    {
        Assert.HasCount(39, FieldLists.MotorcycleDisplacement.Options);
        Assert.HasCount(20, FieldLists.OptionalItem.Options);
        Assert.HasCount(13, FieldLists.CarEnginePower.Options);
        Assert.AreEqual("Acima de 1.000", FieldLists.MotorcycleDisplacement.Find(39)!.Label);
        Assert.AreEqual("4.0 ou mais", FieldLists.CarEnginePower.Find(13)!.Label);
    }

    [TestMethod]
    public void FieldList_BuscaPorId()
    {
        Assert.AreEqual("Loft", FieldLists.ApartmentType.Find(5).Label);
        Assert.IsNull(FieldLists.ApartmentType.Find(99));
        Assert.IsTrue(FieldLists.ApartmentType.Contains(1));
        Assert.IsFalse(FieldLists.ApartmentType.Contains(0));
    }

    [TestMethod]
    public void Tamanhos_Roupas_PPaXGG_CalcadoAdulto34a45_CalcadoInfantil16a33_IdEhANumeracao()
    {
        CollectionAssert.AreEqual(new[] { "PP", "P", "M", "G", "GG", "XG", "XGG" }, FieldLists.ClothingSize.Options.Select(o => o.Label).ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(1, 7).ToArray(), FieldLists.ClothingSize.Options.Select(o => o.Id).ToArray());

        CollectionAssert.AreEqual(Enumerable.Range(34, 12).ToArray(), FieldLists.AdultShoeSize.Options.Select(o => o.Id).ToArray());
        Assert.IsTrue(FieldLists.AdultShoeSize.Options.All(o => o.Label == o.Id.ToString(CultureInfo.InvariantCulture)));
        Assert.IsFalse(FieldLists.AdultShoeSize.Contains(33));
        Assert.IsFalse(FieldLists.AdultShoeSize.Contains(46));

        CollectionAssert.AreEqual(Enumerable.Range(16, 18).ToArray(), FieldLists.ChildShoeSize.Options.Select(o => o.Id).ToArray());
        Assert.IsTrue(FieldLists.ChildShoeSize.Options.All(o => o.Label == o.Id.ToString(CultureInfo.InvariantCulture)));
        Assert.IsFalse(FieldLists.ChildShoeSize.Contains(15));
        Assert.IsFalse(FieldLists.ChildShoeSize.Contains(34));
    }

    [TestMethod]
    public void Genero_Tem4Opcoes_NaOrdemDaDecisao()
    {
        CollectionAssert.AreEqual(new[] { "Masculino", "Feminino", "Unissex", "Infantil" }, FieldLists.Gender.Options.Select(o => o.Label).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, FieldLists.Gender.Options.Select(o => o.Id).ToArray());
    }
}
