using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Photos;

/// <summary>Site de teste com a pasta de fotos de verdade (uma pasta temporária) e atalhos para pôr uma foto num anúncio.</summary>
internal sealed class PhotoSite : IDisposable
{
    private PhotoSite(DraftSite site, string folder)
    {
        Site = site;
        Folder = folder;
    }

    public DraftSite Site { get; }

    public string Folder { get; }

    public static async Task<PhotoSite> StartAsync(int? requestsPerMinute = null)
    {
        PhotoFixtures.Init(); // o ImageMagick liga uma vez com a política de produção antes de qualquer outro uso
        string folder = Path.Combine(Path.GetTempPath(), "gazeta-entrega-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return new PhotoSite(await DraftSite.StartAsync(requestsPerMinute, folder), folder);
    }

    /// <summary>Cria um anúncio na situação pedida e uma foto de verdade nele (processada e gravada pelo site).</summary>
    public async Task<(int AdId, int PhotoId, StoredPhoto Stored)> AddPhotoAsync(string authorEmail, byte status)
    {
        int adId = await Site.AddAdAsync(authorEmail, status);
        using IServiceScope scope = Site.Harness.Factory.Services.CreateScope();
        IPhotoIngestion ingestion = scope.ServiceProvider.GetRequiredService<IPhotoIngestion>();
        StoredPhoto stored = await ingestion.IngestAsync(adId, new MemoryStream(PhotoFixtures.Solid(MagickFormat.Jpeg, 2000, 1000)), CancellationToken.None);

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        AdPhoto photo = new() { AdId = adId, SortOrder = 0, StorageKey = stored.StorageKey, OriginalKey = stored.OriginalKey, Width = stored.Width, Height = stored.Height, SizeBytes = stored.SizeBytes };
        db.AdPhotos.Add(photo);
        await db.SaveChangesAsync();
        return (adId, photo.Id, stored);
    }

    public static string Url(int adId, int photoId, string size = "480") => $"/fotos/{adId}/{photoId}-{size}.webp";

    public void Dispose()
    {
        Site.Dispose();
        try
        {
            Directory.Delete(Folder, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}

/// <summary>A rota <c>/fotos/{adId}/{photoId}-{480|1600}.webp</c> (ADR-005): cache longo só no publicado, 404 igual para "não existe" e "não pode ver", e nada de <c>_originals/</c>.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class EntregaTests
#pragma warning restore CA1515
{
    private const string Writer = PanelFixture.WriterEmail;

    private static async Task<(HttpStatusCode Status, string Cache, string Type, int Length)> ProbeAsync(HttpClient client, string url)
    {
        using HttpResponseMessage response = await client.GetAsync(url);
        byte[] body = await response.Content.ReadAsByteArrayAsync();
        return (response.StatusCode, string.Join(",", response.Headers.CacheControl?.ToString() ?? string.Empty), response.Content.Headers.ContentType?.ToString() ?? string.Empty, body.Length);
    }

    [TestMethod]
    public async Task Publicado_TemCacheLongo_EEntregaAsDuasVersoes()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, int photoId, StoredPhoto stored) = await photos.AddPhotoAsync(Writer, AdStatus.Published);
        using HttpClient anonymous = photos.Site.Harness.Anonymous();

        using HttpResponseMessage thumb = await anonymous.GetAsync(PhotoSite.Url(adId, photoId, "480"));
        using HttpResponseMessage large = await anonymous.GetAsync(PhotoSite.Url(adId, photoId, "1600"));

        Assert.AreEqual(HttpStatusCode.OK, thumb.StatusCode);
        Assert.AreEqual("image/webp", thumb.Content.Headers.ContentType?.MediaType);
        Assert.AreEqual("public, max-age=31536000, immutable", thumb.Headers.GetValues("Cache-Control").Single());
        Assert.AreEqual("public, max-age=31536000, immutable", large.Headers.GetValues("Cache-Control").Single());
        CollectionAssert.AreEqual(await File.ReadAllBytesAsync(Path.Combine(photos.Folder, stored.StorageKey + "_480.webp")), await thumb.Content.ReadAsByteArrayAsync());
        CollectionAssert.AreEqual(await File.ReadAllBytesAsync(Path.Combine(photos.Folder, stored.StorageKey + "_1600.webp")), await large.Content.ReadAsByteArrayAsync());
        using MagickImage image = new(await thumb.Content.ReadAsByteArrayAsync());
        Assert.AreEqual(480u, image.Width);
    }

    [TestMethod]
    [DataRow(AdStatus.Draft)]
    [DataRow(AdStatus.InReview)]
    [DataRow(AdStatus.Rejected)]
    [DataRow(AdStatus.Archived)]
    public async Task AnuncioNaoPublicado_Devolve404IgualAoInexistente_ParaQuemNaoPodeVer(byte status)
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, int photoId, _) = await photos.AddPhotoAsync(Writer, status);
        await photos.Site.Harness.Factory.CreateUserAsync("outro.redator@exemplo.com.br", "Outro Redator", PanelFixture.Password, RoleNames.Writer);
        using HttpClient anonymous = photos.Site.Harness.Anonymous();
        using HttpClient stranger = await PanelFixture.SignedInAsync(photos.Site.Harness.Factory, "outro.redator@exemplo.com.br");

        var missing = await ProbeAsync(anonymous, PhotoSite.Url(adId, photoId + 1000));
        var asAnonymous = await ProbeAsync(anonymous, PhotoSite.Url(adId, photoId));
        var asStranger = await ProbeAsync(stranger, PhotoSite.Url(adId, photoId));

        Assert.AreEqual(HttpStatusCode.NotFound, missing.Status);
        Assert.AreEqual(missing, asAnonymous, "o anônimo não distingue 'não existe' de 'não pode ver'");
        Assert.AreEqual(missing, asStranger, "outro Redator também não");
    }

