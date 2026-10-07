using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Photos;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Support;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Photos;

/// <summary>Chamadas da API de fotos e atalhos para olhar o que ficou no banco e no disco.</summary>
internal static class PhotoApi
{
    public const string Pdf = "Formato não aceito. Use JPG, PNG, WebP, GIF ou HEIC";

    public const string TooBig = "A foto excede o limite de 10 MB";

    public static string Url(int adId) => $"/api/v1/ads/{adId}/photos";

    public static async Task<string> TokenAsync(HttpClient client)
    {
        string page = await client.GetStringAsync("/painel/anuncios");
        return Regex.Match(page, @"name=""request-verification-token""[^>]*content=""([^""]+)""").Groups[1].Value;
    }

    public static async Task<HttpResponseMessage> UploadAsync(HttpClient client, int adId, byte[] content, string token = null, bool withToken = true, string fileName = "foto.jpg", string field = "file")
    {
        if (withToken)
        {
            token ??= await TokenAsync(client);
        }

        using MultipartFormDataContent form = new();
        ByteArrayContent file = new(content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, field, fileName);
        using HttpRequestMessage request = new(HttpMethod.Post, Url(adId)) { Content = form };
        if (withToken)
        {
            request.Headers.Add("RequestVerificationToken", token);
        }

        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string url, string token = null)
    {
        using HttpRequestMessage request = new(method, url);
        request.Headers.Add("RequestVerificationToken", token ?? await TokenAsync(client));
        return await client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> CoverAsync(HttpClient client, int adId, int photoId, string token = null) =>
        SendAsync(client, HttpMethod.Post, $"{Url(adId)}/{photoId}/cover", token);

    public static Task<HttpResponseMessage> DeleteAsync(HttpClient client, int adId, int photoId, string token = null) =>
        SendAsync(client, HttpMethod.Delete, $"{Url(adId)}/{photoId}", token);

    public static byte[] Jpeg(MagickColor color = null) => PhotoFixtures.Solid(MagickFormat.Jpeg, 800, 600, color);

    public static async Task<JsonElement> ProblemAsync(HttpResponseMessage response)
    {
        using JsonDocument document = await CepHarness.JsonAsync(response);
        return document.RootElement.Clone();
    }

    public static Task<List<AdPhoto>> RowsAsync(this PhotoSite site, int adId) =>
        site.Site.Harness.WithDbAsync(db => db.AdPhotos.AsNoTracking().Where(p => p.AdId == adId).OrderBy(p => p.SortOrder).ToListAsync());

    /// <summary>Tudo o que há no disco da pasta de fotos, menos a configuração do ImageMagick.</summary>
    public static string[] DiskFiles(this PhotoSite site) =>
        [.. Directory.EnumerateFiles(site.Folder, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(site.Folder, f).Replace('\\', '/'))
            .Where(f => !f.StartsWith("_magick/", StringComparison.Ordinal))];

    /// <summary>Põe <paramref name="count"/> fotos no anúncio direto no banco (sem arquivos), para testar o limite sem processar 20 imagens.</summary>
    public static Task AddRowsAsync(this PhotoSite site, int adId, int count) => site.Site.Harness.WithDbAsync(async db =>
    {
        for (int i = 0; i < count; i++)
        {
            db.AdPhotos.Add(new AdPhoto { AdId = adId, SortOrder = i, StorageKey = $"{adId}/{Guid.NewGuid():N}", Width = 1, Height = 1, SizeBytes = 1 });
        }

        await db.SaveChangesAsync();
        return 0;
    });
}

/// <summary>Enviar, trocar a capa e remover fotos pela API (US-008-S02 a S06, NFR-12), com o servidor de verdade, o ImageMagick de verdade e a pasta de fotos em disco.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PhotosEndpointsTests
#pragma warning restore CA1515
{
    private const string Writer = PanelFixture.WriterEmail;
    private const int Services = 66;
    private const int Jobs = 96;

    [TestMethod]
    public async Task US008S02_AdicionarFotosAoAnuncio_GravaNaOrdemEnviada_APrimeiraECapa_ESobrevivemAoReabrir()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);
        string token = await PhotoApi.TokenAsync(photos.Site.Writer);
        List<int> ids = [];

