namespace GazetaMarketplace.Core.Photos;

/// <summary>Os cinco formatos que o site aceita (NFR-12). Quem decide é o conteúdo do arquivo (<see cref="PhotoSignature"/>), nunca a extensão.</summary>
public enum PhotoFormat
{
    Jpeg = 1,
    Png = 2,
    Gif = 3,
    WebP = 4,
    Heic = 5
}

public static class PhotoFormats
{
    /// <summary>A extensão do original guardado em <c>_originals/</c>; vem do formato detectado, nunca do nome enviado.</summary>
    public static string Extension(PhotoFormat format) => format switch
    {
        PhotoFormat.Jpeg => "jpg",
        PhotoFormat.Png => "png",
        PhotoFormat.Gif => "gif",
        PhotoFormat.WebP => "webp",
        PhotoFormat.Heic => "heic",
        _ => throw new System.ArgumentOutOfRangeException(nameof(format), format, null)
    };
}
