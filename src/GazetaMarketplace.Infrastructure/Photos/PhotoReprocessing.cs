using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.Infrastructure.Photos;

/// <inheritdoc cref="IPhotoReprocessing"/>
public sealed class PhotoReprocessing(AppDbContext context, IPhotoStorage storage, IImageProcessor processor) : IPhotoReprocessing
{
    public async Task ReprocessAsync(int photoId, CancellationToken cancellationToken)
    {
        AdPhoto photo = await context.AdPhotos.SingleOrDefaultAsync(p => p.Id == photoId, cancellationToken)
            ?? throw new NotFoundException("Foto", photoId);

        // Sem chave (a limpeza de 30 dias a anulou) ou com o arquivo ausente: não há de onde regerar
        byte[] original = photo.OriginalKey is null ? null : await storage.ReadOriginalAsync(photo.OriginalKey, cancellationToken);
        if (original is null)
        {
            throw new ConflictException(PhotoMessages.OriginalUnavailable);
        }

        PhotoFormat format = PhotoSignature.Detect(original.AsSpan(0, Math.Min(original.Length, PhotoSignature.HeaderLength)))
            ?? throw new ValidationException(new System.Collections.Generic.Dictionary<string, string[]> { [PhotoMessages.Field] = [PhotoMessages.UnsupportedFormat] });

        ProcessedImage processed = await PhotoIngestion.ProcessAsync(processor, original, format, cancellationToken);
        await storage.ReplaceVersionsAsync(photo.StorageKey, processed.Large, processed.Thumb, cancellationToken);

        photo.Width = processed.Width;
        photo.Height = processed.Height;
        photo.SizeBytes = processed.Large.Length;
        await context.SaveChangesAsync(cancellationToken);
    }
}