    [TestMethod]
    [DataRow(AdStatus.Draft)]
    [DataRow(AdStatus.InReview)]
    [DataRow(AdStatus.Rejected)]
    [DataRow(AdStatus.Archived)]
    public async Task AnuncioNaoPublicado_OAutorEOAdministradorVeem_SemCachePublico(byte status)
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, int photoId, _) = await photos.AddPhotoAsync(Writer, status);

        var asAuthor = await ProbeAsync(photos.Site.Writer, PhotoSite.Url(adId, photoId));
        var asAdmin = await ProbeAsync(photos.Site.Admin, PhotoSite.Url(adId, photoId, "1600"));

        foreach (var seen in new[] { asAuthor, asAdmin })
        {
            Assert.AreEqual(HttpStatusCode.OK, seen.Status);
            Assert.AreEqual("image/webp", seen.Type);
            CollectionAssert.AreEquivalent(new[] { "no-store", "private" }, seen.Cache.Split(',', StringSplitOptions.TrimEntries), "nunca fica no cache público");
        }
    }

    [TestMethod]
    public async Task FotoDeOutroAnuncio_Devolve404_MesmoPublicado()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, int photoId, _) = await photos.AddPhotoAsync(Writer, AdStatus.Published);
        (int otherAd, _, _) = await photos.AddPhotoAsync(Writer, AdStatus.Published);
        using HttpClient anonymous = photos.Site.Harness.Anonymous();

        var wrongAd = await ProbeAsync(anonymous, PhotoSite.Url(otherAd, photoId));
        var right = await ProbeAsync(anonymous, PhotoSite.Url(adId, photoId));

        Assert.AreEqual(HttpStatusCode.NotFound, wrongAd.Status);
        Assert.AreEqual(HttpStatusCode.OK, right.Status);
    }

    [TestMethod]
    public async Task OriginaisNaoTemRota()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, int photoId, StoredPhoto stored) = await photos.AddPhotoAsync(Writer, AdStatus.Published);
        Assert.IsTrue(File.Exists(Path.Combine(photos.Folder, stored.OriginalKey)), "o original existe no disco");
        using HttpClient anonymous = photos.Site.Harness.Anonymous();
        string guid = Path.GetFileNameWithoutExtension(stored.OriginalKey);
        string[] attempts =
        [
            "/" + stored.OriginalKey,
            "/fotos/" + stored.OriginalKey,
            "/fotos/_originals/" + Path.GetFileName(stored.OriginalKey),
            $"/fotos/{adId}/{guid}.jpg",
            $"/fotos/{adId}/{photoId}-original.webp",
            $"/fotos/{adId}/{photoId}-0.webp",
            "/_originals/" + Path.GetFileName(stored.OriginalKey),
            "/fotos/_originals",
            "/fotos/%5Foriginals/" + Path.GetFileName(stored.OriginalKey)
        ];

        foreach (string url in attempts)
        {
            var seen = await ProbeAsync(anonymous, url);
            Assert.AreEqual(HttpStatusCode.NotFound, seen.Status, url);
        }
    }

    [TestMethod]
    public async Task TentativaDeSairDaPastaBase_E_Recusada()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, int photoId, _) = await photos.AddPhotoAsync(Writer, AdStatus.Published);
        using HttpClient anonymous = photos.Site.Harness.Anonymous();
        string[] attempts =
        [
            "/fotos/1/..%2F..%2Fappsettings.json",
            "/fotos/..%2F..%2Fappsettings.json",
            $"/fotos/{adId}/..%2F{photoId}-480.webp",
            $"/fotos/{adId}/{photoId}-..%2F..%2F480.webp",
            $"/fotos/{adId}/{photoId}-480.webp/../../x",
            "/fotos/%2e%2e/%2e%2e/etc/passwd",
        ];

        foreach (string url in attempts)
        {
            var seen = await ProbeAsync(anonymous, url);
            Assert.IsTrue(seen.Status is HttpStatusCode.NotFound or HttpStatusCode.BadRequest, $"{url} → {(int)seen.Status}");
            Assert.AreNotEqual("image/webp", seen.Type, url);
        }
    }

    [TestMethod]
    public async Task RotaComIdNaoNumerico_Devolve404()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, int photoId, _) = await photos.AddPhotoAsync(Writer, AdStatus.Published);
        using HttpClient anonymous = photos.Site.Harness.Anonymous();
        string[] attempts =
        [
            "/fotos/abc/1-480.webp",
            $"/fotos/{adId}/x-480.webp",
            $"/fotos/{adId}/{photoId}-abc.webp",
            $"/fotos/{adId}/{photoId}-300.webp",
            $"/fotos/{adId}/{photoId}-4800.webp",
            $"/fotos/{adId}/{photoId}-.webp",
            $"/fotos/{adId}/{photoId}-480.jpg",
            $"/fotos/-1/{photoId}-480.webp",
            $"/fotos/{adId}/-1-480.webp",
            $"/fotos/{adId}/1.5-480.webp",
            "/fotos/99999999999/1-480.webp",
            $"/fotos/{adId}/{photoId}"
        ];

        foreach (string url in attempts)
        {
            var seen = await ProbeAsync(anonymous, url);
            Assert.AreEqual(HttpStatusCode.NotFound, seen.Status, url);
        }

        Assert.AreEqual(HttpStatusCode.OK, (await ProbeAsync(anonymous, PhotoSite.Url(adId, photoId))).Status, "a rota boa continua funcionando");
    }

    [TestMethod]
    public async Task ArquivoQueSumiuDoDisco_Devolve404_NaoErro500()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, int photoId, StoredPhoto stored) = await photos.AddPhotoAsync(Writer, AdStatus.Published);
        File.Delete(Path.Combine(photos.Folder, stored.StorageKey + "_480.webp"));
        using HttpClient anonymous = photos.Site.Harness.Anonymous();

        Assert.AreEqual(HttpStatusCode.NotFound, (await ProbeAsync(anonymous, PhotoSite.Url(adId, photoId, "480"))).Status);
        Assert.AreEqual(HttpStatusCode.OK, (await ProbeAsync(anonymous, PhotoSite.Url(adId, photoId, "1600"))).Status);
    }

    [TestMethod]
    public async Task Fotos_TemLimiteProprioDe300PorMinutoPorIp_ENaoContamNoLimiteGlobalDe100()
    {
        using PhotoSite photos = await PhotoSite.StartAsync(); // limite global padrão: 100 por minuto
        (int adId, int photoId, _) = await photos.AddPhotoAsync(Writer, AdStatus.Published);
        using HttpClient anonymous = photos.Site.Harness.Anonymous();

        // 300 pedidos de foto passam, bem acima dos 100 do limite global
        for (int i = 1; i <= 300; i++)
        {
            using HttpResponseMessage ok = await anonymous.GetAsync(PhotoSite.Url(adId, photoId));
            Assert.AreEqual(HttpStatusCode.OK, ok.StatusCode, $"pedido {i}");
        }

        using HttpResponseMessage blocked = await anonymous.GetAsync(PhotoSite.Url(adId, photoId));
        using HttpResponseMessage page = await anonymous.GetAsync("/painel/entrar");

        Assert.AreEqual(HttpStatusCode.TooManyRequests, blocked.StatusCode, "o 301º pedido de foto é recusado");
        Assert.IsTrue(blocked.Headers.Contains("Retry-After"));
        Assert.AreEqual(HttpStatusCode.OK, page.StatusCode, "o resto do site não foi afetado: as fotos não gastaram o limite global");
    }

    [TestMethod]
    public async Task OutroIp_NaoEAfetadoPeloLimiteDeFotosDoPrimeiro()
    {
        using PhotoSite photos = await PhotoSite.StartAsync();
        (int adId, int photoId, _) = await photos.AddPhotoAsync(Writer, AdStatus.Published);
        using HttpClient first = photos.Site.Harness.Anonymous();
        using HttpClient second = photos.Site.Harness.Anonymous();

        for (int i = 0; i < 301; i++)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, PhotoSite.Url(adId, photoId));
            request.Headers.Add(Support.WebFactory.RemoteIpHeader, "203.0.113.10");
            using HttpResponseMessage response = await first.SendAsync(request);
        }

        using HttpRequestMessage other = new(HttpMethod.Get, PhotoSite.Url(adId, photoId));
        other.Headers.Add(Support.WebFactory.RemoteIpHeader, "203.0.113.20");
        using HttpResponseMessage fromOther = await second.SendAsync(other);
        using HttpRequestMessage again = new(HttpMethod.Get, PhotoSite.Url(adId, photoId));
        again.Headers.Add(Support.WebFactory.RemoteIpHeader, "203.0.113.10");
        using HttpResponseMessage fromFirst = await first.SendAsync(again);

        Assert.AreEqual(HttpStatusCode.OK, fromOther.StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, fromFirst.StatusCode);
    }
}