        foreach (MagickColor color in new[] { MagickColors.Red, MagickColors.Green, MagickColors.Blue })
        {
            using HttpResponseMessage created = await PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg(color), token);
            Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
            JsonElement body = await PhotoApi.ProblemAsync(created);
            ids.Add(body.GetProperty("id").GetInt32());
            Assert.AreEqual(ids.Count - 1, body.GetProperty("sortOrder").GetInt32(), "cada foto entra no fim");
            Assert.AreEqual($"/fotos/{adId}/{ids[^1]}-480.webp", body.GetProperty("url480").GetString());
            Assert.AreEqual($"/fotos/{adId}/{ids[^1]}-1600.webp", body.GetProperty("url1600").GetString());
            Assert.AreEqual(800, body.GetProperty("width").GetInt32());
            Assert.AreEqual(600, body.GetProperty("height").GetInt32());
        }

        List<AdPhoto> rows = await photos.RowsAsync(adId);
        CollectionAssert.AreEqual(ids, rows.Select(r => r.Id).ToList(), "a ordem de gravação é a ordem enviada");
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, rows.Select(r => r.SortOrder).ToArray());
        foreach (AdPhoto row in rows)
        {
            Assert.IsTrue(File.Exists(Path.Combine(photos.Folder, row.StorageKey + "_480.webp")), "a miniatura está no disco");
            Assert.IsTrue(File.Exists(Path.Combine(photos.Folder, row.StorageKey + "_1600.webp")), "a versão grande está no disco");
            Assert.IsTrue(File.Exists(Path.Combine(photos.Folder, row.OriginalKey)), "o original também fica, para a limpeza de 30 dias");
        }

        // Ao salvar o rascunho e reabrir, as fotos continuam, na mesma ordem (D5)
        string page = await DraftSite.BodyAsync(await photos.Site.Writer.GetAsync($"/painel/anuncios/{adId}/editar"));
        StringAssert.Contains(page, "Fotos (3 de 20)");
        int[] shown = [.. Regex.Matches(page, @"data-photo-id=""(\d+)""").Select(m => int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))];
        CollectionAssert.AreEqual(ids, shown, "a página mostra as miniaturas na ordem enviada");
        string list = page[page.IndexOf("data-photo-list", StringComparison.Ordinal)..page.IndexOf("</ol>", StringComparison.Ordinal)];
        Assert.AreEqual(1, Regex.Matches(list, @"fotos__capa""[^>]*>").Count(m => !m.Value.Contains("hidden", StringComparison.Ordinal)), "só a primeira tem a marca Capa");
        Assert.IsTrue(Regex.IsMatch(list, $@"data-photo-id=""{ids[0]}""[\s\S]*?fotos__capa""\s+data-show-on-cover\s*>Capa"), "a marca está na primeira");
    }

    [TestMethod]
    public async Task US008S03_TrocarACapaERemoverUmaFoto()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);
        string token = await PhotoApi.TokenAsync(photos.Site.Writer);
        List<int> ids = [];
        foreach (MagickColor color in new[] { MagickColors.Red, MagickColors.Green, MagickColors.Blue })
        {
            ids.Add((await PhotoApi.ProblemAsync(await PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg(color), token))).GetProperty("id").GetInt32());
        }

        // "Tornar capa" na 3ª: ela vai para a primeira posição; as outras descem, na mesma ordem
        using HttpResponseMessage cover = await PhotoApi.CoverAsync(photos.Site.Writer, adId, ids[2], token);
        Assert.AreEqual(HttpStatusCode.NoContent, cover.StatusCode);
        List<AdPhoto> afterCover = await photos.RowsAsync(adId);
        CollectionAssert.AreEqual(new[] { ids[2], ids[0], ids[1] }, afterCover.Select(r => r.Id).ToArray());
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, afterCover.Select(r => r.SortOrder).ToArray());

        // "Remover" na 2ª (agora a foto ids[0]): o rascunho passa a ter 2 fotos e as posições ficam sem buraco
        AdPhoto removed = afterCover[1];
        using HttpResponseMessage delete = await PhotoApi.DeleteAsync(photos.Site.Writer, adId, removed.Id, token);
        Assert.AreEqual(HttpStatusCode.NoContent, delete.StatusCode);
        List<AdPhoto> afterDelete = await photos.RowsAsync(adId);
        CollectionAssert.AreEqual(new[] { ids[2], ids[1] }, afterDelete.Select(r => r.Id).ToArray());
        CollectionAssert.AreEqual(new[] { 0, 1 }, afterDelete.Select(r => r.SortOrder).ToArray(), "as posições se renumeram");
        Assert.IsFalse(File.Exists(Path.Combine(photos.Folder, removed.StorageKey + "_480.webp")), "D3: as duas versões WebP são apagadas");
        Assert.IsFalse(File.Exists(Path.Combine(photos.Folder, removed.StorageKey + "_1600.webp")));
        Assert.IsFalse(File.Exists(Path.Combine(photos.Folder, removed.OriginalKey)), "SC-07: o original (com o GPS do vendedor) some junto com a foto, na hora");
        Assert.AreEqual(photos.DiskFiles().Length, afterDelete.Count * 3, "as fotos que ficaram mantêm os três arquivos (duas versões e o original)");
        using HttpResponseMessage gone = await photos.Site.Writer.GetAsync(PhotoSite.Url(adId, removed.Id));
        Assert.AreEqual(HttpStatusCode.NotFound, gone.StatusCode, "a URL da foto removida deixa de funcionar");
    }

    [TestMethod]
    public async Task RemoverACapa_APrimeiraDasOutrasPassaASerCapa_ETornarCapaDaCapaNaoMudaNada()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);
        string token = await PhotoApi.TokenAsync(photos.Site.Writer);
        List<int> ids = [];
        for (int i = 0; i < 3; i++)
        {
            ids.Add((await PhotoApi.ProblemAsync(await PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg(), token))).GetProperty("id").GetInt32());
        }

        Assert.AreEqual(HttpStatusCode.NoContent, (await PhotoApi.CoverAsync(photos.Site.Writer, adId, ids[0], token)).StatusCode);
        CollectionAssert.AreEqual(ids, (await photos.RowsAsync(adId)).Select(r => r.Id).ToList(), "a capa já é a primeira: nada muda");

        Assert.AreEqual(HttpStatusCode.NoContent, (await PhotoApi.DeleteAsync(photos.Site.Writer, adId, ids[0], token)).StatusCode);
        List<AdPhoto> rows = await photos.RowsAsync(adId);
        CollectionAssert.AreEqual(new[] { ids[1], ids[2] }, rows.Select(r => r.Id).ToArray());
        Assert.AreEqual(0, rows[0].SortOrder, "a próxima assume a capa, sem lacuna");
    }

    [TestMethod]
    public async Task US008S04_PassarDoLimiteDe20Fotos_Devolve409ComAMensagem_ENadaEGravado()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);
        await photos.AddRowsAsync(adId, 20);

        using HttpResponseMessage response = await PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg());

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        JsonElement problem = await PhotoApi.ProblemAsync(response);
        Assert.AreEqual("CONFLICT", problem.GetProperty("code").GetString());
        Assert.AreEqual("Cada anúncio pode ter no máximo 20 fotos", problem.GetProperty("detail").GetString());
        Assert.AreEqual(20, (await photos.RowsAsync(adId)).Count, "o anúncio continua com 20 fotos");
        Assert.AreEqual(0, photos.DiskFiles().Length, "a recusa vem antes de processar: nenhum arquivo é gravado");
    }

    [TestMethod]
    public async Task LimiteDe20_AVigesimaFotoEntra_AVigesimaPrimeiraNao()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);
        await photos.AddRowsAsync(adId, 19);

        using HttpResponseMessage twentieth = await PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg());
        using HttpResponseMessage twentyFirst = await PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg());

        Assert.AreEqual(HttpStatusCode.Created, twentieth.StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, twentyFirst.StatusCode);
        Assert.AreEqual(20, (await photos.RowsAsync(adId)).Count);
    }

    [TestMethod]
    public async Task Servicos_AceitamNoMaximo6Fotos()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer, categoryId: Services);
        await photos.AddRowsAsync(adId, 6);

        using HttpResponseMessage response = await PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg());

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.AreEqual("Cada anúncio pode ter no máximo 6 fotos", (await PhotoApi.ProblemAsync(response)).GetProperty("detail").GetString());
    }

    [TestMethod]
    public async Task VagasDeEmprego_NaoTemFotos_OEnvioEhRecusado()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer, categoryId: Jobs);

        using HttpResponseMessage response = await PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg());

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.AreEqual("Este tipo de anúncio não tem fotos", (await PhotoApi.ProblemAsync(response)).GetProperty("detail").GetString());
        Assert.AreEqual(0, (await photos.RowsAsync(adId)).Count);
    }

    [TestMethod]
    public async Task US008S05_PdfEFotoDe15MB_SaoRecusadosComAsMensagensDaSpec_ENenhumEhAdicionado()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);
        string token = await PhotoApi.TokenAsync(photos.Site.Writer);
        byte[] pdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\n");
        byte[] fifteenMb = new byte[15 * 1024 * 1024];
        fifteenMb[0] = 0xFF;
        fifteenMb[1] = 0xD8;

        using HttpResponseMessage pdfResponse = await PhotoApi.UploadAsync(photos.Site.Writer, adId, pdf, token, fileName: "contrato.pdf");
        using HttpResponseMessage bigResponse = await PhotoApi.UploadAsync(photos.Site.Writer, adId, fifteenMb, token);

        Assert.AreEqual(HttpStatusCode.BadRequest, pdfResponse.StatusCode);
        JsonElement pdfProblem = await PhotoApi.ProblemAsync(pdfResponse);
        Assert.AreEqual("VALIDATION_ERROR", pdfProblem.GetProperty("code").GetString());
        Assert.AreEqual(PhotoApi.Pdf, pdfProblem.GetProperty("errors").GetProperty("file")[0].GetString());
        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, bigResponse.StatusCode, "acima de 11 MB o servidor recusa antes de ler o corpo");
        Assert.AreEqual(0, (await photos.RowsAsync(adId)).Count, "nenhum dos dois é adicionado");
        Assert.AreEqual(0, photos.DiskFiles().Length, "e nada é gravado em disco");
    }

    [TestMethod]
    public async Task ArquivoDe10a11MB_RecebeAMensagemDeValidacao_NaoUm413()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);
        byte[] content = new byte[(10 * 1024 * 1024) + 1024]; // 10 MB e 1 KB: passa pelo limite do servidor (11 MB), não pelo da aplicação (10 MB)
        content[0] = 0xFF;
        content[1] = 0xD8;

        using HttpResponseMessage response = await PhotoApi.UploadAsync(photos.Site.Writer, adId, content);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(PhotoApi.TooBig, (await PhotoApi.ProblemAsync(response)).GetProperty("errors").GetProperty("file")[0].GetString());
        Assert.AreEqual(0, photos.DiskFiles().Length);
    }

    [TestMethod]
    public async Task ArquivoVazio_ESemOCampoFile_Devolvem400()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);
        string token = await PhotoApi.TokenAsync(photos.Site.Writer);

        using HttpResponseMessage empty = await PhotoApi.UploadAsync(photos.Site.Writer, adId, [], token);
        using HttpResponseMessage wrongField = await PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg(), token, field: "outro");

        Assert.AreEqual(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, wrongField.StatusCode);
        Assert.AreEqual("VALIDATION_ERROR", (await PhotoApi.ProblemAsync(wrongField)).GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task US008S06_FalhaDeGravacao_NaoDeixaArquivosNemRegistro()
    {
        // Simula a pior hora: a foto já foi processada e gravada em disco quando o registro no banco falha (aqui, o anúncio some no meio)
        using PhotoSite photos = await PhotoSite.StartAsync(services: services =>
        {
            services.RemoveAll<IPhotoIngestion>();
            services.AddScoped<PhotoIngestion>();
            services.AddScoped<IPhotoIngestion>(sp => new DeletesAdAfterIngestion(sp.GetRequiredService<PhotoIngestion>(), sp));
        });
        int adId = await photos.Site.AddAdAsync(Writer);

        using HttpResponseMessage response = await PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg());

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.AreEqual(0, photos.DiskFiles().Length, "os arquivos da foto que não foi registrada são apagados, o original também");
        Assert.AreEqual(0, await photos.Site.Harness.WithDbAsync(db => db.AdPhotos.CountAsync()));
    }

    [TestMethod]
    public async Task SemToken_Devolve400_ESemLogin_Devolve401()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);
        int photoId = (await PhotoApi.ProblemAsync(await PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg()))).GetProperty("id").GetInt32();
        using HttpClient anonymous = photos.Site.Harness.Anonymous();

        using HttpResponseMessage noToken = await PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg(), withToken: false);
        using HttpRequestMessage coverNoToken = new(HttpMethod.Post, $"{PhotoApi.Url(adId)}/{photoId}/cover");
        using HttpResponseMessage coverResponse = await photos.Site.Writer.SendAsync(coverNoToken);
        using HttpRequestMessage deleteNoToken = new(HttpMethod.Delete, $"{PhotoApi.Url(adId)}/{photoId}");
        using HttpResponseMessage deleteResponse = await photos.Site.Writer.SendAsync(deleteNoToken);
        using HttpResponseMessage anonymousUpload = await PhotoApi.UploadAsync(anonymous, adId, PhotoApi.Jpeg(), token: "qualquer");

        Assert.AreEqual(HttpStatusCode.BadRequest, noToken.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, coverResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousUpload.StatusCode);
        Assert.AreEqual(1, (await photos.RowsAsync(adId)).Count, "as recusas não mexem em nada");
    }

    [TestMethod]
    public async Task Autorizacao_OutroRedatorEmRevisaoEPublicado_Devolvem403_OAdministradorEdita_AnuncioInexistente404()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        await photos.Site.Harness.Factory.CreateUserAsync("outro.redator@exemplo.com.br", "Outro Redator", PanelFixture.Password, RoleNames.Writer);
        using HttpClient stranger = await PanelFixture.SignedInAsync(photos.Site.Harness.Factory, "outro.redator@exemplo.com.br");
        int draft = await photos.Site.AddAdAsync(Writer);
        int inReview = await photos.Site.AddAdAsync(Writer, AdStatus.InReview);
        int published = await photos.Site.AddAdAsync(Writer, AdStatus.Published);
        int own = (await PhotoApi.ProblemAsync(await PhotoApi.UploadAsync(photos.Site.Writer, draft, PhotoApi.Jpeg()))).GetProperty("id").GetInt32();

        using HttpResponseMessage otherUpload = await PhotoApi.UploadAsync(stranger, draft, PhotoApi.Jpeg());
        using HttpResponseMessage otherCover = await PhotoApi.CoverAsync(stranger, draft, own);
        using HttpResponseMessage otherDelete = await PhotoApi.DeleteAsync(stranger, draft, own);
        using HttpResponseMessage reviewUpload = await PhotoApi.UploadAsync(photos.Site.Writer, inReview, PhotoApi.Jpeg());
        using HttpResponseMessage publishedUpload = await PhotoApi.UploadAsync(photos.Site.Writer, published, PhotoApi.Jpeg());
        using HttpResponseMessage missingAd = await PhotoApi.UploadAsync(photos.Site.Writer, 99999, PhotoApi.Jpeg());
        using HttpResponseMessage adminUpload = await PhotoApi.UploadAsync(photos.Site.Admin, draft, PhotoApi.Jpeg());

        foreach (HttpResponseMessage denied in new[] { otherUpload, otherCover, otherDelete, reviewUpload, publishedUpload })
        {
            Assert.AreEqual(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.AreEqual("FORBIDDEN", (await PhotoApi.ProblemAsync(denied)).GetProperty("code").GetString());
        }

        Assert.AreEqual(HttpStatusCode.NotFound, missingAd.StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, adminUpload.StatusCode, "o Administrador também edita o rascunho de outra pessoa");
        Assert.AreEqual(2, (await photos.RowsAsync(draft)).Count, "só a foto do autor e a do Administrador");
        Assert.AreEqual(0, (await photos.RowsAsync(inReview)).Count);
    }

    [TestMethod]
    public async Task CapaERemocao_ComFotoDeOutroAnuncioOuInexistente_Devolvem404()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int first = await photos.Site.AddAdAsync(Writer);
        int second = await photos.Site.AddAdAsync(Writer);
        int photoOfFirst = (await PhotoApi.ProblemAsync(await PhotoApi.UploadAsync(photos.Site.Writer, first, PhotoApi.Jpeg()))).GetProperty("id").GetInt32();

        using HttpResponseMessage wrongAdCover = await PhotoApi.CoverAsync(photos.Site.Writer, second, photoOfFirst);
        using HttpResponseMessage wrongAdDelete = await PhotoApi.DeleteAsync(photos.Site.Writer, second, photoOfFirst);
        using HttpResponseMessage missingCover = await PhotoApi.CoverAsync(photos.Site.Writer, first, 99999);
        using HttpResponseMessage missingDelete = await PhotoApi.DeleteAsync(photos.Site.Writer, first, 99999);

        foreach (HttpResponseMessage response in new[] { wrongAdCover, wrongAdDelete, missingCover, missingDelete })
        {
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        Assert.AreEqual(1, (await photos.RowsAsync(first)).Count, "a foto do primeiro anúncio continua lá");
    }

    [TestMethod]
    public async Task Envio_31oNaMesmaJanelaDeUmMinuto_Devolve429ComRetryAfter_SoParaAqueleUsuario()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);
        string token = await PhotoApi.TokenAsync(photos.Site.Writer);
        string adminToken = await PhotoApi.TokenAsync(photos.Site.Admin);

        for (int i = 1; i <= 30; i++)
        {
            using HttpResponseMessage allowed = await PhotoApi.UploadAsync(photos.Site.Writer, adId, [], token); // vazio: recusado pelo serviço, mas conta no limite
            Assert.AreEqual(HttpStatusCode.BadRequest, allowed.StatusCode, $"o envio {i} ainda passa pelo limite");
        }

        using HttpResponseMessage limited = await PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg(), token);
        using HttpResponseMessage otherUser = await PhotoApi.UploadAsync(photos.Site.Admin, adId, PhotoApi.Jpeg(), adminToken);

        Assert.AreEqual(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.AreEqual("RATE_LIMITED", (await PhotoApi.ProblemAsync(limited)).GetProperty("code").GetString());
        Assert.IsTrue(limited.Headers.Contains("Retry-After"), "diz quando tentar de novo");
        Assert.AreEqual(HttpStatusCode.Created, otherUser.StatusCode, "o limite é por usuário: o Administrador não é afetado");
        Assert.AreEqual(1, (await photos.RowsAsync(adId)).Count, "só a foto do Administrador");
    }

    [TestMethod]
    public async Task EnviosSimultaneos_NoMesmoAnuncio_GravamPosicoesDiferentes()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        int adId = await photos.Site.AddAdAsync(Writer);
        string token = await PhotoApi.TokenAsync(photos.Site.Writer);

        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => PhotoApi.UploadAsync(photos.Site.Writer, adId, PhotoApi.Jpeg(), token)));

        // No SQLite do teste uma gravação de cada vez passa; o bloqueio de verdade (UPDLOCK) é provado no SQL Server, em PhotoConcurrencyTests
        int created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        List<AdPhoto> rows = await photos.RowsAsync(adId);
        Assert.AreEqual(created, rows.Count, "cada resposta 201 tem uma linha");
        Assert.AreEqual(rows.Count, rows.Select(r => r.StorageKey).Distinct().Count(), "chaves diferentes");
        Assert.AreEqual(photos.DiskFiles().Length, rows.Count * 3, "três arquivos por foto: duas versões e o original, nenhum sobrando");
        foreach (HttpResponseMessage response in responses)
        {
            response.Dispose();
        }
    }

    /// <summary>Faz o anúncio sumir depois de a foto estar em disco, para o registro falhar.</summary>
    private sealed class DeletesAdAfterIngestion(IPhotoIngestion inner, IServiceProvider services) : IPhotoIngestion
    {
        public async Task<StoredPhoto> IngestAsync(int adId, Stream content, CancellationToken cancellationToken)
        {
            StoredPhoto stored = await inner.IngestAsync(adId, content, cancellationToken);
            using IServiceScope scope = services.CreateScope();
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Ads.Where(a => a.Id == adId).ExecuteDeleteAsync(cancellationToken);
            return stored;
        }
    }
}
