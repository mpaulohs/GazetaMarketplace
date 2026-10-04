using GazetaMarketplace.Core.Formatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Formatting;

/// <summary>
/// O único leitor de preço: o campo envia a notação brasileira e um número sem vírgula são reais (D1). O teste de "passagem" (handoff) é o que
/// liga o que a máscara do JavaScript entrega ao que o servidor lê.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PriceTextTests
#pragma warning restore CA1515
{
    [TestMethod]
    [DataRow("R$ 62.000,00", 6_200_000L)]
    [DataRow("62.000,00", 6_200_000L)]
    [DataRow("R$ 62.000,00", 6_200_000L)]
    [DataRow("62000", 6_200_000L)]
    [DataRow("62.000", 6_200_000L)]
    [DataRow("62000,5", 6_200_050L)]
    [DataRow("0,01", 1L)]
    [DataRow("59.000,00", 5_900_000L)]
    [DataRow("99.999.999,99", 9_999_999_999L)]
    [DataRow("1.250,75", 125_075L)]
    [DataRow("  1250 ", 125_000L)]
    [DataRow("0", 0L)]
    public void NotacaoBrasileira_ViraCentavos(string text, long expected)
    {
        Assert.AreEqual(PriceParse.Ok, PriceText.TryParse(text, out long cents));
        Assert.AreEqual(expected, cents);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    [DataRow("R$")]
    [DataRow("R$ ")]
    public void CampoVazio_ESemPreco(string text)
    {
        Assert.AreEqual(PriceParse.Empty, PriceText.TryParse(text, out _));
    }

    [TestMethod]
    [DataRow("abc")]
    [DataRow("12.5")]
    [DataRow("62.00")]
    [DataRow("1,2,3")]
    [DataRow("1.2.3")]
    [DataRow("62,000.00")]
    [DataRow("62000,123")]
    [DataRow("-5")]
    [DataRow("R$ R$ 5")]
    [DataRow("5,")]
    [DataRow(",50")]
    [DataRow("1e3")]
    public void TextoQueNaoEUmValorEmReais_ERecusado_SemAdivinhar(string text)
    {
        Assert.AreEqual(PriceParse.Invalid, PriceText.TryParse(text, out _), "'12.5' não vira R$ 125,00 em silêncio");
    }

    [TestMethod]
    public void NumeroGigante_NaoEstouraEFicaAcimaDoTeto()
    {
        Assert.AreEqual(PriceParse.Ok, PriceText.TryParse("99999999999999999999999", out long cents));
        Assert.AreEqual(long.MaxValue, cents, "o limite é conferido por quem chama, aqui só não pode dar exceção nem número negativo");
    }

    [TestMethod]
    [DataRow(6_200_000L, "62.000,00")]
    [DataRow(1L, "0,01")]
    [DataRow(125_075L, "1.250,75")]
    [DataRow(9_999_999_999L, "99.999.999,99")]
    public void Format_VoltaParaOCampoComDuasCasas(long cents, string expected)
    {
        Assert.AreEqual(expected, PriceText.Format(cents));
    }

    [TestMethod]
    [DataRow(1L)]
    [DataRow(99L)]
    [DataRow(6_200_000L)]
    [DataRow(9_999_999_999L)]
    public void FormatEDepoisTryParse_VoltaAoMesmoValor(long cents)
    {
        Assert.AreEqual(PriceParse.Ok, PriceText.TryParse(PriceText.Format(cents), out long back));
        Assert.AreEqual(cents, back);
    }

    /// <summary>
    /// A passagem: o que a máscara do navegador (<c>price.js</c>) deixa no campo é o que o servidor lê. As saídas da coluna do meio são conferidas
    /// no navegador por <c>PriceMaskTests</c> (E2E); aqui se confere que o servidor entende cada uma delas.
    /// </summary>
    [TestMethod]
    [DataRow("6200000", "62.000,00", 6_200_000L)]
    [DataRow("1", "0,01", 1L)]
    [DataRow("12", "0,12", 12L)]
    [DataRow("123", "1,23", 123L)]
    [DataRow("1234567", "12.345,67", 1_234_567L)]
    [DataRow("9999999999", "99.999.999,99", 9_999_999_999L)]
    [DataRow("0062", "0,62", 62L)]
    public void SaidaDaMascara_ELidaPeloServidorComOMesmoValor(string typedDigits, string maskOutput, long expected)
    {
        Assert.AreEqual(typedDigits.TrimStart('0').PadLeft(3, '0'), maskOutput.Replace(".", string.Empty, System.StringComparison.Ordinal).Replace(",", string.Empty, System.StringComparison.Ordinal).PadLeft(3, '0'), "a tabela é coerente");
        Assert.AreEqual(PriceParse.Ok, PriceText.TryParse(maskOutput, out long cents));
        Assert.AreEqual(expected, cents);
    }
}
