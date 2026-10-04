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

/// <summary>S18: nada que identifique a pessoa (GPS, câmera, descrição) sobrevive nas versões publicadas. O original guarda tudo, mas nenhuma rota o serve.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class MetadadosTests
#pragma warning restore CA1515
{
    private static bool Contains(byte[] haystack, string text) =>
        Encoding.ASCII.GetString(haystack).Contains(text, StringComparison.Ordinal);

    [TestMethod]
    public void OFixtureTemMesmoGpsNoExif_AntesDeProcessar()
    {
        // Sem isto, o teste do "depois" passaria por a foto nunca ter tido GPS
        byte[] source = PhotoFixtures.JpegWithGps(100, 80);

        using MagickImage image = new(source);
        IExifProfile exif = image.GetExifProfile();
        Assert.IsNotNull(exif);
        Assert.IsNotNull(exif.GetValue(ExifTag.GPSLatitude), "o JPEG de teste tem latitude");
        Assert.IsNotNull(exif.GetValue(ExifTag.GPSLongitude), "o JPEG de teste tem longitude");
        Assert.IsTrue(Contains(source, PhotoFixtures.Secret));
    }

    [TestMethod]
    public void Gps_NaoSobrevive_NasVersoes()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();

        ProcessedImage result = holder.Processor.Process(PhotoFixtures.JpegWithGps(2000, 1500), PhotoFormat.Jpeg);

        foreach (byte[] version in new[] { result.Large, result.Thumb })
        {
            using MagickImage image = new(version);
            IExifProfile exif = image.GetExifProfile();
            Assert.IsTrue(exif is null || (exif.GetValue(ExifTag.GPSLatitude) is null && exif.GetValue(ExifTag.GPSLongitude) is null && exif.GetValue(ExifTag.Make) is null),
                "sem GPS nem fabricante no EXIF");
            Assert.IsNull(image.GetXmpProfile(), "sem XMP");
            Assert.IsFalse(Contains(version, PhotoFixtures.Secret), "o texto do EXIF não aparece em lugar nenhum do arquivo");
            Assert.IsFalse(Contains(version, "CameraSecreta"));
            Assert.IsFalse(Contains(version, "EXIF"), "nem o bloco EXIF existe");
        }
    }

    [TestMethod]
    public async Task NoArquivoGravado_OGpsTambemNaoEstaNasVersoes_SoNoOriginal()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        PhotoIngestion ingestion = new(holder.Processor, holder.Storage, TimeProvider.System);

        StoredPhoto stored = await ingestion.IngestAsync(3, new MemoryStream(PhotoFixtures.JpegWithGps(1200, 900)), CancellationToken.None);

        byte[] large = await File.ReadAllBytesAsync(Path.Combine(holder.Folder, stored.StorageKey + "_1600.webp"));
        byte[] thumb = await File.ReadAllBytesAsync(Path.Combine(holder.Folder, stored.StorageKey + "_480.webp"));
        byte[] original = await File.ReadAllBytesAsync(Path.Combine(holder.Folder, stored.OriginalKey));
        Assert.IsFalse(Contains(large, PhotoFixtures.Secret));
        Assert.IsFalse(Contains(thumb, PhotoFixtures.Secret));
        Assert.IsTrue(Contains(original, PhotoFixtures.Secret), "o original fica como veio, por isso nenhuma rota o serve (ADR-005)");
    }
}
