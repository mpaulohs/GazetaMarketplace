using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Photos;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Infrastructure.Photos;

/// <inheritdoc cref="IPhotoStorage"/>
/// <remarks>
/// Pasta persistente fora da raiz do site (<c>PhotoStorage:BasePath</c>). Todo caminho nasce de uma chave com formato fixo (ids numéricos e GUIDs gerados pelo
/// site) e o caminho final é conferido: precisa ficar dentro da pasta base (RC-4). Cada arquivo é gravado num temporário e renomeado, então uma foto pela
/// metade nunca aparece.
/// </remarks>
public sealed partial class FileSystemPhotoStorage(IOptions<PhotoStorageOptions> options) : IPhotoStorage
{
    public const string OriginalsFolder = "_originals";

    [GeneratedRegex(@"^[1-9][0-9]{0,9}/[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex StorageKeyShape();

    [GeneratedRegex(@"^_originals/[0-9]{4}-(0[1-9]|1[0-2])/[0-9a-f]{32}\.(jpg|png|gif|webp|heic)$", RegexOptions.CultureInvariant)]
    private static partial Regex OriginalKeyShape();

    public async Task SaveVersionsAsync(string storageKey, byte[] large, byte[] thumb, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(large);
        ArgumentNullException.ThrowIfNull(thumb);
        string largePath = VersionPath(storageKey, PhotoSize.Large);
        string thumbPath = VersionPath(storageKey, PhotoSize.Thumb);
        Directory.CreateDirectory(Path.GetDirectoryName(largePath)!);

        try
        {
            await WriteAtomicallyAsync(largePath, large, cancellationToken);
            await WriteAtomicallyAsync(thumbPath, thumb, cancellationToken);
        }
        catch
        {
            // Ou as duas versões, ou nenhuma; a limpeza não pode esconder o erro que a causou
            TryDelete(largePath);
            TryDelete(thumbPath);
            throw;
        }
    }

    public async Task<string> SaveOriginalAsync(byte[] content, PhotoFormat format, DateTime utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        string key = $"{OriginalsFolder}/{utcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture)}/{Guid.NewGuid():N}.{PhotoFormats.Extension(format)}";
        string path = OriginalPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            await WriteAtomicallyAsync(path, content, cancellationToken);
        }
        catch
        {
            TryDelete(path);
            throw;
        }

        return key;
    }

    public Task<Stream> OpenAsync(string storageKey, PhotoSize size, CancellationToken cancellationToken)
    {
        string path = VersionPath(storageKey, size);
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream>(null);
        }

        try
        {
            return Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan));
        }
        catch (FileNotFoundException)
        {
            return Task.FromResult<Stream>(null);
        }
        catch (DirectoryNotFoundException)
        {
            return Task.FromResult<Stream>(null);
        }
    }

    public Task DeleteAsync(string storageKey, string originalKey, CancellationToken cancellationToken)
    {
        if (storageKey is not null)
        {
            DeleteIfExists(VersionPath(storageKey, PhotoSize.Large));
            DeleteIfExists(VersionPath(storageKey, PhotoSize.Thumb));
        }

        if (originalKey is not null)
        {
            DeleteIfExists(OriginalPath(originalKey));
        }

        return Task.CompletedTask;
    }

    private string VersionPath(string storageKey, PhotoSize size)
    {
        if (storageKey is null || !StorageKeyShape().IsMatch(storageKey))
        {
            throw new ArgumentException("Chave de foto inválida.", nameof(storageKey));
        }

        return Confine($"{storageKey}_{PhotoSizes.Suffix(size)}.webp");
    }

    private string OriginalPath(string originalKey)
    {
        if (originalKey is null || !OriginalKeyShape().IsMatch(originalKey))
        {
            throw new ArgumentException("Chave de original inválida.", nameof(originalKey));
        }

        return Confine(originalKey);
    }

    // A pasta base é conferida a cada uso (e não na partida) porque em desenvolvimento ela pode nem estar configurada
    private string Confine(string relative)
    {
        string baseFolder = options.Value.BasePath;
        if (string.IsNullOrWhiteSpace(baseFolder))
        {
            throw new InvalidOperationException("PhotoStorage:BasePath não está configurado.");
        }

        string root = Path.GetFullPath(baseFolder);
        string full = Path.GetFullPath(Path.Combine(root, relative));
        if (!full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("O caminho da foto sai da pasta base.", nameof(relative));
        }

        return full;
    }

    private static async Task WriteAtomicallyAsync(string path, byte[] content, CancellationToken cancellationToken)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temp, content, cancellationToken);
            File.Move(temp, path, overwrite: false);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (DirectoryNotFoundException)
        {
            // já não existe
        }
    }
}
