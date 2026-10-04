using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Infrastructure.Photos;
using ImageMagick;
using ImageMagick.Formats;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Tests.Photos;

/// <summary>
/// Imagens de teste feitas na hora. O ImageMagick é ligado uma vez por processo com a <b>política de produção</b>; o único formato a mais é <c>XC</c> (cor sólida),
/// que só serve para criar as imagens daqui e nunca é usado para ler um arquivo enviado.
/// </summary>
internal static class PhotoFixtures
{
    public const string Secret = "SEGREDO-NA-FOTO";

    static PhotoFixtures() => MagickRuntime.EnsureInitialized(Path.Combine(Path.GetTempPath(), "gazeta-photo-tests"), "XC");

    /// <summary>Garante que o ImageMagick foi ligado com a política antes de qualquer leitura.</summary>
    public static void Init()
    {
    }

    public static MagickImageProcessorHolder NewProcessor() => new();

    /// <summary>O processador de verdade, com uma pasta de fotos própria do teste.</summary>
    public sealed class MagickImageProcessorHolder : IDisposable
    {
        public MagickImageProcessorHolder()
        {
            Init();
            Folder = Path.Combine(Path.GetTempPath(), "gazeta-fotos-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Folder);
            Options = Microsoft.Extensions.Options.Options.Create(new PhotoStorageOptions { BasePath = Folder });
            Processor = new MagickImageProcessor(Options);
            Storage = new FileSystemPhotoStorage(Options);
        }

        public string Folder { get; }

        public IOptions<PhotoStorageOptions> Options { get; }

        public MagickImageProcessor Processor { get; }

        public FileSystemPhotoStorage Storage { get; }

        /// <summary>Todos os arquivos gravados pelo site (sem a configuração do ImageMagick), com o caminho relativo.</summary>
        public string[] Files() => [.. Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(Folder, f).Replace('\\', '/'))
            .Where(f => !f.StartsWith("_magick/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];

        public void Dispose()
        {
            try
            {
                Directory.Delete(Folder, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    public static byte[] Heic() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Photos", "Fixtures", "sample.heic"));

    public static byte[] Solid(MagickFormat format, uint width, uint height, MagickColor color = null)
    {
        using MagickImage image = new(color ?? MagickColors.SteelBlue, width, height);
        image.Format = format;
        return image.ToByteArray();
    }

    /// <summary>JPEG com GPS, descrição e fabricante no EXIF e a orientação pedida (1 = normal, 6 = girada 90°).</summary>
    public static byte[] JpegWithGps(uint width, uint height, ushort orientation = 1)
    {
        using MagickImage image = new(MagickColors.Red, width, height);
        ExifProfile exif = new();
        exif.SetValue(ExifTag.GPSLatitudeRef, "S");
        exif.SetValue(ExifTag.GPSLatitude, [new Rational(23, 1), new Rational(32, 1), new Rational(10, 1)]);
        exif.SetValue(ExifTag.GPSLongitudeRef, "W");
        exif.SetValue(ExifTag.GPSLongitude, [new Rational(46, 1), new Rational(38, 1), new Rational(5, 1)]);
        exif.SetValue(ExifTag.ImageDescription, Secret);
        exif.SetValue(ExifTag.Make, "CameraSecreta");
        exif.SetValue(ExifTag.Orientation, orientation);
        image.SetProfile(exif);
        image.Orientation = orientation == 6 ? OrientationType.RightTop : OrientationType.TopLeft;
        image.Format = MagickFormat.Jpeg;
        return image.ToByteArray();
    }

    /// <summary>Imagem de ruído (difícil de comprimir), para comparar o tamanho em qualidades diferentes.</summary>
    public static byte[] NoisyJpeg(uint width, uint height)
    {
        using MagickImage image = new(MagickColors.Gray, width, height);
        image.AddNoise(NoiseType.Uniform);
        image.Format = MagickFormat.Jpeg;
        image.Quality = 95;
        return image.ToByteArray();
    }

    /// <summary>Imagem de ruído no formato pedido (não comprime à toa, então cortá-la no meio perde dados de verdade).</summary>
    public static byte[] Noisy(MagickFormat format, uint width = 300, uint height = 200)
    {
        using MagickImage image = new(MagickColors.Gray, width, height);
        image.AddNoise(NoiseType.Uniform);
        image.Format = format;
        image.Quality = 95;
        return image.ToByteArray();
    }

    public static byte[] AnimatedGif()
    {
        using MagickImageCollection frames = new();
        foreach (MagickColor color in new[] { MagickColors.Red, MagickColors.Green, MagickColors.Blue })
        {
            MagickImage frame = new(color, 60, 40) { AnimationDelay = 10 };
            frames.Add(frame);
        }

        return frames.ToByteArray(MagickFormat.Gif);
    }

    /// <summary>PNG cinza válido e quase vazio de bytes (a imagem é só zeros comprimidos): serve para provar o limite de pixels sem gastar memória no teste.</summary>
    public static byte[] ZeroPng(int width, int height)
    {
        using MemoryStream raw = new();
        using (ZLibStream zlib = new(raw, CompressionLevel.Fastest, leaveOpen: true))
        {
            byte[] row = new byte[width + 1];
            for (int y = 0; y < height; y++)
            {
                zlib.Write(row, 0, row.Length);
            }
        }

        using MemoryStream png = new();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        byte[] header = new byte[13];
        WriteInt(header, 0, width);
        WriteInt(header, 4, height);
        header[8] = 8; // 8 bits
        header[9] = 0; // cinza
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", raw.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    /// <summary>Cabeçalho de JPEG seguido de padding, até o tamanho pedido (não é uma imagem; serve com um processador falso).</summary>
    public static byte[] JpegShapedBytes(int length)
    {
        byte[] bytes = new byte[length];
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        return bytes;
    }

    private static void WriteInt(byte[] target, int offset, int value)
    {
        target[offset] = (byte)(value >> 24);
        target[offset + 1] = (byte)(value >> 16);
        target[offset + 2] = (byte)(value >> 8);
        target[offset + 3] = (byte)value;
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        byte[] length = new byte[4];
        WriteInt(length, 0, data.Length);
        stream.Write(length);
        byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);
        byte[] crc = new byte[4];
        WriteInt(crc, 0, unchecked((int)Crc32(typeBytes, data)));
        stream.Write(crc);
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in type.Concat(data))
        {
            crc ^= b;
            for (int k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
        }

        return ~crc;
    }
}
