using System;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Fields;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Components;

/// <summary>As regras de apresentação do anúncio (3.8): a forma do valor (S29), o valor por grupo (A6), o nome acessível do card, a localização e a miniatura.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdPresentationTests
#pragma warning restore CA1515
{
    private static FieldGroup Group(string key) => FieldGroupRegistry.Get(key);

    [TestMethod]
    [DataRow(6_200_000L, "R$ 62.000")]
    [DataRow(249_990L, "R$ 2.499,90")]
    [DataRow(50L, "R$ 0,50")]
    [DataRow(18_000L, "R$ 180")]
    [DataRow(100L, "R$ 1")]
    [DataRow(9_999_999_999L, "R$ 99.999.999,99")]
    [DataRow(100_000_000L, "R$ 1.000.000")]
    [DataRow(1_005L, "R$ 10,05")]
    public void Valor_CentavosSoQuandoNaoSaoZero(long cents, string expected)
    {
        Assert.AreEqual(expected, AdPresentation.FormatMoney(cents));
    }

    [TestMethod]
    public void Valor_Negativo_Lanca()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AdPresentation.FormatMoney(-1));
    }

    [TestMethod]
    public void ValorPorGrupo_PrecoSalarioOuTipo()
    {
        AdValue price = AdPresentation.ValueOf(Group(FieldGroupKeys.Cars), 6_200_000, null);
        AdValue salary = AdPresentation.ValueOf(Group(FieldGroupKeys.Jobs), 280_000, null);
        AdValue service = AdPresentation.ValueOf(Group(FieldGroupKeys.Services), null, " Serviços domésticos ");

        Assert.AreEqual(new AdValue(AdValueKind.Price, "Preço", "R$ 62.000"), price);
        Assert.AreEqual(new AdValue(AdValueKind.Salary, "Salário", "R$ 2.800"), salary);
        Assert.AreEqual(new AdValue(AdValueKind.ServiceType, "Tipo", "Serviços domésticos"), service, "o tipo vem aparado e sem valor");
    }

    [TestMethod]
    public void Servicos_NuncaMostramPreco_MesmoQueExista()
    {
        AdValue service = AdPresentation.ValueOf(Group(FieldGroupKeys.Services), 500_000, "Eletricista");

        Assert.AreEqual(AdValueKind.ServiceType, service.Kind);
        Assert.DoesNotContain("R$", service.Text);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(0L)]
    public void SemPreco_NaoHaValorParaMostrar(long? price)
    {
        Assert.IsNull(AdPresentation.ValueOf(Group(FieldGroupKeys.GeneralProducts), price, null), "nunca mostra \"R$ 0\"");
        Assert.IsNull(AdPresentation.ValueOf(Group(FieldGroupKeys.Jobs), price, null));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ServicoSemTipo_NaoHaValorParaMostrar(string type)
    {
        Assert.IsNull(AdPresentation.ValueOf(Group(FieldGroupKeys.Services), null, type));
    }

    [TestMethod]
    public void SoVagasFicamSemFoto()
    {
        Assert.IsFalse(AdPresentation.HasPhotos(Group(FieldGroupKeys.Jobs)));
        Assert.IsTrue(AdPresentation.HasPhotos(Group(FieldGroupKeys.Services)));
        Assert.IsTrue(AdPresentation.HasPhotos(Group(FieldGroupKeys.Cars)));
    }

    [TestMethod]
    [DataRow("Campinas", "SP", "Campinas/SP")]
    [DataRow(" Campinas ", " SP ", "Campinas/SP")]
    [DataRow("Campinas", null, "Campinas")]
    [DataRow("Campinas", "", "Campinas")]
    [DataRow(null, "SP", "SP")]
    [DataRow(null, null, null)]
    [DataRow(" ", " ", null)]
    public void Localizacao_CidadeBarraUf(string city, string uf, string expected)
    {
        Assert.AreEqual(expected, AdPresentation.Location(city, uf));
    }

    [TestMethod]
    public void NomeAcessivel_TituloValorCidadeUf()
    {
        AdValue price = new(AdValueKind.Price, "Preço", "R$ 62.000");
        AdValue salary = new(AdValueKind.Salary, "Salário", "R$ 2.800");
        AdValue service = new(AdValueKind.ServiceType, "Tipo", "Serviços domésticos");

        Assert.AreEqual("Honda Civic 2018, R$ 62.000, Campinas/SP", AdPresentation.AccessibleName("Honda Civic 2018", price, "Campinas", "SP"));
        Assert.AreEqual("Pizzaiolo, Salário R$ 2.800, Campinas/SP", AdPresentation.AccessibleName("Pizzaiolo", salary, "Campinas", "SP"));
        Assert.AreEqual("Diarista, Tipo: Serviços domésticos, São Paulo/SP", AdPresentation.AccessibleName("Diarista", service, "São Paulo", "SP"));
    }

    [TestMethod]
    public void NomeAcessivel_ParteQueFaltaSome_SemVirgulaSobrando()
    {
        AdValue price = new(AdValueKind.Price, "Preço", "R$ 180");

        Assert.AreEqual("Jaqueta jeans, R$ 180", AdPresentation.AccessibleName("Jaqueta jeans", price, null, null));
        Assert.AreEqual("Jaqueta jeans, Goiânia/GO", AdPresentation.AccessibleName("Jaqueta jeans", null, "Goiânia", "GO"));
        Assert.AreEqual("Jaqueta jeans", AdPresentation.AccessibleName("  Jaqueta jeans ", null, " ", ""));
        Assert.AreEqual("Jaqueta jeans, R$ 180, GO", AdPresentation.AccessibleName("Jaqueta jeans", price, null, "GO"));
    }

    [TestMethod]
    public void Miniatura_Endereco_E_Tamanho()
    {
        Assert.AreEqual("/fotos/7/9-480.webp", AdPresentation.CoverUrl(7, 9));
        Assert.AreEqual((480, 360), AdPresentation.ThumbSize(1600, 1200), "paisagem 4:3");
        Assert.AreEqual((480, 853), AdPresentation.ThumbSize(1600, 2844), "retrato: só a largura limita");
        Assert.AreEqual((300, 200), AdPresentation.ThumbSize(300, 200), "nunca amplia");
        Assert.AreEqual((64, 2560), AdPresentation.ThumbSize(500, 20_000), "o lado maior também tem teto (2560)");
        Assert.AreEqual((480, 360), AdPresentation.ThumbSize(0, 0), "sem medida gravada reserva 4:3");
        Assert.AreEqual(new AdCardCover(3, 480, 360), AdCardCover.FromStored(3, 1600, 1200));
    }
}
