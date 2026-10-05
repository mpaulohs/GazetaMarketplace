using System.Globalization;
using GazetaMarketplace.Core.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Search;

/// <summary>
/// O número digitado num filtro (preço em reais, área em m²). A mesma tabela de entradas é conferida aqui (C#, <see cref="DecimalInput"/>) e no E2E (JavaScript, <c>lerNumero</c> do
/// <c>search.js</c>, que confere a faixa invertida antes de enviar): as duas implementações da regra precisam dar o mesmo resultado em cada linha (paridade de implementação dupla).
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DecimalInputTests
#pragma warning restore CA1515
{
    [TestMethod]
    [DataRow("50000", "50000")]
    [DataRow("50.000", "50000")]
    [DataRow("50.000,00", "50000")]
    [DataRow("50000,5", "50000.5")]
    [DataRow("50000.50", "50000.5")]
    [DataRow("R$ 1.234,56", "1234.56")]
    [DataRow("R$1.234,56", "1234.56")]
    [DataRow("r$ 5", "5")]
    [DataRow("0", "0")]
    [DataRow("0,01", "0.01")]
    [DataRow("007", "7")]
    [DataRow("1.234", "1234")]
    [DataRow("1.23", "1.23")]
    [DataRow("12.345.678,9", "12345678.9")]
    [DataRow(" 42 ", "42")]
    [DataRow("\t42\n", "42")]
    [DataRow("99999999,99", "99999999.99")]
    [DataRow("", null)]
    [DataRow("   ", null)]
    [DataRow("R$", null)]
    [DataRow("abc", null)]
    [DataRow("-5", null)]
    [DataRow("+5", null)]
    [DataRow("1,2,3", null)]
    [DataRow("50.00.0", null)]
    [DataRow("50000,123", null)]
    [DataRow("1e3", null)]
    [DataRow("5 000", null)]
    [DataRow("1.2345", null)]
    [DataRow(".5", null)]
    [DataRow("5.", null)]
    [DataRow(",5", null)]
    [DataRow("1.000.00", null)]
    [DataRow("12,3.4", null)]
    [DataRow("1,234.56", null)]
    [DataRow("٣٠", null)]
    [DataRow("5\u00a0000", null)]
    [DataRow("5,", null)]
    [DataRow("1..000", null)]
    [DataRow("1.000,", null)]
    public void TabelaDeParidade_CadaEntradaDaOMesmoValorQueOLerNumeroDoJavaScript(string typed, string expected)
    {
        bool read = DecimalInput.TryParse(typed, out decimal value);

        Assert.AreEqual(expected is not null, read, $"\"{typed}\"");
        if (expected is not null)
        {
            Assert.AreEqual(decimal.Parse(expected, CultureInfo.InvariantCulture), value, $"\"{typed}\"");
        }
    }

    [TestMethod]
    [DataRow("50000", 5_000_000L)]
    [DataRow("50000,5", 5_000_050L)]
    [DataRow("0,01", 1L)]
    [DataRow("99999999,99", 9_999_999_999L)]
    public void Centavos_SaoOValorEmReaisVezesCem(string typed, long cents)
    {
        Assert.IsTrue(DecimalInput.TryParseCents(typed, 9_999_999_999L, out long result));
        Assert.AreEqual(cents, result);
    }

    [TestMethod]
    [DataRow("100000000")]
    [DataRow("99999999,995")]
    [DataRow("abc")]
    public void Centavos_AcimaDoTetoOuIlegivel_Recusa(string typed)
    {
        Assert.IsFalse(DecimalInput.TryParseCents(typed, 9_999_999_999L, out _));
    }
}
