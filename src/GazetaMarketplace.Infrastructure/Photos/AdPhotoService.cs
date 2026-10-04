using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Infrastructure.Photos;

/// <inheritdoc cref="IAdPhotoService"/>
/// <remarks>
/// <para>
/// <b>Ordem do envio:</b> o acesso e o limite são conferidos antes de processar (uma recusa não gasta CPU) e conferidos de novo dentro da transação, que é quando
/// vale. O processamento fica <i>fora</i> da transação: ele dura segundos e segurar o bloqueio do anúncio nesse tempo faria os envios do mesmo anúncio esperarem
/// uns pelos outros.
/// </para>
/// <para>
/// <b>Concorrência:</b> a transação começa travando a linha do anúncio (<c>UPDLOCK</c>). Quem chega depois espera o commit de quem veio antes, então
/// a contagem e a próxima posição que ele lê já incluem a foto do outro. Só o SQL Server tem a dica; o SQLite dos testes unitários grava uma transação por vez.
/// Se a gravação falhar depois de os arquivos existirem, eles são apagados; se o processo morrer entre uma coisa e outra, sobram arquivos órfãos que a limpeza da 3.6 varre.
/// </para>
/// </remarks>
public sealed class AdPhotoService(
    AppDbContext context,
    IAdService ads,
    ICategoryTree categories,
    IPhotoIngestion ingestion,
    IPhotoStorage storage,
    ILogger<AdPhotoService> logger) : IAdPhotoService
{
    public async Task<IReadOnlyList<AdPhotoItem>> ListAsync(int adId, CancellationToken cancellationToken)
    {
        List<AdPhoto> photos = await context.AdPhotos.AsNoTracking()
            .Where(p => p.AdId == adId)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);
        return [.. photos.Select(ToItem)];
    }

    public async Task<AdPhotoItem> AddAsync(int adId, Stream content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        Ad ad = await ads.GetForEditAsync(adId, cancellationToken);
        int max = await MaxPhotosAsync(ad, cancellationToken);
        EnsureRoom(max, await context.AdPhotos.CountAsync(p => p.AdId == adId, cancellationToken));

        StoredPhoto stored = await ingestion.IngestAsync(adId, content, cancellationToken);
        try
        {
            AdPhoto photo = await InsertAsync(adId, stored, max, cancellationToken);
            return ToItem(photo);
        }
        catch
        {
            // O registro não foi gravado: os arquivos não podem ficar. A limpeza não esconde o erro de origem
            await TryDeleteFilesAsync(stored.StorageKey, stored.OriginalKey);
            throw;
        }
    }

    public async Task SetCoverAsync(int adId, int photoId, CancellationToken cancellationToken)
    {
        await ads.GetForEditAsync(adId, cancellationToken);
        await InTransactionAsync(adId, async () =>
        {
            List<AdPhoto> photos = await LoadAsync(adId, cancellationToken);
            AdPhoto cover = photos.SingleOrDefault(p => p.Id == photoId) ?? throw new NotFoundException(PhotoMessages.NotFound);
            if (photos[0] == cover)
            {
                return;
            }

            photos.Remove(cover);
            photos.Insert(0, cover);
            Renumber(photos);
            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task DeleteAsync(int adId, int photoId, CancellationToken cancellationToken)
    {
        await ads.GetForEditAsync(adId, cancellationToken);
        AdPhoto removed = null;
        await InTransactionAsync(adId, async () =>
        {
            List<AdPhoto> photos = await LoadAsync(adId, cancellationToken);
            removed = photos.SingleOrDefault(p => p.Id == photoId) ?? throw new NotFoundException(PhotoMessages.NotFound);
            photos.Remove(removed);
            context.AdPhotos.Remove(removed);
            Renumber(photos);
            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        // Depois do commit: o registro já não existe, então a URL deixa de funcionar. O original fica (originalKey nulo) até a limpeza de 30 dias
        await TryDeleteFilesAsync(removed.StorageKey, originalKey: null);
    }

    private async Task<AdPhoto> InsertAsync(int adId, StoredPhoto stored, int max, CancellationToken cancellationToken)
    {
        AdPhoto photo = null;
        await InTransactionAsync(adId, async () =>
        {
            // Já com a linha do anúncio travada: este é o número que vale
            int count = await context.AdPhotos.CountAsync(p => p.AdId == adId, cancellationToken);
            EnsureRoom(max, count);

            int next = count == 0 ? 0 : await context.AdPhotos.Where(p => p.AdId == adId).MaxAsync(p => p.SortOrder, cancellationToken) + 1;
            photo = new AdPhoto
            {
                AdId = adId,
                SortOrder = next,
                StorageKey = stored.StorageKey,
                OriginalKey = stored.OriginalKey,
                Width = stored.Width,
                Height = stored.Height,
                SizeBytes = stored.SizeBytes
            };
            context.AdPhotos.Add(photo);
            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
        return photo;
    }

    // Uma transação que começa travando o anúncio; repetida inteira se o SQL Server pedir (falha transitória)
    private async Task InTransactionAsync(int adId, Func<Task> work, CancellationToken cancellationToken)
    {
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            if (context.Database.IsSqlServer())
            {
                // Lê o id do anúncio só para travar a linha até o commit. SqlQuery exige a coluna "Value"
                List<int> locked = await context.Database
                    .SqlQuery<int>($"SELECT Id AS Value FROM Ads WITH (UPDLOCK, ROWLOCK) WHERE Id = {adId}")
                    .ToListAsync(cancellationToken);
                if (locked.Count == 0)
                {
                    throw new NotFoundException("Anúncio", adId);
                }
            }

            await work();
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private Task<List<AdPhoto>> LoadAsync(int adId, CancellationToken cancellationToken) =>
        context.AdPhotos.Where(p => p.AdId == adId).OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync(cancellationToken);

    // Posições 0, 1, 2… sem buraco, na ordem da lista
    private static void Renumber(List<AdPhoto> photos)
    {
        for (int i = 0; i < photos.Count; i++)
        {
            photos[i].SortOrder = i;
        }
    }

    private async Task<int> MaxPhotosAsync(Ad ad, CancellationToken cancellationToken)
    {
        if (ad.CategoryId is not { } categoryId)
        {
            return FieldGroupRegistry.Default.MaxPhotos;
        }

        CategoryTreeSnapshot snapshot = await categories.GetAsync(cancellationToken);
        return (FieldGroupRegistry.Resolve(snapshot, categoryId) ?? FieldGroupRegistry.Default).MaxPhotos;
    }

    private static void EnsureRoom(int max, int current)
    {
        if (current >= max)
        {
            throw new ConflictException(max == 0 ? PhotoMessages.CategoryHasNoPhotos : PhotoMessages.TooManyPhotos(max));
        }
    }

    private async Task TryDeleteFilesAsync(string storageKey, string originalKey)
    {
        try
        {
            await storage.DeleteAsync(storageKey, originalKey, CancellationToken.None);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // O registro já está certo; o arquivo que sobrar é varrido pela limpeza
            logger.LogWarning(ex, "Não foi possível apagar os arquivos da foto {StorageKey}", storageKey);
        }
    }

    private static AdPhotoItem ToItem(AdPhoto photo) => new(photo.Id, photo.AdId, photo.SortOrder, photo.Width, photo.Height);
}
