using System;
using GazetaMarketplace.Core.Formatacao;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Formatacao;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DataTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void Utc_ExibidaEmSaoPaulo_ComoDdMmAaaa()
    {
        // 02:30 UTC de 03/10 é 23:30 de 02/10 em São Paulo (UTC-3)
        DateTime utc = new(2026, 10, 3, 2, 30, 0, DateTimeKind.Utc);

        Assert.AreEqual("02/10/2026", MoedaEDataFormatter.FormatarData(utc));
        Assert.AreEqual("02/10/2026 23:30", MoedaEDataFormatter.FormatarDataEHora(utc));
    }

    [TestMethod]
    public void DataSemKind_E_TratadaComoUtc()
    {
        DateTime semKind = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);

        Assert.AreEqual("15/01/2026 09:00", MoedaEDataFormatter.FormatarDataEHora(semKind));
    }

    [TestMethod]
    public void FusoDeExibicao_E_AmericaSaoPaulo()
    {
        Assert.AreEqual("America/Sao_Paulo", MoedaEDataFormatter.FusoDeExibicao.Id);
    }
}
