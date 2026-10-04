using System.Globalization;

namespace GazetaMarketplace.Core.Photos;

/// <summary>O endereço público de cada versão de uma foto (a rota de <c>PhotosController</c>); o único lugar que monta esse texto.</summary>
public static class PhotoUrls
{
    public static string For(int adId, int photoId, PhotoSize size) =>
        string.Create(CultureInfo.InvariantCulture, $"/fotos/{adId}/{photoId}-{PhotoSizes.Suffix(size)}.webp");
}
