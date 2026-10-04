using System.Linq;
using GazetaMarketplace.Core.Location;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Cep;

/// <summary>A padronização do nome da cidade (SPEC, US-008 + D6), as 27 UFs e as regras do CEP.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CityNamesAndStatesTests
#pragma warning restore CA1515
{
    [TestMethod]
    [DataRow("sao jose", "Sao Jose")]
    [DataRow("são josé", "São José")]
    [DataRow("SÃO JOSÉ DO RIO PRETO", "São José do Rio Preto")]
    [DataRow("são josé dos campos", "São José dos Campos")]
    [DataRow("rio de janeiro", "Rio de Janeiro")]
    [DataRow("  santa   bárbara   d'oeste ", "Santa Bárbara d'Oeste")]
    [DataRow("santa bárbara d’oeste", "Santa Bárbara d’Oeste")]
    [DataRow("MOGI-MIRIM", "Mogi-Mirim")]
    [DataRow("campos dos goytacazes", "Campos dos Goytacazes")]
    [DataRow("nova iguaçu", "Nova Iguaçu")]
    [DataRow("de", "De")]
    [DataRow("DA SILVA", "Da Silva")]
    [DataRow("barra e bonita", "Barra e Bonita")]
    public void Cidade_IniciaisMaiusculas_AcentosMantidos_ArtigosMinusculosForaDaPrimeiraPalavra(string input, string expected) =>
        Assert.AreEqual(expected, CityNames.Standardize(input));

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Cidade_VaziaViraTextoVazio(string input) => Assert.AreEqual(string.Empty, CityNames.Standardize(input));

    [TestMethod]
    public void Cidade_PadronizarDuasVezes_DaNoMesmo()
    {
        foreach (string name in new[] { "são josé do rio preto", "SANTA BÁRBARA D'OESTE", "mogi-mirim" })
        {
            string once = CityNames.Standardize(name);
            Assert.AreEqual(once, CityNames.Standardize(once));
        }
    }

    [TestMethod]
    public void Ufs_SaoAs27_ComCodigosDoIbgeUnicosENomesUnicos()
    {
        Assert.HasCount(27, BrazilianStates.All);
        Assert.HasCount(27, BrazilianStates.All.Select(s => s.Uf).Distinct());
        Assert.HasCount(27, BrazilianStates.All.Select(s => s.IbgeCode).Distinct());
        Assert.IsTrue(BrazilianStates.All.All(s => s.Uf.Length == 2 && s.Uf == s.Uf.ToUpperInvariant() && s.IbgeCode is >= 11 and <= 53));
        Assert.AreEqual(35, BrazilianStates.Find("SP")!.IbgeCode);
        Assert.AreEqual("Distrito Federal", BrazilianStates.Find("DF")!.Name);
    }

    [TestMethod]
    [DataRow("sp", true)]
    [DataRow(" RJ ", true)]
    [DataRow("XX", false)]
    [DataRow("S", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void Uf_Valida_SemDiferenciarCaixa(string uf, bool valid) => Assert.AreEqual(valid, BrazilianStates.IsValid(uf));

    [TestMethod]
    [DataRow(3550308, "SP")]
    [DataRow(3304557, "RJ")]
    [DataRow(5300108, "DF")]
    [DataRow(1100015, "RO")]
    public void CodigoDoMunicipio_TemNosDoisPrimeirosDigitosAUf(int code, string uf) => Assert.AreEqual(uf, BrazilianStates.FromMunicipalityCode(code)!.Uf);

    [TestMethod]
    [DataRow(0)]
    [DataRow(999_999)]
    [DataRow(10_000_000)]
    [DataRow(9_999_999)]
    [DataRow(6_000_000)]
    public void CodigoDeMunicipioInvalido_NaoTemUf(int code) => Assert.IsNull(BrazilianStates.FromMunicipalityCode(code));

    [TestMethod]
    [DataRow("13015100", true)]
    [DataRow("00000000", true)]
    [DataRow("1301510", false)]
    [DataRow("130151000", false)]
    [DataRow("13015-100", false)]
    [DataRow("1301510a", false)]
    [DataRow("١٣٠١٥١٠٠", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void Cep_SoOitoDigitosAscii(string cep, bool valid) => Assert.AreEqual(valid, CepRules.IsValid(cep));

    [TestMethod]
    public void Cep_FormataParaATela() => Assert.AreEqual("13015-100", CepRules.Format("13015100"));
}
