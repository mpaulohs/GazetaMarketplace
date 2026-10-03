using System;
using GazetaMarketplace.Core.Formatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Formatting;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DateTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void Utc_ExibidaEmSaoPaulo_ComoDdMmAaaa()
    {
        // 02:30 UTC de 03/10 é 23:30 de 02/10 em São Paulo (UTC-3)
        DateTime utc = new(2026, 10, 3, 2, 30, 0, DateTimeKind.Utc);

        Assert.AreEqual("02/10/2026", CurrencyAndDateFormatter.FormatDate(utc));
        Assert.AreEqual("02/10/2026 23:30", CurrencyAndDateFormatter.FormatDateTime(utc));
    }

    [TestMethod]
    public void DataSemKind_E_TratadaComoUtc()
    {
        DateTime withoutKind = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Unspecified);

        Assert.AreEqual("15/01/2026 09:00", CurrencyAndDateFormatter.FormatDateTime(withoutKind));
    }

    [TestMethod]
    public void FusoDeExibicao_E_AmericaSaoPaulo()
    {
        Assert.AreEqual("America/Sao_Paulo", CurrencyAndDateFormatter.DisplayTimeZone.Id);
    }
}
