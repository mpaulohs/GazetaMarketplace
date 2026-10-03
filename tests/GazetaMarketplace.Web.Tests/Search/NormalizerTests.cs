using GazetaMarketplace.Core.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Search;

/// <summary>A normalização da busca (ADR-006): minúsculas, sem acento, espaços repetidos reduzidos.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class NormalizerTests
#pragma warning restore CA1515
{
    [TestMethod]
    [DataRow("Honda Civic", "honda civic")]
    [DataRow("CASA DE CAMPO", "casa de campo")]
    [DataRow("Pão de Açúcar", "pao de acucar")]
    [DataRow("Tênis Ôntem Ñandú", "tenis ontem nandu")]
    [DataRow("Àáâãäå Èéêë Ìíîï Òóôõö Ùúûü Ç", "aaaaaa eeee iiii ooooo uuuu c")]
    public void RemoveAcentos_E_MinusculasNaBusca(string input, string expected) =>
        Assert.AreEqual(expected, Normalizer.Normalize(input));

    [TestMethod]
    public void EspacosRepetidos_ViramUm_ENasPontasSaem()
    {
        Assert.AreEqual("a b c", Normalizer.Normalize("  a   b\t\tc \n"));
        Assert.AreEqual("duas palavras", Normalizer.Normalize("duas  palavras"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   \t ")]
    public void NuloOuVazio_ViraTextoVazio(string input) => Assert.AreEqual(string.Empty, Normalizer.Normalize(input));

    [TestMethod]
    public void AcentoJaDecomposto_E_ComposicaoGeramOMesmoTexto()
    {
        Assert.AreEqual(Normalizer.Normalize("café"), Normalizer.Normalize("café"));
        Assert.AreEqual("cafe", Normalizer.Normalize("café"));
    }

    [TestMethod]
    public void NumerosPontuacaoEEmoji_FicamComoEstao()
    {
        Assert.AreEqual("r$ 62.000,00 - 4x4!", Normalizer.Normalize("R$ 62.000,00 - 4X4!"));
        Assert.AreEqual("carro 🚗", Normalizer.Normalize("Carro 🚗"));
    }

    [TestMethod]
    public void Idempotente_ENuncaMaiorQueOTextoOriginal()
    {
        foreach (string text in new[] { "Pão de Açúcar", "  ÀÉ  ", "Ação ão", "straße" })
        {
            string once = Normalizer.Normalize(text);
            Assert.AreEqual(once, Normalizer.Normalize(once));
            Assert.IsLessThanOrEqualTo(text.Length, once.Length);
        }
    }

    [TestMethod]
    public void TermoDigitado_E_TextoGravado_CasamAcentuadoOuNao()
    {
        // O mesmo código normaliza os dois lados: quem digita sem acento acha o anúncio com acento
        StringAssert.Contains(Normalizer.Normalize("Apartamento Jardim São Paulo"), Normalizer.Normalize("sao PAULO"));
    }
}
