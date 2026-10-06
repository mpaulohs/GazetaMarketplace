using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>
/// SPEC Apêndice A e US-008-S13: o Administrador edita o anúncio Publicado e a situação continua Publicado. Um anúncio no ar não pode perder o que a revisão exigiu: salvar uma edição que o envio à revisão recusaria
/// (sem categoria, descrição, preço, CEP ou foto, ou com campo obrigatório vazio) é recusado com as mesmas mensagens da lista de pendências, e nada é gravado. Rascunho e Rejeitado seguem salvando incompletos.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PublishedEditTests
#pragma warning restore CA1515
{
    private const string Writer = PanelFixture.WriterEmail;
    private const int GeneralProducts = 86;

    // Um anúncio Publicado completo: título, descrição, categoria de produtos gerais com a condição, preço, CEP com cidade e uma foto
    internal static async Task<int> PublishedCompleteAsync(DraftSite site, bool withPhoto = true)
    {
        int id = await site.AddAdAsync(Writer, AdStatus.Published, "Bicicleta aro 29", GeneralProducts, 150_000);
        await site.Harness.WithDbAsync(async db =>
        {
            Ad ad = await db.Ads.SingleAsync(a => a.Id == id);
            ad.SetText("Bicicleta aro 29", "Pouco usada");
            ad.SetLocation("13015100", "Campinas", "SP", false);
            ad.SetAttributes(new AdAttributes().Set("conditionId", 2));
            if (withPhoto)
            {
                db.AdPhotos.Add(new AdPhoto { AdId = id, SortOrder = 0, StorageKey = $"{id}/foto", Width = 100, Height = 80, SizeBytes = 3 });
            }

            await db.SaveChangesAsync();
            return 0;
        });
        return id;
    }

    private static Task<HttpResponseMessage> SaveAsync(DraftSite site, int id, params (string Name, string Value)[] fields)
        => DraftSite.PostAsync(site.Admin, $"/painel/anuncios/{id}/editar", $"/painel/anuncios/{id}/editar", fields);

    private static (string, string)[] Complete(string title = "Bicicleta aro 29 revisada") =>
    [
        ("Title", title), ("Description", "Pouco usada"), ("CategoryId", GeneralProducts.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        ("Price", "R$ 1.500,00"), ("Cep", "13015-100"), ("Fields[conditionId]", "2")
    ];

    [TestMethod]
    public async Task US008S15_AdminSavingAPublishedAd_WithoutDescriptionPriceCategoryOrCep_IsRefused_WithThePendingMessages_AndNothingChanges()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await PublishedCompleteAsync(site);
        int auditBefore = (await site.AuditAsync()).Count;

        HttpResponseMessage saved = await SaveAsync(site, id, ("Title", "Só o título mudou"));
        string page = await DraftSite.BodyAsync(saved);

        Assert.AreEqual(HttpStatusCode.OK, saved.StatusCode, "a página do formulário volta, com os erros");
        StringAssert.Contains(page, AdMessages.CategoryRequired);
        StringAssert.Contains(page, AdMessages.CepRequired);
        Ad ad = await site.LoadAsync(id);
        Assert.AreEqual("Bicicleta aro 29", ad.Title, "nada foi gravado");
        Assert.AreEqual("Pouco usada", ad.Description);
        Assert.AreEqual(GeneralProducts, ad.CategoryId);
        Assert.AreEqual(AdStatus.Published, ad.Status);
        Assert.HasCount(auditBefore, await site.AuditAsync(), "e nada foi auditado");
    }

    [TestMethod]
    public async Task AdminSavingAPublishedAd_WithoutDescription_IsRefused()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await PublishedCompleteAsync(site);
        (string, string)[] withoutDescription = [.. Complete().Where(f => f.Item1 != "Description")];

        HttpResponseMessage saved = await SaveAsync(site, id, withoutDescription);

        Assert.AreEqual(HttpStatusCode.OK, saved.StatusCode);
        StringAssert.Contains(await DraftSite.BodyAsync(saved), AdMessages.DescriptionRequired("Descrição"));
        Assert.AreEqual("Pouco usada", (await site.LoadAsync(id)).Description, "a descrição não foi apagada");
    }

    [TestMethod]
    public async Task AdminSavingAPublishedAd_WithTheRequiredFieldEmptied_IsRefused()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await PublishedCompleteAsync(site);
        (string, string)[] withoutCondition = [.. Complete().Where(f => f.Item1 != "Fields[conditionId]")];

        HttpResponseMessage saved = await SaveAsync(site, id, withoutCondition);

        Assert.AreEqual(HttpStatusCode.OK, saved.StatusCode);
        StringAssert.Contains(await DraftSite.BodyAsync(saved), "Informe a condição");
        Assert.IsTrue(AdAttributes.TryParse((await site.LoadAsync(id)).Attributes, out AdAttributes stored) && stored.TryGetInt("conditionId", out int condition) && condition == 2, "a condição continua gravada");
    }

    [TestMethod]
    public async Task US008S15_AdminSavingAPublishedAd_ThatHasNoPhoto_IsRefused_WithThePhotoMessage()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await PublishedCompleteAsync(site, withPhoto: false);

        HttpResponseMessage saved = await SaveAsync(site, id, Complete());

        Assert.AreEqual(HttpStatusCode.OK, saved.StatusCode);
        StringAssert.Contains(await DraftSite.BodyAsync(saved), AdMessages.PhotosRequired(1));
        Assert.AreEqual("Bicicleta aro 29", (await site.LoadAsync(id)).Title);
    }

    [TestMethod]
    public async Task AdminSavingACompletePublishedAd_Saves_AndItStaysPublished()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await PublishedCompleteAsync(site);

        HttpResponseMessage saved = await SaveAsync(site, id, Complete());

        Assert.AreEqual(HttpStatusCode.Redirect, saved.StatusCode);
        Ad ad = await site.LoadAsync(id);
        Assert.AreEqual("Bicicleta aro 29 revisada", ad.Title);
        Assert.AreEqual(150_000L, ad.PriceCents);
        Assert.AreEqual(AdStatus.Published, ad.Status);
    }

    // R-04: preço com dígito de outro alfabeto ("1,\u0665") dava FormatException e a tela respondia 500; agora volta o formulário com a mensagem de preço inválido
    [TestMethod]
    [DataRow("1,\u0665")]
    [DataRow("\u0661\u0662\u0663")]
    public async Task APriceWithADigitFromAnotherAlphabet_ComesBackAsAFormMessage_NotAServerError(string price)
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await PublishedCompleteAsync(site);

        HttpResponseMessage saved = await SaveAsync(site, id, ("Title", "Bicicleta"), ("Description", "Pouco usada"), ("CategoryId", "86"), ("Price", price), ("Cep", "13015-100"), ("Fields[conditionId]", "2"));

        Assert.AreEqual(HttpStatusCode.OK, saved.StatusCode);
        StringAssert.Contains(await DraftSite.BodyAsync(saved), AdMessages.PriceInvalid);
        Assert.AreEqual(150_000L, (await site.LoadAsync(id)).PriceCents, "nada foi gravado");
    }

    [TestMethod]
    public async Task TheWriterSavingADraft_StillSavesIncomplete()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await site.AddAdAsync(Writer, AdStatus.Draft, "Rascunho");

        HttpResponseMessage saved = await DraftSite.PostAsync(site.Writer, $"/painel/anuncios/{id}/editar", $"/painel/anuncios/{id}/editar", ("Title", "Rascunho com novo título"));

        Assert.AreEqual(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.AreEqual("Rascunho com novo título", (await site.LoadAsync(id)).Title);
    }
}
