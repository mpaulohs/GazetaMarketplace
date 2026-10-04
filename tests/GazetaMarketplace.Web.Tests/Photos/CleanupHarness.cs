using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Infrastructure.Photos;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Tests.Photos;

/// <summary>Relógio que só anda quando o teste manda: <c>GetUtcNow</c> e os temporizadores (<c>Task.Delay</c>, <c>PeriodicTimer</c>) seguem o mesmo tempo.</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private readonly List<ManualTimer> _timers = [];
    private DateTimeOffset _now;

    public ManualTimeProvider(DateTimeOffset now) => _now = now;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return _now;
        }
    }

    /// <summary>Quantos temporizadores estão armados (o serviço em segundo plano arma o dele de forma assíncrona depois do <c>StartAsync</c>).</summary>
    public int TimerCount
    {
        get
        {
            lock (_gate)
            {
                return _timers.Count(t => t.Due is not null);
            }
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
    {
        ManualTimer timer = new(this, callback, state);
        lock (_gate)
        {
            _timers.Add(timer);
        }

        timer.Change(dueTime, period);
        return timer;
    }

    /// <summary>Anda o relógio e dispara, em ordem, cada temporizador que venceu no caminho.</summary>
    public void Advance(TimeSpan by)
    {
        DateTimeOffset target;
        lock (_gate)
        {
            target = _now + by;
        }

        while (true)
        {
            ManualTimer next;
            lock (_gate)
            {
                next = _timers.Where(t => t.Due is not null && t.Due <= target).OrderBy(t => t.Due).FirstOrDefault();
                if (next is null)
                {
                    _now = target;
                    return;
                }

                _now = next.Due!.Value;
            }

            next.Fire();
        }
    }

    private void Remove(ManualTimer timer)
    {
        lock (_gate)
        {
            _timers.Remove(timer);
        }
    }

    private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object state) : ITimer
    {
        private TimeSpan _period = Timeout.InfiniteTimeSpan;

        public DateTimeOffset? Due { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            _period = period;
            Due = dueTime == Timeout.InfiniteTimeSpan ? null : owner.GetUtcNow() + dueTime;
            return true;
        }

        public void Fire()
        {
            Due = _period == Timeout.InfiniteTimeSpan || _period == TimeSpan.Zero ? null : owner.GetUtcNow() + _period;
            callback(state);
        }

        public void Dispose() => owner.Remove(this);

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>Guarda os comandos que não são consultas (os <c>UPDATE</c> do <c>ExecuteUpdate</c>), para provar "uma instrução por lote".</summary>
internal sealed class NonQueryRecorder : DbCommandInterceptor
{
    private readonly ConcurrentQueue<string> _commands = new();

    public IReadOnlyList<string> Commands => [.. _commands];

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        _commands.Enqueue(command.CommandText);
        return ValueTask.FromResult(result);
    }
}

/// <summary>Logger de teste que guarda as mensagens já formatadas.</summary>
internal sealed class ListLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new ListLogger(Entries);

    public void Dispose()
    {
    }

    private sealed class ListLogger(ConcurrentQueue<(LogLevel, string)> entries) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) =>
            entries.Enqueue((logLevel, formatter(state, exception)));
    }
}

/// <summary>Faz um arquivo escolhido falhar ao ser apagado, como um arquivo preso por outro processo; o resto passa para o armazenamento de verdade.</summary>
internal sealed class FlakyMaintenance(IPhotoStorageMaintenance inner, string failingPath) : IPhotoStorageMaintenance
{
    public IEnumerable<StoredFile> List() => inner.List();

    public void Delete(string path)
    {
        if (path == failingPath)
        {
            throw new IOException("arquivo preso por outro processo (teste)");
        }

        inner.Delete(path);
    }

    public int RemoveEmptyFolders(DateTime olderThanUtc) => inner.RemoveEmptyFolders(olderThanUtc);
}

