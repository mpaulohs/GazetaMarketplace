using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Categories;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Conta os comandos SQL enviados ao banco e deixa o teste agir no meio de uma leitura.</summary>
internal sealed class CommandCounter : DbCommandInterceptor
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    /// <summary>Executada logo depois de cada leitura terminar (já com o resultado do banco nas mãos do EF).</summary>
    public Action AfterRead { get; set; }

    public void Reset() => Interlocked.Exchange(ref _count, 0);

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Interlocked.Increment(ref _count);
        AfterRead?.Invoke();
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _count);
        AfterRead?.Invoke();
        return ValueTask.FromResult(result);
    }
}

/// <summary>SQLite em memória com o esquema e a carga inicial de categorias (<c>EnsureCreated</c>), o <see cref="CategoryTree"/> real e um relógio controlável.</summary>
internal sealed class CategoryDb : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _services;

    public CategoryDb()
    {
        _connection.Open();
        Clock = new FakeClock();
        Counter = new CommandCounter();

        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(Clock);
        services.AddScoped<ICurrentUser, FakeCurrentUser>();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection).AddInterceptors(Counter));
        services.AddSingleton<CategoryTree>();
        services.AddSingleton<ICategoryTree>(provider => provider.GetRequiredService<CategoryTree>());
        _services = services.BuildServiceProvider();

        using (IServiceScope scope = _services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        }

        Counter.Reset();
    }

    public FakeClock Clock { get; }

    public CommandCounter Counter { get; }

    public ICategoryTree Tree => _services.GetRequiredService<ICategoryTree>();

    /// <summary>Executa um trabalho com um contexto novo (o mesmo que o site usaria numa requisição).</summary>
    public async Task<T> WithContextAsync<T>(Func<AppDbContext, Task<T>> work)
    {
        using IServiceScope scope = _services.CreateScope();
        return await work(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public Task WithContextAsync(Func<AppDbContext, Task> work) => WithContextAsync<int>(async context =>
    {
        await work(context);
        return 0;
    });

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
    }
}
