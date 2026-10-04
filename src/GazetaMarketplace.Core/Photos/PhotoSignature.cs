using System;

namespace GazetaMarketplace.Core.Photos;

/// <summary>
/// Reconhece o formato pelos primeiros bytes do arquivo (ADR-005): JPEG <c>FF D8 FF</c>, PNG <c>89 50 4E 47 0D 0A 1A 0A</c>, GIF <c>GIF87a/GIF89a</c>,
/// WebP <c>RIFF…WEBP</c> e HEIC/HEIF pela caixa <c>ftyp</c>. AVIF também usa <c>ftyp</c> com a marca genérica <c>mif1</c>, então a marca genérica só vale
/// se uma marca de HEIC aparecer entre as compatíveis; um AVIF puro é recusado.
/// </summary>
public static class PhotoSignature
{
    /// <summary>Quantos bytes do começo do arquivo bastam para decidir.</summary>
    public const int HeaderLength = 64;

    private static readonly string[] HeicBrands = ["heic", "heix", "hevc", "hevx", "heim", "heis", "hevm", "hevs"];
    private static readonly string[] GenericBrands = ["mif1", "msf1"];

    /// <summary>O formato, ou <c>null</c> se o conteúdo não é um dos cinco.</summary>
    public static PhotoFormat? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return PhotoFormat.Jpeg;
        }

        if (header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return PhotoFormat.Png;
        }

        if (header.Length >= 6 && (header[..6].SequenceEqual("GIF87a"u8) || header[..6].SequenceEqual("GIF89a"u8)))
        {
            return PhotoFormat.Gif;
        }

        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return PhotoFormat.WebP;
        }

        return IsHeic(header) ? PhotoFormat.Heic : null;
    }

    private static bool IsHeic(ReadOnlySpan<byte> header)
    {
        if (header.Length < 16 || !header.Slice(4, 4).SequenceEqual("ftyp"u8))
        {
            return false;
        }

        string major = Brand(header, 8);
        if (Array.IndexOf(HeicBrands, major) >= 0)
        {
            return true;
        }

        if (Array.IndexOf(GenericBrands, major) < 0)
        {
            return false;
        }

        // Marca genérica: precisa listar uma marca de HEIC entre as compatíveis (a partir do byte 16, de 4 em 4, até o fim da caixa)
        long boxSize = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
        int end = (int)Math.Min(Math.Max(boxSize, 16), header.Length);
        for (int offset = 16; offset + 4 <= end; offset += 4)
        {
            if (Array.IndexOf(HeicBrands, Brand(header, offset)) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string Brand(ReadOnlySpan<byte> header, int offset) =>
        System.Text.Encoding.ASCII.GetString(header.Slice(offset, 4));
}
