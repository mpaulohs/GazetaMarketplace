using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using ImageMagick;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Photos;

/// <summary>
/// US-008-S16 (R-02b): um anúncio publicado não pode ficar sem foto. Como o Administrador não salva um Publicado que o envio à revisão recusaria (S15), também não remove a última foto dele; é preciso
/// despublicar antes. Rascunho e Rejeitado continuam podendo ficar sem foto.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PublishedPhotoTests
#pragma warning restore CA1515
{
    private const string Admin = PanelFixture.AdminEmail;
    private const string Writer = PanelFixture.WriterEmail;

    [TestMethod]
    public async Task US008S16_AdminRemoveAUltimaFotoDeUmPublicado_E_Recusado_ComAMensagem_ENadaMuda()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, int photoId, StoredPhoto stored) = await photos.AddPhotoAsync(Admin, AdStatus.Published);

        using HttpResponseMessage response = await PhotoApi.DeleteAsync(photos.Site.Admin, adId, photoId);

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.AreEqual(PhotoMessages.LastPhotoOfPublished, (await PhotoApi.ProblemAsync(response)).GetProperty("detail").GetString());
        Assert.HasCount(1, await photos.RowsAsync(adId), "a foto continua no banco");
        Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(photos.Folder, stored.StorageKey + "_480.webp")), "e os arquivos continuam no disco");
        Assert.AreEqual(AdStatus.Published, (await photos.Site.LoadAsync(adId)).Status);
    }

    [TestMethod]
    public async Task AdminRemoveUmaFotoDeUmPublicadoComDuas_Pode_ASegundaNao()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, int firstId, _) = await photos.AddPhotoAsync(Admin, AdStatus.Published);
        string token = await PhotoApi.TokenAsync(photos.Site.Admin);
        using HttpResponseMessage upload = await PhotoApi.UploadAsync(photos.Site.Admin, adId, PhotoApi.Jpeg(MagickColors.Green), token);
        int secondId = (await PhotoApi.ProblemAsync(upload)).GetProperty("id").GetInt32();
        Assert.HasCount(2, await photos.RowsAsync(adId));

        using HttpResponseMessage first = await PhotoApi.DeleteAsync(photos.Site.Admin, adId, firstId, token);
        using HttpResponseMessage second = await PhotoApi.DeleteAsync(photos.Site.Admin, adId, secondId, token);

        Assert.AreEqual(HttpStatusCode.NoContent, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, second.StatusCode, "sobrou uma só: é a última");
        Assert.AreEqual(secondId, (await photos.RowsAsync(adId)).Single().Id);
    }

    [TestMethod]
    [DataRow(AdStatus.Draft)]
    [DataRow(AdStatus.Rejected)]
    public async Task RascunhoERejeitado_PodemFicarSemFoto(byte status)
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, int photoId, _) = await photos.AddPhotoAsync(Writer, status);

        using HttpResponseMessage response = await PhotoApi.DeleteAsync(photos.Site.Admin, adId, photoId);

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        Assert.IsEmpty(await photos.RowsAsync(adId));
    }

    [TestMethod]
    public async Task SemJavaScript_RemoverAUltimaFotoDeUmPublicado_VoltaComOAviso_ENadaMuda()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, int photoId, _) = await photos.AddPhotoAsync(Admin, AdStatus.Published);

        HttpResponseMessage removed = await DraftSite.PostAsync(photos.Site.Admin, $"/painel/anuncios/{adId}/editar", $"/painel/anuncios/{adId}/fotos/{photoId}/remover");

        Assert.AreEqual(HttpStatusCode.Redirect, removed.StatusCode);
        string page = await DraftSite.BodyAsync(await photos.Site.Admin.GetAsync($"/painel/anuncios/{adId}/editar"));
        StringAssert.Contains(page, PhotoMessages.LastPhotoOfPublished);
        Assert.HasCount(1, await photos.RowsAsync(adId));
    }
}
