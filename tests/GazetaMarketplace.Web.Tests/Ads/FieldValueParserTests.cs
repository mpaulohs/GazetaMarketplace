using System;
using System.Collections.Generic;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Fields;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>
/// Leitura estrita dos campos do grupo (D8): número é número, nunca texto. O SQL Server converte "12" entre aspas em número, o C# não — então o
/// formulário nunca pode gravar um valor com o tipo trocado (BACKLOG da 3.1).
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class FieldValueParserTests
#pragma warning restore CA1515
{
    private const int CurrentYear = 2026;
    private const int Cars = 33;

    private static FieldDefinition Km => FieldGroupRegistry.Get(FieldGroupKeys.Cars).Field("km");

    private static string Apply(FieldDefinition field, AdAttributes target, params string[] values) =>
        FieldValueParser.Apply(field, Cars, values, target, CurrentYear);

    [TestMethod]
    [DataRow("45000", 45000)]
    [DataRow("45.000", 45000)]
    [DataRow("0", 0)]
    [DataRow("9.999.999", 9_999_999)]
    [DataRow("  1200  ", 1200)]
    public void Inteiro_AceitaMilharComPontoENadaMais(string text, int expected)
    {
        AdAttributes attributes = new();

        Assert.IsNull(Apply(Km, attributes, text));

        Assert.IsTrue(attributes.TryGetInt("km", out int value));
        Assert.AreEqual(expected, value);
    }

    [TestMethod]
    [DataRow("muito")]
    [DataRow("45k")]
    [DataRow("45,5")]
    [DataRow("45.5")]
    [DataRow("4.5000")]
    [DataRow("-3")]
    [DataRow("1e3")]
    [DataRow("99999999999")]
    public void Inteiro_TextoOuFormatoErrado_ERecusadoEOCampoNaoFicaGravado(string text)
    {
        AdAttributes attributes = new();

        string error = Apply(Km, attributes, text);

        Assert.IsNotNull(error);
        Assert.IsFalse(attributes.Contains("km"), "nada é gravado, nem como texto");
    }

    [TestMethod]
    public void Inteiro_ForaDaFaixa_ERecusadoComAFaixaNaMensagem()
    {
        string error = Apply(Km, new AdAttributes(), "10.000.000");

        StringAssert.Contains(error, "9.999.999");
    }

    [TestMethod]
    public void Vazio_ApagaOCampo()
    {
        AdAttributes attributes = new AdAttributes().Set("km", 100);

        Assert.IsNull(Apply(Km, attributes, ""));
        Assert.IsFalse(attributes.Contains("km"));
        Assert.IsNull(Apply(Km, attributes.Set("km", 5), "   "));
        Assert.IsFalse(attributes.Contains("km"));
        Assert.IsNull(Apply(Km, attributes.Set("km", 5)));
        Assert.IsFalse(attributes.Contains("km"), "campo ausente no formulário também apaga");
    }

    [TestMethod]
    [DataRow("450,75", 450.75)]
    [DataRow("1.200,5", 1200.5)]
    [DataRow("1200", 1200)]
    [DataRow("0,01", 0.01)]
    public void Decimal_AceitaVirgulaEMilharComPonto(string text, double expected)
    {
        FieldDefinition area = FieldGroupRegistry.Get(FieldGroupKeys.RealEstate).Field("areaM2");
        AdAttributes attributes = new();

        Assert.IsNull(FieldValueParser.Apply(area, 5, [text], attributes, CurrentYear));

        Assert.IsTrue(attributes.TryGetDecimal("areaM2", out decimal value));
        Assert.AreEqual((decimal)expected, value);
    }

    [TestMethod]
    [DataRow("450.75")]
    [DataRow("45,075")]
    [DataRow("cem")]
    [DataRow("1,2,3")]
    [DataRow("450,")]
    public void Decimal_PontoDecimalCasasAMaisOuTexto_ERecusado(string text)
    {
        FieldDefinition area = FieldGroupRegistry.Get(FieldGroupKeys.RealEstate).Field("areaM2");
        AdAttributes attributes = new();

        Assert.IsNotNull(FieldValueParser.Apply(area, 5, [text], attributes, CurrentYear));
        Assert.IsFalse(attributes.Contains("areaM2"));
    }

    [TestMethod]
    public void Dinheiro_GravaCentavosComoNumeroLongo()
    {
        FieldDefinition condo = FieldGroupRegistry.Get(FieldGroupKeys.RealEstate).Field("condoFeeCents");
        AdAttributes attributes = new();

        Assert.IsNull(FieldValueParser.Apply(condo, 5, ["1.250,00"], attributes, CurrentYear));

        Assert.IsTrue(attributes.TryGetLong("condoFeeCents", out long cents));
        Assert.AreEqual(125_000L, cents);
        StringAssert.Contains(FieldValueParser.Apply(condo, 5, ["muito"], attributes, CurrentYear), "reais");
        Assert.IsNotNull(FieldValueParser.Apply(condo, 5, ["100.000.000,00"], attributes, CurrentYear), "acima do teto");
    }

    [TestMethod]
    public void Opcao_ForaDaListaEhRecusada_ENuncaGravaOId()
    {
        FieldDefinition transmission = FieldGroupRegistry.Get(FieldGroupKeys.Cars).Field("transmissionId");
        AdAttributes attributes = new();

        Assert.IsNull(FieldValueParser.Apply(transmission, Cars, ["1"], attributes, CurrentYear));
        Assert.IsTrue(attributes.TryGetInt("transmissionId", out int id));
        Assert.AreEqual(1, id);

        AdAttributes other = new();
        Assert.AreEqual(FieldValueParser.InvalidOption, FieldValueParser.Apply(transmission, Cars, ["999"], other, CurrentYear));
        Assert.AreEqual(FieldValueParser.InvalidOption, FieldValueParser.Apply(transmission, Cars, ["automatico"], other, CurrentYear));
        Assert.AreEqual(FieldValueParser.InvalidOption, FieldValueParser.Apply(transmission, Cars, ["1", "2"], other, CurrentYear), "uma opção só");
        Assert.AreEqual(0, new List<string>(other.Keys).Count);
    }

    [TestMethod]
    public void VariasOpcoes_GravaOsIdsSemRepetir_ERecusaSeUmaNaoExiste()
    {
        FieldDefinition optional = FieldGroupRegistry.Get(FieldGroupKeys.Cars).Field("optionalItemIds");
        AdAttributes attributes = new();

        Assert.IsNull(FieldValueParser.Apply(optional, Cars, ["1", "2", "2"], attributes, CurrentYear));
        CollectionAssert.AreEqual(new[] { 1, 2 }, new List<int>(attributes.GetInts("optionalItemIds")));

        AdAttributes bad = new();
        Assert.IsNotNull(FieldValueParser.Apply(optional, Cars, ["1", "9999"], bad, CurrentYear));
        Assert.IsFalse(bad.Contains("optionalItemIds"));
    }

    [TestMethod]
    [DataRow("2027", true)]
    [DataRow("2026", true)]
    [DataRow("1950", true)]
    [DataRow("2028", false)]
    [DataRow("1949", false)]
    [DataRow("dois mil", false)]
    public void AnoDoModelo_VaiDe1950AoAnoQueVem(string text, bool valid)
    {
        FieldDefinition year = FieldGroupRegistry.Get(FieldGroupKeys.TrucksAndBuses).Field("modelYear");
        AdAttributes attributes = new();

        string error = FieldValueParser.Apply(year, 54, [text], attributes, CurrentYear);

        Assert.AreEqual(valid, error is null, error);
        Assert.AreEqual(valid, attributes.Contains("modelYear"));
    }

    [TestMethod]
    public void AnoDeFabricacao_NaoPassaDoAnoAtual()
    {
        FieldDefinition year = FieldGroupRegistry.Get(FieldGroupKeys.Machinery).Field("manufactureYear");

        Assert.IsNull(FieldValueParser.Apply(year, 120, ["2026"], new AdAttributes(), CurrentYear));
        Assert.IsNotNull(FieldValueParser.Apply(year, 120, ["2027"], new AdAttributes(), CurrentYear));
    }

    [TestMethod]
    public void Texto_RespeitaOLimiteDoCampo()
    {
        FieldDefinition text = new() { Key = "nota", Label = "Nota", Type = FieldType.Text, MaxLength = 5 };
        AdAttributes attributes = new();

        Assert.IsNull(FieldValueParser.Apply(text, Cars, ["abcde"], attributes, CurrentYear));
        Assert.AreEqual("abcde", attributes.GetString("nota"));
        Assert.IsNotNull(FieldValueParser.Apply(text, Cars, ["abcdef"], attributes, CurrentYear));
    }

    [TestMethod]
    public void IdDeCatalogo_PrecisaSerInteiro()
    {
        FieldDefinition brand = FieldGroupRegistry.Get(FieldGroupKeys.Cars).Field("brandId");
        AdAttributes attributes = new();

        Assert.IsNull(FieldValueParser.Apply(brand, Cars, ["1"], attributes, CurrentYear));
        Assert.IsNotNull(FieldValueParser.Apply(brand, Cars, ["Honda"], new AdAttributes(), CurrentYear));
        Assert.IsNotNull(FieldValueParser.Apply(brand, Cars, ["1.5"], new AdAttributes(), CurrentYear));
    }

    [TestMethod]
    public void TipoDesconhecido_FalhaAltoEmVezDeGravarQualquerCoisa()
    {
        FieldDefinition unknown = new() { Key = "x", Label = "X", Type = (FieldType)99 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => FieldValueParser.Apply(unknown, Cars, ["1"], new AdAttributes(), CurrentYear));
    }
}
