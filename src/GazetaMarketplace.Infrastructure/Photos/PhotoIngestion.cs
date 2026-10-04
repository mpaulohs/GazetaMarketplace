using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Photos;

namespace GazetaMarketplace.Infrastructure.Photos;

/// <inheritdoc cref="IPhotoIngestion"/>
/// <remarks>
/// A ordem protege o disco: tamanho, depois formato pela assinatura, depois o processamento (que nada grava), e só então os arquivos. Se gravar o original ou as
/// versões falhar, o que já foi escrito é apagado. Um processo que morra entre gravar e registrar em <c>AdPhotos</c> deixa arquivos órfãos; a limpeza da 3.6 os varre.
/// </remarks>
public sealed class PhotoIngestion(IImageProcessor processor, IPhotoStorage storage, TimeProvider time) : IPhotoIngestion
{
    /// <summary>Quantas fotos são decodificadas ao mesmo tempo no processo. Uma foto de 50 milhões de pixels usa uns 200 MB e o ImageMagick tem 512 MB no total (RC-2).</summary>
    public const int MaxConcurrentProcessing = 2;

    private static readonly SemaphoreSlim Slots = new(MaxConcurrentProcessing, MaxConcurrentProcessing);

    public async Task<StoredPhoto> IngestAsync(int adId, Stream content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(adId);

        byte[] data = await ReadLimitedAsync(content, cancellationToken);
        PhotoFormat format = PhotoSignature.Detect(data.AsSpan(0, Math.Min(data.Length, PhotoSignature.HeaderLength)))
            ?? throw Invalid(PhotoMessages.UnsupportedFormat);

        ProcessedImage processed = await ProcessAsync(processor, data, format, cancellationToken);

        string storageKey = $"{adId}/{Guid.NewGuid():N}";
        string originalKey = null;
        try
        {
            originalKey = await storage.SaveOriginalAsync(data, format, time.GetUtcNow().UtcDateTime, cancellationToken);
            await storage.SaveVersionsAsync(storageKey, processed.Large, processed.Thumb, cancellationToken);
        }
        catch
        {
            // Falha no meio: nada fica para trás (a limpeza em si não pode esconder o erro original)
            try
            {
                await storage.DeleteAsync(storageKey, originalKey, CancellationToken.None);
            }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
            {
            }

            throw;
        }

        return new StoredPhoto(storageKey, originalKey, processed.Width, processed.Height, processed.Large.Length);
    }

    /// <summary>Processa dentro do limite de decodificações ao mesmo tempo; o reprocessamento usa o mesmo limite.</summary>
    internal static async Task<ProcessedImage> ProcessAsync(IImageProcessor processor, byte[] data, PhotoFormat format, CancellationToken cancellationToken)
    {
        await Slots.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() => processor.Process(data, format), cancellationToken);
        }
        finally
        {
            Slots.Release();
        }
    }

    // Lê no máximo o limite + 1 byte: um arquivo gigante nunca é carregado inteiro na memória
    private static async Task<byte[]> ReadLimitedAsync(Stream content, CancellationToken cancellationToken)
    {
        if (content.CanSeek && content.Length > PhotoLimits.MaxBytes)
        {
            throw Invalid(PhotoMessages.TooLarge);
        }

        using MemoryStream buffer = new();
        byte[] chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > PhotoLimits.MaxBytes)
            {
                throw Invalid(PhotoMessages.TooLarge);
            }

            buffer.Write(chunk, 0, read);
        }

        if (buffer.Length == 0)
        {
            throw Invalid(PhotoMessages.Empty);
        }

        return buffer.ToArray();
    }

    private static ValidationException Invalid(string message) =>
        new(new Dictionary<string, string[]> { [PhotoMessages.Field] = [message] });
}
