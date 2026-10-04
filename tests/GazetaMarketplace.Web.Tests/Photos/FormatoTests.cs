using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Photos;
using ImageMagick;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Photos;

/// <summary>O formato é decidido pelo conteúdo do arquivo (NFR-12), nunca pelo nome ou pela extensão; HEIC é convertido para WebP.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class FormatoTests
#pragma warning restore CA1515
{
    private static PhotoFormat? Detect(byte[] bytes) => PhotoSignature.Detect(bytes.AsSpan(0, Math.Min(bytes.Length, PhotoSignature.HeaderLength)));

    private static byte[] Ftyp(string major, params string[] compatible)
    {
        byte[] body = [.. Encoding.ASCII.GetBytes("ftyp" + major), 0, 0, 0, 0, .. compatible.SelectMany(c => Encoding.ASCII.GetBytes(c))];
        int size = body.Length + 4;
        return [(byte)(size >> 24), (byte)(size >> 16), (byte)(size >> 8), (byte)size, .. body, .. new byte[40]];
    }

    private static async Task<ValidationException> RefusedAsync(PhotoFixtures.MagickImageProcessorHolder holder, byte[] bytes)
    {
        PhotoIngestion ingestion = new(holder.Processor, holder.Storage, TimeProvider.System);
        return await Assert.ThrowsExactlyAsync<ValidationException>(() => ingestion.IngestAsync(1, new MemoryStream(bytes), CancellationToken.None));
    }

    [TestMethod]
    public void Assinatura_DecideOFormato_NaoAExtensao()
    {
        Assert.AreEqual(PhotoFormat.Jpeg, Detect([0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10]));
        Assert.AreEqual(PhotoFormat.Png, Detect([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0]));
        Assert.AreEqual(PhotoFormat.Gif, Detect(Encoding.ASCII.GetBytes("GIF89a....")));
        Assert.AreEqual(PhotoFormat.Gif, Detect(Encoding.ASCII.GetBytes("GIF87a....")));
        Assert.AreEqual(PhotoFormat.WebP, Detect([.. Encoding.ASCII.GetBytes("RIFF"), 1, 2, 3, 4, .. Encoding.ASCII.GetBytes("WEBPVP8 ")]));
        Assert.AreEqual(PhotoFormat.Heic, Detect(Ftyp("heic", "mif1", "heic")));
        Assert.AreEqual(PhotoFormat.Heic, Detect(Ftyp("heix")));
        Assert.AreEqual(PhotoFormat.Heic, Detect(Ftyp("mif1", "mif1", "heic")), "marca genérica com marca de HEIC entre as compatíveis");
        Assert.AreEqual(PhotoFormat.Heic, Detect(Ftyp("msf1", "msf1", "hevc")));
    }

    [TestMethod]
    public void QuemNaoEUmDosCincoFormatos_NaoTemFormato()
    {
        Assert.IsNull(Detect(Encoding.ASCII.GetBytes("%PDF-1.7 ...")));
        Assert.IsNull(Detect(Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"/>")));
        Assert.IsNull(Detect(Encoding.ASCII.GetBytes("push graphic-context\nviewbox 0 0 10 10")));
        Assert.IsNull(Detect(Encoding.ASCII.GetBytes("RIFF....WAVEfmt ")), "RIFF que não é WebP");
        Assert.IsNull(Detect(Ftyp("avif", "mif1", "avif")), "AVIF não é aceito");
        Assert.IsNull(Detect(Ftyp("mif1", "mif1", "miaf")), "marca genérica sem marca de HEIC");
        Assert.IsNull(Detect(Ftyp("isom", "isom", "mp42")), "vídeo MP4");
        Assert.IsNull(Detect([0xFF, 0xD8]), "JPEG cortado antes do terceiro byte");
        Assert.IsNull(Detect([]));
        Assert.IsNull(Detect([0x89, 0x50, 0x4E, 0x47]), "PNG cortado");
    }

    [TestMethod]
    public async Task ArquivoFalsoComExtensaoJpg_ERecusado_ENadaVaiParaODisco()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();

        // A extensão nem chega ao site (só o conteúdo); estes são os conteúdos que um ".jpg" falso teria
        ValidationException pdf = await RefusedAsync(holder, Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n<<>>\nendobj"));
        ValidationException text = await RefusedAsync(holder, Encoding.UTF8.GetBytes("isto não é uma foto"));
        ValidationException svg = await RefusedAsync(holder, Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg' width='10' height='10'><rect width='10' height='10'/></svg>"));

        foreach (ValidationException error in new[] { pdf, text, svg })
        {
            Assert.AreEqual("Formato não aceito. Use JPG, PNG, WebP, GIF ou HEIC", error.Errors["file"][0]);
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), holder.Files());
    }

    [TestMethod]
    public async Task Original_GuardaAExtensaoDoFormatoDetectado()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        PhotoIngestion ingestion = new(holder.Processor, holder.Storage, TimeProvider.System);

        // PNG de verdade: o original sai como .png, seja qual for o nome que o navegador enviou
        StoredPhoto stored = await ingestion.IngestAsync(7, new MemoryStream(PhotoFixtures.Solid(MagickFormat.Png, 50, 40)), CancellationToken.None);

        StringAssert.EndsWith(stored.OriginalKey, ".png");
        StringAssert.StartsWith(stored.OriginalKey, "_originals/");
        Assert.IsTrue(File.Exists(Path.Combine(holder.Folder, stored.OriginalKey)));
    }

    [TestMethod]
    [DataRow("jpeg")]
    [DataRow("png")]
    [DataRow("gif")]
    [DataRow("webp")]
    [DataRow("heic")]
    public void OsCincoFormatos_ViramWebP(string kind)
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        (byte[] bytes, PhotoFormat format) = kind switch
        {
            "jpeg" => (PhotoFixtures.JpegWithGps(80, 60), PhotoFormat.Jpeg),
            "png" => (PhotoFixtures.Solid(MagickFormat.Png, 80, 60), PhotoFormat.Png),
            "gif" => (PhotoFixtures.Solid(MagickFormat.Gif, 80, 60), PhotoFormat.Gif),
            "webp" => (PhotoFixtures.Solid(MagickFormat.WebP, 80, 60), PhotoFormat.WebP),
            _ => (PhotoFixtures.Heic(), PhotoFormat.Heic)
        };

        ProcessedImage result = holder.Processor.Process(bytes, format);

        Assert.AreEqual(PhotoFormat.WebP, Detect(result.Large));
        Assert.AreEqual(PhotoFormat.WebP, Detect(result.Thumb));
        Assert.IsGreaterThan(0, result.Width);
        Assert.IsGreaterThan(0, result.Height);
    }

    [TestMethod]
    public void Heic_E_ConvertidoParaWebP_ComAsDimensoesDoOriginal()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        byte[] heic = PhotoFixtures.Heic();
        Assert.AreEqual(PhotoFormat.Heic, Detect(heic), "o arquivo de teste tem assinatura de HEIC");

        ProcessedImage result = holder.Processor.Process(heic, PhotoFormat.Heic);

        Assert.AreEqual(64, result.Width);
        Assert.AreEqual(48, result.Height);
        using MagickImage large = new(result.Large);
        Assert.AreEqual(MagickFormat.WebP, large.Format);
        Assert.AreEqual(64u, large.Width);
        Assert.AreEqual(48u, large.Height);
    }
}
