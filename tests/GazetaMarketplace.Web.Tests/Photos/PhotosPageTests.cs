using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Support;
using ImageMagick;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Photos;

/// <summary>A seção de fotos da página do anúncio e o caminho <b>sem JavaScript</b> (formulários comuns, uma foto por vez): o que a página mostra e o que cada POST faz.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PhotosPageTests
#pragma warning restore CA1515
{
    private const string Writer = PanelFixture.WriterEmail;

    private static string EditUrl(int adId) => $"/painel/anuncios/{adId}/editar";

    private static async Task<string> PageAsync(HttpClient client, int adId) => await DraftSite.BodyAsync(await client.GetAsync(EditUrl(adId)));

    /// <summary>POST multipart da página (sem JavaScript): o token vai no campo do formulário, como o navegador enviaria.</summary>
    private static async Task<HttpResponseMessage> PostUploadAsync(HttpClient client, int adId, byte[] content, string fileName = "foto.jpg")
    {
        string page = await client.GetStringAsync(EditUrl(adId));
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        using MultipartFormDataContent form = new();
        form.Add(new StringContent(token), "__RequestVerificationToken");
        form.Add(new ByteArrayContent(content), "file", fileName);
        return await client.PostAsync($"/painel/anuncios/{adId}/fotos", form);
    }

    private static async Task<HttpResponseMessage> PostActionAsync(HttpClient client, int adId, string path)
    {
        string page = await client.GetStringAsync(EditUrl(adId));
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        return await client.PostAsync($"/painel/anuncios/{adId}/fotos/{path}", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
    }

    [TestMethod]
    public async Task AnuncioNovo_AindaNaoTemFotos_PedeParaSalvarOrascunhoPrimeiro()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();

        string page = await DraftSite.BodyAsync(await photos.Site.Writer.GetAsync("/painel/anuncios/novo"));

        StringAssert.Contains(page, "Salve o rascunho primeiro para poder enviá-las");
        Assert.IsFalse(page.Contains("data-photo-upload", StringComparison.Ordinal), "sem anúncio gravado não há onde enviar");
        Assert.IsFalse(page.Contains("data-photos", StringComparison.Ordinal), "e não há seção de galeria");
    }

    [TestMethod]
    [DataRow(null, "Fotos (0 de 20)")]
    [DataRow(33, "Fotos (0 de 20)")]
    [DataRow(66, "Fotos (0 de 6)")]
    public async Task RascunhoGravado_MostraOLimiteDeFotosDaCategoria_EOFormularioDeEnvio(int? categoryId, string heading)
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer, categoryId: categoryId);

        string page = await PageAsync(photos.Site.Writer, adId);

        StringAssert.Contains(page, heading);
        StringAssert.Contains(page, "Enviar fotos");
        StringAssert.Contains(page, "enctype=\"multipart/form-data\"");
        StringAssert.Contains(page, "Este anúncio ainda não tem fotos");
        StringAssert.Contains(page, "data-upload-url=\"/api/v1/ads/" + adId + "/photos\"");
    }

    [TestMethod]
    public async Task VagasDeEmprego_NaoTemSecaoDeEnvio()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer, categoryId: 96);

        string page = await PageAsync(photos.Site.Writer, adId);

        StringAssert.Contains(page, "Vagas de emprego não têm fotos");
        Assert.IsFalse(page.Contains("data-photo-upload", StringComparison.Ordinal));
        Assert.IsFalse(page.Contains("Fotos (", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task OsBotoesDeSalvarFicamForaDoFormulario_EAindaEnviamOFormularioDoAnuncio()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);

        string page = await PageAsync(photos.Site.Writer, adId);

        string formEnd = page[page.IndexOf("id=\"anuncio-form\"", StringComparison.Ordinal)..];
        formEnd = formEnd[..formEnd.IndexOf("</form>", StringComparison.Ordinal)];
        Assert.IsFalse(formEnd.Contains("data-submit-button", StringComparison.Ordinal), "o botão não fica dentro do formulário (as fotos têm formulários próprios entre os dois)");
        Assert.IsTrue(Regex.IsMatch(page, @"<button[^>]*form=""anuncio-form""[^>]*data-submit-button"), "mas se liga a ele pelo atributo form");
        Assert.AreEqual(1, Regex.Matches(page, @"<form[^>]*id=""anuncio-form""").Count);
    }

    [TestMethod]
    public async Task SemJavaScript_EnviarVariasFotosUmaPorVez_MostraAMensagemEAsMiniaturasNaOrdem()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);

        HttpResponseMessage first = await PostUploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg(MagickColors.Red));
        Assert.AreEqual(HttpStatusCode.Redirect, first.StatusCode);
        Assert.AreEqual(EditUrl(adId), first.Destination());
        string afterFirst = await PageAsync(photos.Site.Writer, adId);
        StringAssert.Contains(afterFirst, "Foto adicionada.");
        StringAssert.Contains(afterFirst, "Fotos (1 de 20)");

        await PostUploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg(MagickColors.Green));
        string afterSecond = await PageAsync(photos.Site.Writer, adId);

        StringAssert.Contains(afterSecond, "Fotos (2 de 20)");
        Assert.AreEqual(2, (await photos.RowsAsync(adId)).Count);
        StringAssert.Contains(afterSecond, "alt=\"Foto 1\"");
        StringAssert.Contains(afterSecond, "alt=\"Foto 2\"");
        StringAssert.Contains(afterSecond, "Foto adicionada.");
        Assert.IsFalse((await PageAsync(photos.Site.Writer, adId)).Contains("Foto adicionada.", StringComparison.Ordinal), "a mensagem aparece uma vez só");
    }

    [TestMethod]
    public async Task SemJavaScript_ArquivoRecusado_VoltaComAMensagemNaSecaoDeFotos()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);

        HttpResponseMessage response = await PostUploadAsync(photos.Site.Writer, adId, System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 conteudo"), "contrato.pdf");
        string page = await PageAsync(photos.Site.Writer, adId);

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.IsTrue(Regex.IsMatch(page, @"role=""alert""[^>]*data-photo-error[^>]*>" + Regex.Escape(PhotoApi.Pdf)), "a recusa aparece na seção, como alerta");
        Assert.AreEqual(0, (await photos.RowsAsync(adId)).Count);
    }

    [TestMethod]
    public async Task SemJavaScript_PostSemArquivo_VoltaPedindoUmaFoto()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);
        string page = await photos.Site.Writer.GetStringAsync(EditUrl(adId));
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

        HttpResponseMessage response = await photos.Site.Writer.PostAsync($"/painel/anuncios/{adId}/fotos",
            new MultipartFormDataContent { { new StringContent(token), "__RequestVerificationToken" } });
        string after = await PageAsync(photos.Site.Writer, adId);

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        StringAssert.Contains(after, "Escolha uma foto para enviar");
    }

    [TestMethod]
    public async Task SemJavaScript_TornarCapa_EReordena_ERemoverPedeConfirmacaoEmPaginaPropria()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);
        foreach (MagickColor color in new[] { MagickColors.Red, MagickColors.Green, MagickColors.Blue })
        {
            await PostUploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg(color));
        }

        List<AdPhoto> rows = await photos.RowsAsync(adId);
        HttpResponseMessage cover = await PostActionAsync(photos.Site.Writer, adId, $"{rows[2].Id}/capa");
        Assert.AreEqual(HttpStatusCode.Redirect, cover.StatusCode);
        CollectionAssert.AreEqual(new[] { rows[2].Id, rows[0].Id, rows[1].Id }, (await photos.RowsAsync(adId)).Select(r => r.Id).ToArray());
        StringAssert.Contains(await PageAsync(photos.Site.Writer, adId), "Capa alterada.");

        // Remover: primeiro a página de confirmação (GET não remove nada), depois o POST
        string confirmation = await DraftSite.BodyAsync(await photos.Site.Writer.GetAsync($"/painel/anuncios/{adId}/fotos/{rows[0].Id}/remover"));
        StringAssert.Contains(confirmation, "Remover esta foto?");
        Assert.AreEqual(3, (await photos.RowsAsync(adId)).Count, "só olhar a confirmação não remove");
        HttpResponseMessage remove = await PostActionAsync(photos.Site.Writer, adId, $"{rows[0].Id}/remover");
        Assert.AreEqual(HttpStatusCode.Redirect, remove.StatusCode);
        Assert.AreEqual(2, (await photos.RowsAsync(adId)).Count);
        StringAssert.Contains(await PageAsync(photos.Site.Writer, adId), "Foto removida.");
    }

    [TestMethod]
    public async Task SemJavaScript_OutroRedator_Recebe403_EFotoInexistente404()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        await photos.Site.Harness.Factory.CreateUserAsync("outro.redator@exemplo.com.br", "Outro Redator", PanelFixture.Password, RoleNames.Writer);
        using HttpClient stranger = await PanelFixture.SignedInAsync(photos.Site.Harness.Factory, "outro.redator@exemplo.com.br");
        int adId = await photos.Site.AddAdAsync(Writer);
        await PostUploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg());
        AdPhoto row = (await photos.RowsAsync(adId)).Single();

        HttpResponseMessage confirmByStranger = await stranger.GetAsync($"/painel/anuncios/{adId}/fotos/{row.Id}/remover");
        HttpResponseMessage missing = await photos.Site.Writer.GetAsync($"/painel/anuncios/{adId}/fotos/99999/remover");
        string strangerPage = await DraftSite.BodyAsync(await stranger.GetAsync(EditUrl(adId)));

        Assert.AreEqual(HttpStatusCode.Forbidden, confirmByStranger.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.IsFalse(strangerPage.Contains("data-photo-id", StringComparison.Ordinal), "quem não pode ver o anúncio não vê as fotos");
        Assert.AreEqual(1, (await photos.RowsAsync(adId)).Count);
    }

    [TestMethod]
    public async Task AnuncioEmRevisao_MostraAsFotosSoParaLeitura_SemEnvioNemBotoes()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, _, _) = await photos.AddPhotoAsync(Writer, AdStatus.InReview);

        string page = await PageAsync(photos.Site.Writer, adId);

        StringAssert.Contains(page, "data-read-only");
        StringAssert.Contains(page, "Fotos (1 de 20)");
        StringAssert.Contains(page, "data-photo-id");
        Assert.IsFalse(page.Contains("data-photo-upload", StringComparison.Ordinal), "sem formulário de envio");
        Assert.IsFalse(page.Contains("data-action=", StringComparison.Ordinal), "sem Tornar capa nem Remover");
    }

    [TestMethod]
    public async Task SemJavaScript_AnuncioEmRevisao_NaoAceitaEnvioPelaPagina()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer, AdStatus.InReview);
        string page = await photos.Site.Writer.GetStringAsync("/painel/anuncios");
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        using MultipartFormDataContent form = new();
        form.Add(new StringContent(token), "__RequestVerificationToken");
        form.Add(new ByteArrayContent(PhotoApi.Jpeg()), "file", "foto.jpg");

        HttpResponseMessage response = await photos.Site.Writer.PostAsync($"/painel/anuncios/{adId}/fotos", form);

        Assert.IsTrue(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.BadRequest, "recusado: com o token da página ou sem ele");
        Assert.AreEqual(0, (await photos.RowsAsync(adId)).Count);
    }
}
