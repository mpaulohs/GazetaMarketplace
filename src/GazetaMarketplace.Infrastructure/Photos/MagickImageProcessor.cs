using System;
using System.Collections.Generic;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Photos;
using ImageMagick;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Infrastructure.Photos;

/// <inheritdoc cref="IImageProcessor"/>
/// <remarks>
/// O formato vem da assinatura do arquivo e é imposto na leitura: um SVG com extensão <c>.jpg</c> nunca chega a um decodificador de SVG (a política também
/// os desliga). As dimensões são lidas antes de decodificar (RC-2). GIF e WebP animados viram o primeiro quadro.
/// </remarks>
public sealed class MagickImageProcessor(IOptions<PhotoStorageOptions> options) : IImageProcessor
{
    public ProcessedImage Process(byte[] content, PhotoFormat format)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Na primeira foto, não ao montar o objeto: toda página do anúncio cria o serviço de fotos e não deve ligar a biblioteca nativa à toa
        MagickRuntime.EnsureInitialized(options.Value.BasePath);
        MagickReadSettings settings = new() { Format = ToMagick(format), FrameIndex = 0, FrameCount = 1 };
        string unreadable = format == PhotoFormat.Heic ? PhotoMessages.HeicUnreadable : PhotoMessages.Unreadable;

        try
        {
            MagickImageInfo info = new(content, settings);
            if (info.Width == 0 || info.Height == 0 || info.Width > PhotoLimits.MaxSide || info.Height > PhotoLimits.MaxSide
                || (long)info.Width * info.Height > PhotoLimits.MaxPixels)
            {
                throw Invalid(PhotoMessages.TooManyPixels);
            }

            using MagickImage image = new();
            List<string> warnings = [];
            image.Warning += (_, e) => warnings.Add(e.Message);
            image.Read(content, settings);

            // Arquivo cortado no meio (conexão que caiu): o ImageMagick devolve a parte lida e só avisa. Uma foto pela metade não vai ao ar
            if (warnings.Exists(IsTruncation))
            {
                throw Invalid(unreadable);
            }

            image.AutoOrient();
            ToSrgb(image);
            image.Strip(); // nenhum metadado sobrevive: GPS, câmera, data, miniatura embutida

            image.Format = MagickFormat.WebP;
            image.Quality = PhotoLimits.WebPQuality;

            Shrink(image, PhotoLimits.LargeWidth);
            int width = (int)image.Width;
            int height = (int)image.Height;
            byte[] large = image.ToByteArray();

            Shrink(image, PhotoLimits.ThumbWidth);
            byte[] thumb = image.ToByteArray();
            return new ProcessedImage(large, thumb, width, height);
        }
        catch (MagickResourceLimitErrorException)
        {
            throw Invalid(PhotoMessages.TooManyPixels);
        }
        catch (MagickException)
        {
            throw Invalid(unreadable);
        }
    }

    private static bool IsTruncation(string warning) =>
        warning.Contains("premature end", StringComparison.OrdinalIgnoreCase)
        || warning.Contains("unexpected end", StringComparison.OrdinalIgnoreCase)
        || warning.Contains("insufficient image data", StringComparison.OrdinalIgnoreCase)
        || warning.Contains("end of file", StringComparison.OrdinalIgnoreCase)
        || warning.Contains("truncated", StringComparison.OrdinalIgnoreCase);

    // Só diminui, mantendo a proporção: a largura cabe em maxWidth e o lado maior (a altura de uma foto em retrato) cabe em MaxLongSide.
    // Foto menor que os dois limites mantém o tamanho
    private static void Shrink(MagickImage image, int maxWidth)
    {
        double scale = Math.Min(1d, Math.Min((double)maxWidth / image.Width, (double)PhotoLimits.MaxLongSide / Math.Max(image.Width, image.Height)));
        if (scale >= 1d)
        {
            return;
        }

        uint width = (uint)Math.Max(1, (long)Math.Round(image.Width * scale));
        uint height = (uint)Math.Max(1, (long)Math.Round(image.Height * scale));
        image.Resize(width, height);
    }

    // Com perfil de cor embutido (ou CMYK) a imagem é convertida para sRGB antes de o perfil ser descartado; senão as cores mudariam
    private static void ToSrgb(MagickImage image)
    {
        IColorProfile profile = image.GetColorProfile();
        if (image.ColorSpace == ColorSpace.CMYK)
        {
            image.TransformColorSpace(profile ?? ColorProfiles.USWebCoatedSWOP, ColorProfiles.SRGB);
        }
        else if (profile is not null && !string.Equals(profile.Description, ColorProfiles.SRGB.Description, StringComparison.Ordinal))
        {
            image.TransformColorSpace(profile, ColorProfiles.SRGB);
        }
    }

    private static MagickFormat ToMagick(PhotoFormat format) => format switch
    {
        PhotoFormat.Jpeg => MagickFormat.Jpeg,
        PhotoFormat.Png => MagickFormat.Png,
        PhotoFormat.Gif => MagickFormat.Gif,
        PhotoFormat.WebP => MagickFormat.WebP,
        PhotoFormat.Heic => MagickFormat.Heic,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    private static ValidationException Invalid(string message) =>
        new(new Dictionary<string, string[]> { [PhotoMessages.Field] = [message] });
}
