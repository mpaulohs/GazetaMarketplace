using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>Preço em centavos: nulo = sem preço (Serviços), nunca zero; teto de R$ 99.999.999,99 (S28).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PriceTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void Servico_GravaNulo_NuncaZero()
    {
        Ad ad = Ad.CreateDraft("Pintor de paredes", 1);
        Assert.IsNull(ad.PriceCents, "um anúncio novo não tem preço");

        ad.SetPrice(6_200_000);
        ad.SetPrice(null);
        Assert.IsNull(ad.PriceCents, "'sem preço' volta a ser nulo");
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-1L)]
    [DataRow(-6_200_000L)]
    [DataRow(10_000_000_000L)]
    [DataRow(long.MaxValue)]
    public void ZeroNegativoEAcimaDoTeto_SaoRecusados(long cents)
    {
        Ad ad = Ad.CreateDraft("Honda Civic 2018", 1);
        ad.SetPrice(5_000);

        ValidationException error = Assert.ThrowsExactly<ValidationException>(() => ad.SetPrice(cents));

        Assert.IsTrue(error.Errors.ContainsKey("price"));
        Assert.AreEqual(5_000, ad.PriceCents, "a recusa não muda o preço");
    }

    [TestMethod]
    [DataRow(1L)]
    [DataRow(6_200_000L)]
    [DataRow(9_999_999_999L)]
    public void DeUmCentavoAoTeto_Aceita(long cents)
    {
        Ad ad = Ad.CreateDraft("Honda Civic 2018", 1);

        ad.SetPrice(cents);

        Assert.AreEqual(cents, ad.PriceCents);
    }
}
