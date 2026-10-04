using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Infrastructure.Photos;

/// <summary>O que uma rodada da limpeza fez.</summary>
/// <param name="OriginalsDeleted">Originais com mais de 30 dias apagados do disco.</param>
/// <param name="OriginalKeysCleared">Linhas de <c>AdPhotos</c> que tiveram <c>OriginalKey</c> anulada.</param>
/// <param name="OrphansDeleted">Arquivos sem registro (versões órfãs e temporários) apagados.</param>
/// <param name="FoldersRemoved">Pastas vazias removidas.</param>
/// <param name="Failures">Arquivos que não puderam ser apagados (presos ou sem permissão); ficam para a próxima rodada.</param>
public sealed record PhotoCleanupResult(int OriginalsDeleted, int OriginalKeysCleared, int OrphansDeleted, int FoldersRemoved, int Failures);

/// <summary>
/// Limpeza diária das fotos (ADR-005): apaga os <b>originais</b> com mais de 30 dias e os <b>arquivos órfãos</b>. Roda 1 minuto depois da partida e a cada 24 horas;
/// como o IIS recicla o processo, na prática roda a cada reinício também (apagar de novo é inofensivo, e um atraso não perde nada: tudo o que passou do prazo sai).
/// </summary>
/// <remarks>
/// <para>
/// <b>Originais:</b> a idade vem da data de gravação do <i>arquivo</i>, não de <c>AdPhotos</c>: o original de uma foto removida já não tem linha no banco. Depois de
/// apagar, <c>OriginalKey</c> é anulada em lotes de 500, uma instrução <c>UPDATE … WHERE OriginalKey IN (…)</c> por lote. Se o processo cair entre apagar e anular, a chave
/// aponta para um arquivo ausente, o que o reprocessamento já trata como "original indisponível".
/// </para>
/// <para>
/// <b>Órfãos:</b> versões WebP em <c>&lt;adId&gt;/</c> sem linha em <c>AdPhotos</c> (o processo caiu entre gravar e registrar, ou a remoção não conseguiu apagar o arquivo) e
/// <c>*.tmp</c> de gravações interrompidas. Só valem os com mais de 24 horas: um envio grava os arquivos segundos antes de registrar a foto. <b>Nunca</b> apaga uma versão com
/// registro, por mais velha que seja, nem o que não tem o nome gerado pelo site (a pasta <c>_magick</c>, por exemplo).
/// </para>
/// </remarks>
public sealed class OriginalsCleanupService(
    IServiceScopeFactory scopes,
    IPhotoStorageMaintenance storage,
    IOptions<PhotoStorageOptions> options,
    TimeProvider time,
    ILogger<OriginalsCleanupService> log) : BackgroundService
{
    private static readonly TimeSpan FirstRun = TimeSpan.FromMinutes(1);

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private static readonly PhotoCleanupResult Nothing = new(0, 0, 0, 0, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(FirstRun, time, stoppingToken);
            using PeriodicTimer timer = new(Interval, time);
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // desligamento do site
        }
    }

    /// <summary>Uma limpeza. Falha de banco ou de disco vai para o log e a rotina tenta de novo no próximo dia.</summary>
    public async Task<PhotoCleanupResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        string baseFolder = options.Value.BasePath;
        if (string.IsNullOrWhiteSpace(baseFolder) || !Directory.Exists(baseFolder))
        {
            log.LogWarning("Limpeza das fotos pulada: a pasta de fotos (PhotoStorage:BasePath) não está configurada ou não existe");
            return Nothing;
        }

        Counters counters = new();
        try
        {
            using IServiceScope scope = scopes.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DateTime now = time.GetUtcNow().UtcDateTime;
            await SweepAsync(context, now, counters, cancellationToken);
            counters.Folders = storage.RemoveEmptyFolders(now - PhotoLimits.OrphanGrace);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            log.LogError(error, "Falha na limpeza das fotos");
        }

        PhotoCleanupResult result = new(counters.Originals, counters.Keys, counters.Orphans, counters.Folders, counters.Failures);
        log.LogInformation(
            "Limpeza das fotos: {Originals} originais apagados ({Keys} OriginalKey anuladas), {Orphans} arquivos órfãos apagados, {Folders} pastas vazias removidas, {Failures} falhas",
            result.OriginalsDeleted, result.OriginalKeysCleared, result.OrphansDeleted, result.FoldersRemoved, result.Failures);
        return result;
    }

    private async Task SweepAsync(AppDbContext context, DateTime now, Counters counters, CancellationToken cancellationToken)
    {
        List<string> deletedOriginals = [];
        List<StoredFile> versions = [];

        foreach (StoredFile file in storage.List())
        {
            cancellationToken.ThrowIfCancellationRequested();
            TimeSpan age = now - file.LastWriteUtc;
            switch (file.Kind)
            {
                case StoredFileKind.Original when age > PhotoLimits.OriginalRetention:
                    if (TryDelete(file, "Original apagado", counters))
                    {
                        counters.Originals++;
                        deletedOriginals.Add(file.Path);
                    }

                    break;
                case StoredFileKind.Temporary when age > PhotoLimits.OrphanGrace:
                    if (TryDelete(file, "Arquivo temporário apagado", counters))
                    {
                        counters.Orphans++;
                    }

                    break;
                case StoredFileKind.Version when age > PhotoLimits.OrphanGrace:
                    versions.Add(file);
                    break;
                default:
                    break;
            }

            if (deletedOriginals.Count >= PhotoLimits.CleanupBatchSize)
            {
                await ClearOriginalKeysAsync(context, deletedOriginals, counters, cancellationToken);
            }

            if (versions.Count >= PhotoLimits.CleanupBatchSize)
            {
                await DeleteOrphanVersionsAsync(context, versions, counters, cancellationToken);
            }
        }

        await ClearOriginalKeysAsync(context, deletedOriginals, counters, cancellationToken);
        await DeleteOrphanVersionsAsync(context, versions, counters, cancellationToken);
    }

    // Uma instrução UPDATE por lote
    private static async Task ClearOriginalKeysAsync(AppDbContext context, List<string> keys, Counters counters, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
        {
            return;
        }

        string[] batch = [.. keys];
        keys.Clear();
        counters.Keys += await context.AdPhotos
            .Where(p => batch.Contains(p.OriginalKey))
            .ExecuteUpdateAsync(set => set.SetProperty(p => p.OriginalKey, (string)null), cancellationToken);
    }

    private async Task DeleteOrphanVersionsAsync(AppDbContext context, List<StoredFile> files, Counters counters, CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            return;
        }

        StoredFile[] batch = [.. files];
        files.Clear();
        string[] keys = [.. batch.Select(f => f.StorageKey).Distinct()];
        HashSet<string> registered = [.. await context.AdPhotos.AsNoTracking().Where(p => keys.Contains(p.StorageKey)).Select(p => p.StorageKey).ToListAsync(cancellationToken)];

        foreach (StoredFile file in batch.Where(f => !registered.Contains(f.StorageKey)))
        {
            if (TryDelete(file, "Arquivo órfão apagado", counters))
            {
                counters.Orphans++;
            }
        }
    }

    private bool TryDelete(StoredFile file, string message, Counters counters)
    {
        try
        {
            storage.Delete(file.Path);
            log.LogInformation("{Message}: {Path}", message, file.Path);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            counters.Failures++;
            log.LogWarning(error, "Não foi possível apagar {Path}; fica para a próxima limpeza", file.Path);
            return false;
        }
    }

    private sealed class Counters
    {
        public int Originals { get; set; }

        public int Keys { get; set; }

        public int Orphans { get; set; }

        public int Folders { get; set; }

        public int Failures { get; set; }
    }
}
