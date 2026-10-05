using System;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>A entidade <see cref="Ad"/>: rascunho só com o título, colunas de busca preenchidas só pelo C# e trilha de decisão.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdEntityTests
#pragma warning restore CA1515
{
    private static readonly DateTime Now = new(2026, 10, 3, 14, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void RascunhoSoComOTitulo_US008S07()
    {
        Ad ad = Ad.CreateDraft("  Moto para retirar peças  ", 5);

        Assert.AreEqual("Moto para retirar peças", ad.Title, "espaços das pontas saem");
        Assert.AreEqual(AdStatus.Draft, ad.Status);
        Assert.AreEqual(5, ad.AuthorId);
        Assert.IsNull(ad.CategoryId, "rascunho nasce sem categoria");
        Assert.IsNull(ad.Description);
        Assert.IsNull(ad.PriceCents);
        Assert.IsNull(ad.Cep);
        Assert.AreEqual("{}", ad.Attributes);
        Assert.IsFalse(ad.LocationManual);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("    ")]
    public void SemTitulo_NaoCria_US008S08(string title)
    {
        ValidationException error = Assert.ThrowsExactly<ValidationException>(() => Ad.CreateDraft(title, 1));

        CollectionAssert.AreEqual(new[] { "Informe um título" }, error.Errors["title"]);
    }

    [TestMethod]
    public void Titulo_Ate120Caracteres()
    {
        Ad ad = Ad.CreateDraft(new string('a', 120), 1);
        Assert.HasCount(120, ad.Title);

        Assert.ThrowsExactly<ValidationException>(() => Ad.CreateDraft(new string('a', 121), 1));
        Assert.ThrowsExactly<ValidationException>(() => ad.SetText(new string('a', 121), null));
    }

    [TestMethod]
    public void Descricao_ATe6000Caracteres_EVaziaViraNula()
    {
        Ad ad = Ad.CreateDraft("Casa", 1);

        ad.SetText("Casa", new string('x', 6000));
        Assert.HasCount(6000, ad.Description);
        Assert.ThrowsExactly<ValidationException>(() => ad.SetText("Casa", new string('x', 6001)));

        ad.SetText("Casa", "   ");
        Assert.IsNull(ad.Description);
        Assert.IsNull(ad.DescriptionSearch);
    }

    [TestMethod]
    public void ColunasDeBusca_SaoNormalizadasPeloCSharp()
    {
        Ad ad = Ad.CreateDraft("Pão de Açúcar — APARTAMENTO", 1);
        ad.SetText("Pão de Açúcar — APARTAMENTO", "Ótimo   imóvel, São Paulo");

        Assert.AreEqual("pao de acucar — apartamento", ad.TitleSearch);
        Assert.AreEqual("otimo imovel, sao paulo", ad.DescriptionSearch);
        Assert.AreEqual("Pão de Açúcar — APARTAMENTO", ad.Title, "o texto exibido não muda");
    }

    [TestMethod]
    public void ColunasDeBusca_NaoTemSetterPublico()
    {
        foreach (string name in new[] { nameof(Ad.TitleSearch), nameof(Ad.DescriptionSearch), nameof(Ad.VehicleBrandId), nameof(Ad.VehicleModelId), nameof(Ad.ModelYear), nameof(Ad.Km), nameof(Ad.AreaM2), nameof(Ad.Status), nameof(Ad.PriceCents) })
        {
            Assert.IsFalse(typeof(Ad).GetProperty(name)!.SetMethod!.IsPublic, $"{name} só muda pelos métodos do anúncio");
        }
    }

    [TestMethod]
    public void Localizacao_PadronizaUfECidade_ECepNulo()
    {
        Ad ad = Ad.CreateDraft("Casa", 1);

        ad.SetLocation("13015100", "  Campinas ", "sp", false);
        Assert.AreEqual("13015100", ad.Cep);
        Assert.AreEqual("Campinas", ad.City);
        Assert.AreEqual("SP", ad.Uf);
        Assert.IsFalse(ad.LocationManual);

        ad.SetLocation(" ", "", null, true);
        Assert.IsNull(ad.Cep);
        Assert.IsNull(ad.City);
        Assert.IsNull(ad.Uf);
        Assert.IsTrue(ad.LocationManual);
    }

    [TestMethod]
    public void Atributos_GravamOJsonDoObjeto()
    {
        Ad ad = Ad.CreateDraft("Civic", 1);

        ad.SetAttributes(new AdAttributes().Set("km", 45000));

        Assert.AreEqual("""{"km":45000}""", ad.Attributes);
        Assert.ThrowsExactly<ArgumentNullException>(() => ad.SetAttributes(null));
    }

    [TestMethod]
    public void Envio_GravaSentAt_ELimpaARejeicaoAnterior()
    {
        Ad ad = AdFactory.At(AdStatus.Rejected, 5);
        Assert.AreEqual("Fotos escuras", ad.RejectionReason);
        Assert.IsNotNull(ad.RejectedAt);

        ad.ApplyTransition(AdStatus.InReview, 5, Now, null);

        Assert.AreEqual(Now, ad.SentAt);
        Assert.IsNull(ad.RejectionReason);
        Assert.IsNull(ad.RejectedAt);
        Assert.IsNull(ad.RejectedById);
    }

    [TestMethod]
    public void Publicar_GravaQuemEQuando()
    {
        Ad ad = AdFactory.At(AdStatus.InReview, 5);

        ad.ApplyTransition(AdStatus.Published, 99, Now, "ignorado");

        Assert.AreEqual(Now, ad.PublishedAt);
        Assert.AreEqual(99, ad.PublishedById);
        Assert.IsNull(ad.RejectionReason, "o motivo só vale para rejeitar");
    }

    [TestMethod]
    public void Rejeitar_GravaQuemQuandoEOMotivoSemEspacos()
    {
        Ad ad = AdFactory.At(AdStatus.InReview, 5);

        ad.ApplyTransition(AdStatus.Rejected, 99, Now, "  Fotos escuras  ");

        Assert.AreEqual(Now, ad.RejectedAt);
        Assert.AreEqual(99, ad.RejectedById);
        Assert.AreEqual("Fotos escuras", ad.RejectionReason);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Rejeitar_SemMotivo_Recusa_ENaoMudaNada(string reason)
    {
        Ad ad = AdFactory.At(AdStatus.InReview, 5);

        ValidationException error = Assert.ThrowsExactly<ValidationException>(() => ad.ApplyTransition(AdStatus.Rejected, 99, Now, reason));

        CollectionAssert.AreEqual(new[] { "Informe o motivo da rejeição" }, error.Errors["reason"]);
        Assert.AreEqual(AdStatus.InReview, ad.Status);
        Assert.IsNull(ad.RejectedAt);
    }

    [TestMethod]
    public void Rejeitar_MotivoAte500Caracteres()
    {
        Ad ok = AdFactory.At(AdStatus.InReview, 5);
        ok.ApplyTransition(AdStatus.Rejected, 99, Now, new string('m', 500));
        Assert.HasCount(500, ok.RejectionReason);

        Ad tooLong = AdFactory.At(AdStatus.InReview, 5);
        Assert.ThrowsExactly<ValidationException>(() => tooLong.ApplyTransition(AdStatus.Rejected, 99, Now, new string('m', 501)));
        Assert.AreEqual(AdStatus.InReview, tooLong.Status);
    }

    [TestMethod]
    public void Despublicar_LimpaAPublicacao_ArquivarGravaAData()
    {
        Ad ad = AdFactory.At(AdStatus.Published, 5);
        Assert.IsNotNull(ad.PublishedAt);

        ad.ApplyTransition(AdStatus.Draft, 99, Now, null);
        Assert.IsNull(ad.PublishedAt);
        Assert.IsNull(ad.PublishedById);
        Assert.AreEqual(AdStatus.Draft, ad.Status);

        ad.ApplyTransition(AdStatus.Archived, 99, Now, null);
        Assert.AreEqual(Now, ad.ArchivedAt);
        Assert.AreEqual(99, ad.ArchivedById);
    }

    [TestMethod]
    [DataRow(AdStatus.Draft)]
    [DataRow(AdStatus.InReview)]
    [DataRow(AdStatus.Rejected)]
    [DataRow(AdStatus.Published)]
    public void Arquivar_DeQualquerSituacao_GravaQuemEQuando(byte from)
    {
        Ad ad = AdFactory.At(from, 5, deciderId: 77);

        ad.ApplyTransition(AdStatus.Archived, 42, Now, null);

        Assert.AreEqual(AdStatus.Archived, ad.Status);
        Assert.AreEqual(Now, ad.ArchivedAt);
        Assert.AreEqual(42, ad.ArchivedById, "quem arquivou, não quem publicou nem quem rejeitou antes");
    }

    [TestMethod]
    public void Arquivar_NaoMexeNaTrilhaDePublicacaoNemDeRejeicao_ENenhumaOutraPassagemGravaArquivamento()
    {
        Ad published = AdFactory.At(AdStatus.Published, 5, deciderId: 77);
        Ad inReview = AdFactory.At(AdStatus.InReview, 5);

        published.ApplyTransition(AdStatus.Archived, 42, Now, null);
        inReview.ApplyTransition(AdStatus.Published, 42, Now, null);

        Assert.AreEqual(77, published.PublishedById, "a publicação fica no histórico");
        Assert.IsNull(inReview.ArchivedById);
        Assert.IsNull(inReview.ArchivedAt);
    }

    [TestMethod]
    public void Arquivar_UmPublicado_MantemAPublicacaoNoHistorico()
    {
        Ad ad = AdFactory.At(AdStatus.Published, 5);

        ad.ApplyTransition(AdStatus.Archived, 99, Now, null);

        Assert.IsNotNull(ad.PublishedAt);
        Assert.AreEqual(Now, ad.ArchivedAt);
    }
}
