using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Entidade só de teste: a tarefa 0.6 ainda não tem entidade editável real.</summary>
internal sealed class TestEntity : BaseEntity
{
    public string Name { get; set; }
}

internal sealed class TestAppDbContext(
    DbContextOptions<TestAppDbContext> options, ICurrentUser user, TimeProvider time) : AppDbContext(options, user, time)
{
    public DbSet<TestEntity> Entities => Set<TestEntity>();

    // O EF lê o valor gerado com RETURNING, que no SQLite não enxerga o que um trigger AFTER altera.
    // Depois de salvar, recarrega o token (o SQL Server real devolve o rowversion certo; ver /test).
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        List<EntityEntry> touched = ChangeTracker.Entries<BaseEntity>().Where(e => e.State is EntityState.Added or EntityState.Modified).Cast<EntityEntry>().ToList();
        int total = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        foreach (EntityEntry entry in touched)
        {
            await entry.ReloadAsync(cancellationToken);
        }

        return total;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        List<EntityEntry> touched = ChangeTracker.Entries<BaseEntity>().Where(e => e.State is EntityState.Added or EntityState.Modified).Cast<EntityEntry>().ToList();
        int total = base.SaveChanges(acceptAllChangesOnSuccess);
        foreach (EntityEntry entry in touched)
        {
            entry.Reload();
        }

        return total;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestEntity>(e =>
        {
            e.ToTable("Entities");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });
    }
}

/// <summary>
/// Banco SQLite em memória (Template A de rules/testing.md). O SQLite não tem rowversion: o fixture
/// o simula com triggers (randomblob). O comportamento real é provado no /test (ConcorrenciaSqlServerTests).
/// </summary>
internal sealed class TestDatabase : IDisposable
{
    public TestDatabase()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        using TestAppDbContext context = NewContext();
        context.Database.EnsureCreated();
        context.Database.ExecuteSqlRaw(
            "CREATE TRIGGER Entities_rv_insert AFTER INSERT ON Entities BEGIN UPDATE Entities SET RowVersion = randomblob(8) WHERE rowid = NEW.rowid; END;");
        context.Database.ExecuteSqlRaw(
            "CREATE TRIGGER Entities_rv_update AFTER UPDATE ON Entities BEGIN UPDATE Entities SET RowVersion = randomblob(8) WHERE rowid = NEW.rowid; END;");
    }

    public SqliteConnection Connection { get; }

    public FakeCurrentUser User { get; } = new();

    public FakeClock Clock { get; } = new();

    public TestAppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAppDbContext>()
            .UseSqlite(Connection, sqlite => sqlite.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options, User, Clock);

    /// <summary>O contexto de produção na mesma conexão (as migrations pertencem a ele, não ao derivado de teste).</summary>
    public AppDbContext NewAppDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(Connection, sqlite => sqlite.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options, User, Clock);

    public void Dispose() => Connection.Dispose();
}
