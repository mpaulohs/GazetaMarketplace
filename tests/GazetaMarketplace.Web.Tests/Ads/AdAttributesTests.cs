using System.Linq;
using GazetaMarketplace.Core.Ads;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>Os campos do grupo em JSON (ADR-002): ida e volta, leitura estrita no tipo e só objetos JSON.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdAttributesTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void Vazio_EhUmObjetoJsonVazio() => Assert.AreEqual("{}", new AdAttributes().ToJson());

    [TestMethod]
    public void IdaEVolta_PreservaInteirosDecimaisTextosELista()
    {
        string json = new AdAttributes()
            .Set("brandId", 12)
            .Set("km", 45000)
            .Set("areaM2", 450.75m)
            .Set("productType", "Camisa")
            .Set("optionalItemIds", [1, 4, 9])
            .ToJson();

        Assert.IsTrue(AdAttributes.TryParse(json, out AdAttributes read));
        Assert.IsTrue(read.TryGetInt("brandId", out int brand));
        Assert.AreEqual(12, brand);
        Assert.IsTrue(read.TryGetInt("km", out int km));
        Assert.AreEqual(45000, km);
        Assert.IsTrue(read.TryGetDecimal("areaM2", out decimal area));
        Assert.AreEqual(450.75m, area);
        Assert.AreEqual("Camisa", read.GetString("productType"));
        CollectionAssert.AreEqual(new[] { 1, 4, 9 }, read.GetInts("optionalItemIds").ToArray());
        StringAssert.Contains(json, "\"areaM2\":450.75", "decimal com ponto, sem cultura");
    }

    [TestMethod]
    public void TipoErrado_EhAusente_NuncaUmValorAdivinhado()
    {
        Assert.IsTrue(AdAttributes.TryParse("""{"km":"abc","brandId":"12","name":5,"areaM2":"450.75","ids":[1,"2"]}""", out AdAttributes read));

        Assert.IsFalse(read.TryGetInt("km", out _), "texto não é número");
        Assert.IsFalse(read.TryGetInt("brandId", out _), "'12' entre aspas não é número");
        Assert.IsNull(read.GetString("name"), "número não é texto");
        Assert.IsFalse(read.TryGetDecimal("areaM2", out _));
        Assert.IsEmpty(read.GetInts("ids"), "um item que não é inteiro invalida a lista");
        Assert.IsFalse(read.TryGetInt("naoExiste", out _));
        Assert.IsNull(read.GetString("naoExiste"));
    }

    [TestMethod]
    public void Decimal_NaoEhLidoComoInteiro()
    {
        Assert.IsTrue(AdAttributes.TryParse("""{"km":45000.5}""", out AdAttributes read));
        Assert.IsFalse(read.TryGetInt("km", out _));
        Assert.IsTrue(read.TryGetDecimal("km", out decimal value));
        Assert.AreEqual(45000.5m, value);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("não é json")]
    [DataRow("{\"km\":")]
    [DataRow("[1,2,3]")]
    [DataRow("42")]
    [DataRow("\"texto\"")]
    [DataRow("null")]
    public void SoObjetoJsonValidoEhAceito(string json)
    {
        bool parsed = AdAttributes.TryParse(json, out AdAttributes read);

        Assert.IsFalse(parsed);
        Assert.IsNull(read);
    }

    [TestMethod]
    public void SetComNulo_RemoveOCampo_ERemoveTiraDoJson()
    {
        AdAttributes attributes = new AdAttributes().Set("brand", "Honda").Set("model", "Civic");

        attributes.Set("brand", (string)null);
        attributes.Remove("model");

        Assert.AreEqual("{}", attributes.ToJson());
        Assert.IsFalse(attributes.Contains("brand"));
    }

    [TestMethod]
    public void Chaves_SaoAsDoJson_NaOrdemDeEntrada()
    {
        AdAttributes attributes = new AdAttributes().Set("b", 1).Set("a", 2);

        CollectionAssert.AreEqual(new[] { "b", "a" }, attributes.Keys.ToArray());
    }
}
