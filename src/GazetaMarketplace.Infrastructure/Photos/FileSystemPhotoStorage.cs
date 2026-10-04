using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
public sealed partial class FileSystemPhotoStorage(IOptions<PhotoStorageOptions> options) : IPhotoStorage, IPhotoStorageMaintenance
{
    public const string OriginalsFolder = "_originals";

    [GeneratedRegex(@"^[1-9][0-9]{0,9}/[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex StorageKeyShape();

    [GeneratedRegex(@"^_originals/[0-9]{4}-(0[1-9]|1[0-2])/[0-9a-f]{32}\.(jpg|png|gif|webp|heic)$", RegexOptions.CultureInvariant)]
    private static partial Regex OriginalKeyShape();

    // Formatos de nome dos arquivos temporários (o nome final + "." + GUID + ".tmp") que a limpeza reconhece
    [GeneratedRegex(@"^[1-9][0-9]{0,9}/[0-9a-f]{32}_(1600|480)\.webp\.[0-9a-f]{32}\.tmp$", RegexOptions.CultureInvariant)]
    private static partial Regex VersionTempShape();

    [GeneratedRegex(@"^_originals/[0-9]{4}-(0[1-9]|1[0-2])/[0-9a-f]{32}\.(jpg|png|gif|webp|heic)\.[0-9a-f]{32}\.tmp$", RegexOptions.CultureInvariant)]
    private static partial Regex OriginalTempShape();

    [GeneratedRegex(@"^[1-9][0-9]{0,9}$", RegexOptions.CultureInvariant)]
    private static partial Regex AdFolderShape();

    [GeneratedRegex(@"^[0-9]{4}-(0[1-9]|1[0-2])$", RegexOptions.CultureInvariant)]
    private static partial Regex MonthFolderShape();

    [GeneratedRegex(@"^[1-9][0-9]{0,9}/[0-9a-f]{32}_(1600|480)\.webp$", RegexOptions.CultureInvariant)]
    private static partial Regex VersionFileShape();

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

    public async Task ReplaceVersionsAsync(string storageKey, byte[] large, byte[] thumb, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(large);
        ArgumentNullException.ThrowIfNull(thumb);
        string largePath = VersionPath(storageKey, PhotoSize.Large);
        string thumbPath = VersionPath(storageKey, PhotoSize.Thumb);
        Directory.CreateDirectory(Path.GetDirectoryName(largePath)!);

        // Cada arquivo troca de uma vez (grava ao lado e renomeia por cima): nunca existe uma versão pela metade
        await WriteAtomicallyAsync(largePath, large, cancellationToken, overwrite: true);
        await WriteAtomicallyAsync(thumbPath, thumb, cancellationToken, overwrite: true);
    }

    public async Task<byte[]> ReadOriginalAsync(string originalKey, CancellationToken cancellationToken)
    {
        string path = OriginalPath(originalKey);
        try
        {
            return await File.ReadAllBytesAsync(path, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
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

    public IEnumerable<StoredFile> List()
    {
        string root = Root();
        if (!Directory.Exists(root))
        {
            yield break;
        }

        // Os originais: _originals/<yyyy-MM>/<arquivo>
        string originals = Path.Combine(root, OriginalsFolder);
        foreach (string month in SafeDirectories(originals))
        {
            string monthName = Path.GetFileName(month);
            if (!MonthFolderShape().IsMatch(monthName))
            {
                continue;
            }

            foreach (StoredFile file in ClassifyFiles(month, $"{OriginalsFolder}/{monthName}"))
            {
                yield return file;
            }
        }

        // As versões: <adId>/<arquivo>. A pasta de configuração do ImageMagick (_magick) e qualquer pasta fora do formato nunca entram
        foreach (string adFolder in SafeDirectories(root))
        {
            string adName = Path.GetFileName(adFolder);
            if (!AdFolderShape().IsMatch(adName))
            {
                continue;
            }

            foreach (StoredFile file in ClassifyFiles(adFolder, adName))
            {
                yield return file;
            }
        }
    }

    public void Delete(string path)
    {
        if (Classify(path, DateTime.MinValue) is null)
        {
            throw new ArgumentException("Caminho fora do formato gerado pelo site.", nameof(path));
        }

        DeleteIfExists(Confine(path));
    }

    public int RemoveEmptyFolders(DateTime olderThanUtc)
    {
        string root = Root();
        int removed = 0;
        string originals = Path.Combine(root, OriginalsFolder);
        foreach (string month in SafeDirectories(originals).Where(d => MonthFolderShape().IsMatch(Path.GetFileName(d))))
        {
            removed += TryRemoveEmpty(month, olderThanUtc);
        }

        foreach (string adFolder in SafeDirectories(root).Where(d => AdFolderShape().IsMatch(Path.GetFileName(d))))
        {
            removed += TryRemoveEmpty(adFolder, olderThanUtc);
        }

        return removed;
    }

    private string Root()
    {
        string baseFolder = options.Value.BasePath;
        if (string.IsNullOrWhiteSpace(baseFolder))
        {
            throw new InvalidOperationException("PhotoStorage:BasePath não está configurado.");
        }

        return Path.GetFullPath(baseFolder);
    }

    private static IEnumerable<string> SafeDirectories(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        try
        {
            // Um atalho (link simbólico) para outra pasta nunca é seguido
            return Directory.EnumerateDirectories(folder).Where(d => !File.GetAttributes(d).HasFlag(FileAttributes.ReparsePoint)).ToList();
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
    }

    private static IEnumerable<StoredFile> ClassifyFiles(string folder, string relativeFolder)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(folder).ToList();
        }
        catch (DirectoryNotFoundException)
        {
            yield break;
        }

        foreach (string file in files)
        {
            FileInfo info = new(file);
            if (!info.Exists || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                continue;
            }

            if (Classify($"{relativeFolder}/{info.Name}", info.LastWriteTimeUtc) is { } stored)
            {
                yield return stored;
            }
        }
    }

    // Só o que tem exatamente o nome que o site gera
    private static StoredFile Classify(string relativePath, DateTime lastWriteUtc)
    {
        if (relativePath is null)
        {
            return null;
        }

        if (OriginalKeyShape().IsMatch(relativePath))
        {
            return new StoredFile(relativePath, StoredFileKind.Original, null, lastWriteUtc);
        }

        if (VersionFileShape().IsMatch(relativePath))
        {
            return new StoredFile(relativePath, StoredFileKind.Version, relativePath[..relativePath.LastIndexOf('_')], lastWriteUtc);
        }

        if (VersionTempShape().IsMatch(relativePath) || OriginalTempShape().IsMatch(relativePath))
        {
            return new StoredFile(relativePath, StoredFileKind.Temporary, null, lastWriteUtc);
        }

        return null;
    }

    private static int TryRemoveEmpty(string folder, DateTime olderThanUtc)
    {
        try
        {
            if (Directory.GetLastWriteTimeUtc(folder) < olderThanUtc && !Directory.EnumerateFileSystemEntries(folder).Any())
            {
                Directory.Delete(folder, recursive: false);
                return 1;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Entrou arquivo no meio, ou a pasta está em uso: fica para a próxima limpeza
        }

        return 0;
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

    private static async Task WriteAtomicallyAsync(string path, byte[] content, CancellationToken cancellationToken, bool overwrite = false)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temp, content, cancellationToken);
            File.Move(temp, path, overwrite);
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
