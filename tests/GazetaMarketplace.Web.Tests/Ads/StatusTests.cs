using System.Collections.Generic;
using System.Linq;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>As situações do anúncio e as passagens do Apêndice A da SPEC.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class StatusTests
#pragma warning restore CA1515
{
    // Escrito à mão a partir do Apêndice A da SPEC (a segunda representação da tabela de passagens)
    private static readonly HashSet<(byte From, byte To)> Expected =
    [
        (1, 2), (4, 2), // Rascunho e Rejeitado → Em revisão
        (2, 3), (2, 4), // Em revisão → Publicado ou Rejeitado
        (3, 1), // Publicado → Rascunho
        (1, 5), (2, 5), (4, 5), (3, 5) // qualquer um menos Arquivado → Arquivado
    ];

    [TestMethod]
    public void Constantes_SaoOsValoresDoBancoDoArchitecture_ESqlFragmentsUsaOMesmoPublicado()
    {
        // Os números do banco (ARCHITECTURE §6.2), em ordem: Rascunho, Em revisão, Publicado, Rejeitado, Arquivado
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, new[] { AdStatus.Draft, AdStatus.InReview, AdStatus.Published, AdStatus.Rejected, AdStatus.Archived });
        object published = typeof(SqlFragments).GetField(nameof(SqlFragments.PublishedStatus))!.GetRawConstantValue();
        Assert.AreEqual((byte)3, published, "o fragmento 'só publicados' das leituras Dapper");
        Assert.AreEqual(AdStatus.Published, (byte)published);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, AdStatus.All.ToArray());
    }

    [TestMethod]
    public void Rotulos_SaoOsDaSpec_ENumeroDesconhecidoNaoEhValido()
    {
        CollectionAssert.AreEqual(
            new[] { "Rascunho", "Em revisão", "Publicado", "Rejeitado", "Arquivado" },
            AdStatus.All.Select(AdStatus.Label).ToArray());
        Assert.IsFalse(AdStatus.IsValid(0));
        Assert.IsFalse(AdStatus.IsValid(6));
        Assert.IsTrue(AdStatus.All.All(AdStatus.IsValid));
    }

    [TestMethod]
    public void TabelaDePassagens_TemExatamenteAsNoveDoApendiceA()
    {
        HashSet<(byte, byte)> actual = [.. AdStatusRules.All.Select(t => (t.From, t.To))];

        Assert.AreEqual(9, AdStatusRules.All.Count);
        Assert.IsTrue(Expected.SetEquals(actual));
    }

    [TestMethod]
    public void TransicoesValidas_E_Invalidas_AsVinteECincoCombinacoes()
    {
        foreach (byte from in AdStatus.All)
        {
            foreach (byte to in AdStatus.All)
            {
                Ad ad = AdFactory.At(from);
                bool valid = Expected.Contains((from, to));

                if (valid)
                {
                    ad.ApplyTransition(to, 7, new System.DateTime(2026, 10, 3, 0, 0, 0, System.DateTimeKind.Utc), "motivo");
                    Assert.AreEqual(to, ad.Status, $"{from} → {to}");
                }
                else
                {
                    ConflictException conflict = Assert.ThrowsExactly<ConflictException>(() => ad.ApplyTransition(to, 7, System.DateTime.UtcNow, "motivo"), $"{from} → {to}");
                    Assert.AreEqual("CONFLICT", conflict.Code);
                    Assert.AreEqual(from, ad.Status, "a recusa não muda a situação");
                }
            }
        }
    }

    [TestMethod]
    public void Arquivado_EhDefinitivo()
    {
        Assert.IsEmpty(AdStatusRules.All.Where(t => t.From == AdStatus.Archived));
        Assert.IsNull(AdStatusRules.Find(AdStatus.Archived, AdStatus.Draft));
        Assert.IsNull(AdStatusRules.Find(AdStatus.Archived, AdStatus.Published));
    }

    [TestMethod]
    public void SoOEnvioEhDoAutor_AsDemaisPassagensSaoDoAdministrador()
    {
        CollectionAssert.AreEquivalent(
            new[] { (AdStatus.Draft, AdStatus.InReview), (AdStatus.Rejected, AdStatus.InReview) },
            AdStatusRules.All.Where(t => t.AuthorAllowed).Select(t => (t.From, t.To)).ToArray());
    }

    [TestMethod]
    public void AcoesDaAuditoria_SaoUmaPorTipoDePassagem()
    {
        Assert.AreEqual("ad.submit", AdStatusRules.Find(AdStatus.Draft, AdStatus.InReview).Action);
        Assert.AreEqual("ad.submit", AdStatusRules.Find(AdStatus.Rejected, AdStatus.InReview).Action);
        Assert.AreEqual("ad.publish", AdStatusRules.Find(AdStatus.InReview, AdStatus.Published).Action);
        Assert.AreEqual("ad.reject", AdStatusRules.Find(AdStatus.InReview, AdStatus.Rejected).Action);
        Assert.AreEqual("ad.unpublish", AdStatusRules.Find(AdStatus.Published, AdStatus.Draft).Action);
        Assert.IsTrue(AdStatusRules.All.Where(t => t.To == AdStatus.Archived).All(t => t.Action == "ad.archive"));
    }
}
