using System;
using System.Linq;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Fields;

/// <summary>Ano do modelo: de 1950 ("1950 ou anterior") ao ano atual + 1, pelo relógio do site no fuso de São Paulo.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ModelYearTests
#pragma warning restore CA1515
{
    private static FakeClock ClockAt(string utcIso) => new() { Now = DateTimeOffset.Parse(utcIso, System.Globalization.CultureInfo.InvariantCulture) };

    [TestMethod]
    public void Lista_Em2026_VaiDe2027Ate1951_Mais1950OuAnterior()
    {
        FieldList years = FieldLists.ModelYears(2026);

        Assert.AreEqual("VehicleModelYear", years.Name);
        Assert.AreEqual(2027, years.Options[0].Id, "ano atual + 1");
        Assert.AreEqual("2027", years.Options[0].Label);
        Assert.AreEqual(1950, years.Options[^1].Id, "o id de '1950 ou anterior' é 1950");
        Assert.AreEqual("1950 ou anterior", years.Options[^1].Label);
        Assert.AreEqual(1951, years.Options[^2].Id);
        Assert.HasCount((2027 - 1951 + 1) + 1, years.Options);
        CollectionAssert.AreEqual(Enumerable.Range(0, years.Options.Count - 1).Select(i => 2027 - i).ToArray(), years.Options.Take(years.Options.Count - 1).Select(o => o.Id).ToArray(), "decrescente, sem buracos");
        Assert.IsTrue(years.Options.Take(years.Options.Count - 1).All(o => o.Label == o.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)), "o rótulo é o próprio ano");
    }

    [TestMethod]
    public void Lista_NaViradaDeAno_GanhaUmAnoNoTopo()
    {
        Assert.AreEqual(2027, FieldLists.ModelYears(2026).Options[0].Id);
        Assert.AreEqual(2028, FieldLists.ModelYears(2027).Options[0].Id);
        Assert.AreEqual(FieldLists.ModelYears(2026).Options.Count + 1, FieldLists.ModelYears(2027).Options.Count);
    }

    [TestMethod]
    public void AnoAtual_VemDoFusoDeSaoPaulo_NaoDoUtc()
    {
        // 31/12/2026 23:59:59 em São Paulo (UTC-3) ainda é 2026, mesmo já sendo 2027 em UTC
        Assert.AreEqual(2026, ModelYearRules.CurrentYear(ClockAt("2027-01-01T02:59:59Z")));
        // 01/01/2027 00:00:00 em São Paulo é 2027
        Assert.AreEqual(2027, ModelYearRules.CurrentYear(ClockAt("2027-01-01T03:00:00Z")));
        Assert.AreEqual(2026, ModelYearRules.CurrentYear(ClockAt("2026-10-03T12:00:00Z")));
    }

    [TestMethod]
    public void Validacao_AceitaDe1950AoAnoAtualMais1_UsandoORelogioDoSite()
    {
        FakeClock lastSecondOf2026 = ClockAt("2027-01-01T02:59:59Z");

        Assert.IsTrue(ModelYearRules.IsValid(1950, lastSecondOf2026));
        Assert.IsTrue(ModelYearRules.IsValid(2027, lastSecondOf2026), "2026 + 1");
        Assert.IsFalse(ModelYearRules.IsValid(2028, lastSecondOf2026), "ainda não");
        Assert.IsFalse(ModelYearRules.IsValid(1949, lastSecondOf2026));

        lastSecondOf2026.Now = ClockAt("2027-01-01T03:00:00Z").Now; // virou o ano em São Paulo
        Assert.IsTrue(ModelYearRules.IsValid(2028, lastSecondOf2026), "na virada, 2027 + 1");
        Assert.IsFalse(ModelYearRules.IsValid(2029, lastSecondOf2026));
    }

    [TestMethod]
    public void CamposDeAnoDoModelo_SaoDoTipoModelYear_ComPisoDe1950()
    {
        foreach (string key in new[] { FieldGroupKeys.TrucksAndBuses, FieldGroupKeys.BoatsAndAircraft })
        {
            FieldDefinition year = FieldGroupRegistry.Get(key)!.Field("modelYear")!;
            Assert.AreEqual(FieldType.ModelYear, year.Type, key);
            Assert.IsTrue(year.Required, key);
            Assert.AreEqual(1950m, year.Min, key);
            Assert.IsNull(year.Options, "a lista é gerada pelo ano atual, não fixa");
        }
    }
}
