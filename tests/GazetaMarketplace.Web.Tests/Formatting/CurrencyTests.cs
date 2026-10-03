using GazetaMarketplace.Core.Formatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Formatting;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CurrencyTests
#pragma warning restore CA1515
{
    // U+00A0 entre "R$" e o número: o valor não quebra de linha
    [TestMethod]
    [DataRow(123450L, "R$ 1.234,50")]
    [DataRow(123400L, "R$ 1.234")]
    [DataRow(5L, "R$ 0,05")]
    [DataRow(100L, "R$ 1")]
    [DataRow(0L, "R$ 0")]
    [DataRow(1000000L, "R$ 10.000")]
    [DataRow(999999999999L, "R$ 9.999.999.999,99")]
    public void Centavos_SoQuandoNaoSaoZero(long cents, string expected)
    {
        Assert.AreEqual(expected, CurrencyAndDateFormatter.FormatCurrency(cents));
    }

    [TestMethod]
    public void Formatacao_NaoDependeDaCulturaDaThread()
    {
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            Assert.AreEqual("R$ 1.234,50", CurrencyAndDateFormatter.FormatCurrency(123450));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}
