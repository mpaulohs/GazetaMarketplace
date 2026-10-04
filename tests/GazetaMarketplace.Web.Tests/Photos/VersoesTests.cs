using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Photos;
using ImageMagick;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Photos;

/// <summary>As duas versões WebP (1.600 e 480 px, qualidade 80), a orientação pelo EXIF e a regra de nunca ampliar.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class VersoesTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void Gera1600e480_Webp()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();

        ProcessedImage result = holder.Processor.Process(PhotoFixtures.Solid(MagickFormat.Jpeg, 3000, 2000), PhotoFormat.Jpeg);

        using MagickImage large = new(result.Large);
        using MagickImage thumb = new(result.Thumb);
        Assert.AreEqual(MagickFormat.WebP, large.Format);
        Assert.AreEqual(MagickFormat.WebP, thumb.Format);
        Assert.AreEqual(1600u, large.Width);
        Assert.AreEqual(1067u, large.Height, "a proporção se mantém: 3000 × 2000 → 1600 × 1067");
        Assert.AreEqual(480u, thumb.Width);
        Assert.AreEqual(320u, thumb.Height);
        Assert.AreEqual(1600, result.Width);
        Assert.AreEqual(1067, result.Height);
    }

    [TestMethod]
    public void NuncaAmplia_FotoPequenaMantemOTamanhoNasDuasVersoes()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();

        ProcessedImage result = holder.Processor.Process(PhotoFixtures.Solid(MagickFormat.Png, 300, 200), PhotoFormat.Png);

        using MagickImage large = new(result.Large);
        using MagickImage thumb = new(result.Thumb);
        Assert.AreEqual(300u, large.Width, "menor que 1600: não é ampliada");
        Assert.AreEqual(300u, thumb.Width, "menor que 480: a miniatura também não é ampliada");
        Assert.AreEqual(200u, thumb.Height);
    }

    [TestMethod]
    public void FotoMedia_SoAMiniaturaEncolhe()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();

        ProcessedImage result = holder.Processor.Process(PhotoFixtures.Solid(MagickFormat.Jpeg, 1000, 500), PhotoFormat.Jpeg);

        using MagickImage large = new(result.Large);
        using MagickImage thumb = new(result.Thumb);
        Assert.AreEqual(1000u, large.Width);
        Assert.AreEqual(480u, thumb.Width);
        Assert.AreEqual(240u, thumb.Height);
    }

    [TestMethod]
    public void Qualidade80_ProduzOMesmoArquivoQueOWebPFeitoDiretoNa80_EMenorQueNa95()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        byte[] source = PhotoFixtures.NoisyJpeg(400, 300);

        ProcessedImage result = holder.Processor.Process(source, PhotoFormat.Jpeg);

        byte[] Encode(uint quality)
        {
            using MagickImage image = new(source, new MagickReadSettings { Format = MagickFormat.Jpeg });
            image.Strip();
            image.Format = MagickFormat.WebP;
            image.Quality = quality;
            return image.ToByteArray();
        }

        CollectionAssert.AreEqual(Encode(80), result.Large, "é exatamente a codificação em qualidade 80");
        Assert.IsLessThan(Encode(95).Length, result.Large.Length, "qualidade 80 é menor que 95");
    }

    [TestMethod]
    public void Orientacao_DoExif_EAplicadaNaImagem()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();

        // 40 × 20 com orientação "girada 90°" (6): de pé, a foto é 20 × 40
        byte[] source = PhotoFixtures.JpegWithGps(40, 20, orientation: 6);
        using (MagickImage check = new(source))
        {
            Assert.AreEqual(OrientationType.RightTop, check.Orientation, "o arquivo de teste está mesmo marcado como girado");
            Assert.AreEqual(40u, check.Width);
        }

        ProcessedImage result = holder.Processor.Process(source, PhotoFormat.Jpeg);

        Assert.AreEqual(20, result.Width);
        Assert.AreEqual(40, result.Height);
        using MagickImage large = new(result.Large);
        Assert.AreEqual(20u, large.Width);
        Assert.AreEqual(40u, large.Height);
    }

    [TestMethod]
    public void GifAnimado_ViraOPrimeiroQuadro_ImagemEstatica()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        byte[] gif = PhotoFixtures.AnimatedGif();
        using (MagickImageCollection original = new(gif))
        {
            Assert.AreEqual(3, original.Count, "o GIF de teste tem 3 quadros");
        }

        ProcessedImage result = holder.Processor.Process(gif, PhotoFormat.Gif);

        using MagickImageCollection frames = new(result.Large);
        Assert.AreEqual(1, frames.Count);
        IMagickColor<byte> pixel = frames[0].GetPixels().GetPixel(5, 5).ToColor();
        Assert.IsTrue(pixel.R > 200 && pixel.G < 60 && pixel.B < 60, "é o primeiro quadro (vermelho)");
    }

    [TestMethod]
    public void PngComTransparencia_MantemOAlpha()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();

        ProcessedImage result = holder.Processor.Process(PhotoFixtures.Solid(MagickFormat.Png, 50, 50, MagickColors.Transparent), PhotoFormat.Png);

        using MagickImage large = new(result.Large);
        Assert.IsTrue(large.HasAlpha);
        Assert.AreEqual(0, large.GetPixels().GetPixel(10, 10).ToColor().A, "continua transparente");
    }

    [TestMethod]
    public async Task Ingestao_GravaAsDuasVersoesNosNomesCombinados_EGuardaOTamanhoDaGrande()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        PhotoIngestion ingestion = new(holder.Processor, holder.Storage, TimeProvider.System);

        StoredPhoto stored = await ingestion.IngestAsync(42, new MemoryStream(PhotoFixtures.Solid(MagickFormat.Jpeg, 2400, 1600)), CancellationToken.None);

        Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(stored.StorageKey, "^42/[0-9a-f]{32}$"), stored.StorageKey);
        string[] files = holder.Files();
        Assert.HasCount(3, files);
        CollectionAssert.Contains(files, stored.StorageKey + "_1600.webp");
        CollectionAssert.Contains(files, stored.StorageKey + "_480.webp");
        Assert.IsTrue(files.Any(f => f == stored.OriginalKey));
        Assert.AreEqual(1600, stored.Width);
        Assert.AreEqual(1067, stored.Height);
        Assert.AreEqual(new FileInfo(Path.Combine(holder.Folder, stored.StorageKey + "_1600.webp")).Length, stored.SizeBytes);
        Assert.AreEqual(2400u, new MagickImage(File.ReadAllBytes(Path.Combine(holder.Folder, stored.OriginalKey))).Width, "o original fica como foi enviado");
        Assert.IsFalse(files.Any(f => f.EndsWith(".tmp", StringComparison.Ordinal)), "nenhum temporário sobra");
    }

    [TestMethod]
    public async Task EnviosAoMesmoTempo_NoMesmoAnuncio_GravamChavesDiferentes_SemSobrescrever()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        PhotoIngestion ingestion = new(holder.Processor, holder.Storage, TimeProvider.System);
        byte[] bytes = PhotoFixtures.Solid(MagickFormat.Jpeg, 800, 600);

        StoredPhoto[] stored = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => ingestion.IngestAsync(5, new MemoryStream(bytes), CancellationToken.None)));

        Assert.AreEqual(8, stored.Select(s => s.StorageKey).Distinct().Count());
        Assert.AreEqual(8, stored.Select(s => s.OriginalKey).Distinct().Count());
        Assert.HasCount(24, holder.Files(), "8 fotos × (original + 2 versões)");
        foreach (StoredPhoto photo in stored)
        {
            await using Stream opened = await holder.Storage.OpenAsync(photo.StorageKey, PhotoSize.Large, CancellationToken.None);
            Assert.IsNotNull(opened);
            Assert.IsGreaterThan(0, opened.Length);
        }
    }
}
