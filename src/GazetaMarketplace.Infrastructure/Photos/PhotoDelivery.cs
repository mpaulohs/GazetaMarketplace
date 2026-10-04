using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.Infrastructure.Photos;

/// <inheritdoc cref="IPhotoDelivery"/>
/// <remarks>
/// Anúncio publicado: qualquer visitante. Qualquer outra situação: o Administrador e o autor (a mesma regra de leitura de <see cref="AdAccess"/>). A foto precisa
/// pertencer ao anúncio da rota. O resultado é <c>null</c> em todos os casos de recusa, para o site não revelar se a foto existe.
/// </remarks>
public sealed class PhotoDelivery(AppDbContext context, ICurrentUser currentUser, IPhotoStorage storage) : IPhotoDelivery
{
    public async Task<PhotoFile> OpenAsync(int adId, int photoId, PhotoSize size, CancellationToken cancellationToken)
    {
        var row = await context.AdPhotos.AsNoTracking()
            .Where(p => p.Id == photoId && p.AdId == adId)
            .Join(context.Ads, p => p.AdId, a => a.Id, (p, a) => new { p.StorageKey, a.Status, a.AuthorId })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        bool isPublic = row.Status == AdStatus.Published;
        if (!isPublic && !AdAccess.CanView(new AdActor(currentUser.UserId, currentUser.IsAdministrator), row.AuthorId))
        {
            return null;
        }

        Stream stream = await storage.OpenAsync(row.StorageKey, size, cancellationToken);
        return stream is null ? null : new PhotoFile(stream, isPublic);
    }
}
