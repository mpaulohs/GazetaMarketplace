using System;
using System.Globalization;
using System.Linq;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Fields;

/// <summary>Ano de fabricação das Máquinas: de 1950 até o ano atual, sem ano futuro (diferente do ano do modelo de veículos, que aceita o seguinte).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ManufactureYearTests
#pragma warning restore CA1515
{
    private static FakeClock ClockAt(string utcIso) => new() { Now = DateTimeOffset.Parse(utcIso, CultureInfo.InvariantCulture) };

    [TestMethod]
    public void Lista_Em2026_VaiDe2026Ate1950_SemAnoFuturo()
    {
        FieldList years = FieldLists.ManufactureYears(2026);

        Assert.AreEqual("ManufactureYear", years.Name);
        Assert.AreEqual(2026, years.Options[0].Id, "o ano atual, não o seguinte");
        Assert.AreEqual(1950, years.Options[^1].Id);
        Assert.AreEqual("1950", years.Options[^1].Label, "sem o 'ou anterior' do ano do modelo");
        Assert.HasCount(2026 - 1950 + 1, years.Options);
        CollectionAssert.AreEqual(Enumerable.Range(0, years.Options.Count).Select(i => 2026 - i).ToArray(), years.Options.Select(o => o.Id).ToArray(), "decrescente, sem buracos");
        Assert.IsFalse(years.Contains(2027));
    }

    [TestMethod]
    public void Lista_NaViradaDeAno_GanhaUmAnoNoTopo()
    {
        Assert.AreEqual(2027, FieldLists.ManufactureYears(2027).Options[0].Id);
        Assert.AreEqual(FieldLists.ManufactureYears(2026).Options.Count + 1, FieldLists.ManufactureYears(2027).Options.Count);
    }

    [TestMethod]
    public void Validacao_AceitaDe1950AoAnoAtual_RecusaOAnteriorEOFuturo()
    {
        FakeClock clock = ClockAt("2026-10-03T12:00:00Z");

        Assert.IsTrue(ManufactureYearRules.IsValid(1950, clock));
        Assert.IsTrue(ManufactureYearRules.IsValid(2026, clock));
        Assert.IsFalse(ManufactureYearRules.IsValid(1949, clock));
        Assert.IsFalse(ManufactureYearRules.IsValid(2027, clock), "máquina fabricada não é do futuro (o ano do modelo de veículos aceitaria 2027)");
        Assert.IsTrue(ModelYearRules.IsValid(2027, clock), "contraste: veículos aceitam o ano seguinte");
    }

    [TestMethod]
    public void Validacao_UsaOAnoDeSaoPaulo_NaViradaDeAno()
    {
        // 31/12/2026 23:59 em São Paulo ainda é 2026, embora já seja 2027 em UTC
        FakeClock beforeMidnight = ClockAt("2027-01-01T02:59:59Z");
        FakeClock afterMidnight = ClockAt("2027-01-01T03:00:00Z");

        Assert.IsFalse(ManufactureYearRules.IsValid(2027, beforeMidnight));
        Assert.IsTrue(ManufactureYearRules.IsValid(2027, afterMidnight));
        Assert.IsFalse(ManufactureYearRules.IsValid(2028, afterMidnight));
    }

    [TestMethod]
    public void Maquinas_AnoDeFabricacao_EhCampoProprio_SemListaFixaNemObrigatoriedade()
    {
        FieldDefinition year = FieldGroupRegistry.Get(FieldGroupKeys.Machinery)!.Field("manufactureYear")!;

        Assert.AreEqual(FieldType.ManufactureYear, year.Type);
        Assert.IsFalse(year.Required);
        Assert.IsNull(year.Options, "a lista muda com o ano; sai de FieldLists.ManufactureYears");
    }
}