/// <summary>
/// A limpeza e o reprocessamento de verdade sobre uma pasta temporária, SQLite em memória e um relógio manual. Os arquivos são criados com a data de gravação pedida
/// (<c>LastWriteTimeUtc</c>) e os nomes seguem o formato que o site gera.
/// </summary>
internal sealed class CleanupHarness : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _services;
    private int? _adId;

    public CleanupHarness(string basePath = null, Func<IPhotoStorageMaintenance, IPhotoStorageMaintenance> wrap = null)
    {
        _connection.Open();
        Folder = Path.Combine(Path.GetTempPath(), "gazeta-limpeza-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Folder);
        Time = new ManualTimeProvider(new DateTimeOffset(DateTime.UtcNow.Date.AddHours(12), TimeSpan.Zero));
        Recorder = new NonQueryRecorder();
        Logs = new ListLoggerProvider();
        Options = Microsoft.Extensions.Options.Options.Create(new PhotoStorageOptions { BasePath = basePath ?? Folder });

        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(Time);
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser());
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection).AddInterceptors(Recorder));
        _services = services.BuildServiceProvider();
        using (IServiceScope scope = _services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        }

        Storage = new FileSystemPhotoStorage(Options);
        Maintenance = wrap is null ? Storage : wrap(Storage);
        Service = new OriginalsCleanupService(
            _services.GetRequiredService<IServiceScopeFactory>(), Maintenance, Options, Time,
            LoggerFactory.Create(builder => builder.AddProvider(Logs)).CreateLogger<OriginalsCleanupService>());
    }

    public string Folder { get; }

    public ManualTimeProvider Time { get; }

    public NonQueryRecorder Recorder { get; }

    public ListLoggerProvider Logs { get; }

    public IOptions<PhotoStorageOptions> Options { get; }

    public FileSystemPhotoStorage Storage { get; }

    public IPhotoStorageMaintenance Maintenance { get; }

    public OriginalsCleanupService Service { get; }

    public DateTime Now => Time.GetUtcNow().UtcDateTime;

    public static string NewGuid() => Guid.NewGuid().ToString("N");

    /// <summary>A chave de um original no formato do site, no mês pedido.</summary>
    public static string OriginalKey(string month = "2026-08") => $"_originals/{month}/{NewGuid()}.jpg";

    /// <summary>Grava um arquivo (com o caminho relativo dado) e põe a data de gravação <paramref name="age"/> atrás de agora.</summary>
    public string Write(string relativePath, TimeSpan age)
    {
        string full = Path.Combine(Folder, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, [1, 2, 3]);
        File.SetLastWriteTimeUtc(full, Now - age);
        return relativePath;
    }

    public bool Exists(string relativePath) => File.Exists(Path.Combine(Folder, relativePath));

    /// <summary>Registra uma foto em <c>AdPhotos</c> (com o anúncio e o autor de que ela precisa).</summary>
    public async Task<int> AddPhotoAsync(string storageKey, string originalKey)
    {
        using IServiceScope scope = _services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (_adId is null)
        {
            AppUser user = new() { UserName = "autor@exemplo.com.br", NormalizedUserName = "AUTOR@EXEMPLO.COM.BR", Email = "autor@exemplo.com.br", FullName = "Autor" };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            Ad ad = Ad.CreateDraft("Anúncio das fotos", user.Id);
            db.Ads.Add(ad);
            await db.SaveChangesAsync();
            _adId = ad.Id;
        }

        AdPhoto photo = new() { AdId = _adId.Value, SortOrder = await db.AdPhotos.CountAsync(), StorageKey = storageKey, OriginalKey = originalKey, Width = 10, Height = 10, SizeBytes = 3 };
        db.AdPhotos.Add(photo);
        await db.SaveChangesAsync();
        return photo.Id;
    }

    /// <summary>Insere várias fotos de uma vez (uma só gravação).</summary>
    public async Task AddPhotosAsync(IEnumerable<(string StorageKey, string OriginalKey)> photos)
    {
        await AddPhotoAsync($"1/{NewGuid()}", null); // garante o autor e o anúncio
        using IServiceScope scope = _services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        int order = 100;
        foreach ((string storageKey, string originalKey) in photos)
        {
            db.AdPhotos.Add(new AdPhoto { AdId = _adId!.Value, SortOrder = order++, StorageKey = storageKey, OriginalKey = originalKey, Width = 10, Height = 10, SizeBytes = 3 });
        }

        await db.SaveChangesAsync();
    }

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> work)
    {
        using IServiceScope scope = _services.CreateScope();
        return await work(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public Task<AdPhoto> LoadPhotoAsync(int id) => WithDbAsync(db => db.AdPhotos.AsNoTracking().SingleAsync(p => p.Id == id));

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
        try
        {
            Directory.Delete(Folder, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
