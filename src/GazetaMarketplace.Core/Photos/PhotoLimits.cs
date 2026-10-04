namespace GazetaMarketplace.Core.Photos;

/// <summary>Os números das fotos num lugar só (NFR-12, NFR-05, ADR-005).</summary>
public static class PhotoLimits
{
    /// <summary>10 MB por foto; um byte a mais já é recusado.</summary>
    public const int MaxBytes = 10 * 1024 * 1024;

    /// <summary>Largura máxima da versão grande (galeria e ampliação).</summary>
    public const int LargeWidth = 1600;

    /// <summary>Largura máxima da miniatura (cards e lista).</summary>
    public const int ThumbWidth = 480;

    public const int WebPQuality = 80;

    /// <summary>RC-2: acima disso a imagem é recusada antes de decodificar (uma bomba de descompressão cabe em poucos KB de arquivo).</summary>
    public const long MaxPixels = 50_000_000;

    /// <summary>Lado máximo, para recusar uma faixa de 1 × 50 milhões de pixels.</summary>
    public const int MaxSide = 20_000;
}

/// <summary>Qual das duas versões WebP.</summary>
public enum PhotoSize
{
    Large = 1600,
    Thumb = 480
}

public static class PhotoSizes
{
    /// <summary>Lê o tamanho da rota (<c>1600</c> ou <c>480</c>); qualquer outro texto devolve <c>null</c>.</summary>
    public static PhotoSize? Parse(string text) => text switch
    {
        "1600" => PhotoSize.Large,
        "480" => PhotoSize.Thumb,
        _ => null
    };

    public static string Suffix(PhotoSize size) => ((int)size).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
