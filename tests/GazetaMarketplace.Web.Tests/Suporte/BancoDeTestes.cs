using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entidades;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GazetaMarketplace.Web.Tests.Suporte;

/// <summary>Entidade só de teste: a tarefa 0.6 ainda não tem entidade editável real.</summary>
internal sealed class EntidadeDeTeste : BaseEntity
{
    public string Nome { get; set; }
}

internal sealed class AppDbContextDeTeste(
    DbContextOptions<AppDbContextDeTeste> opcoes, IUsuarioAtual usuario, TimeProvider tempo) : AppDbContext(opcoes, usuario, tempo)
{
    public DbSet<EntidadeDeTeste> Entidades => Set<EntidadeDeTeste>();

    // O EF lê o valor gerado com RETURNING, que no SQLite não enxerga o que um trigger AFTER altera.
    // Depois de salvar, recarrega o token (o SQL Server real devolve o rowversion certo; ver /test).
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        List<EntityEntry> tocadas = ChangeTracker.Entries<BaseEntity>().Where(e => e.State is EntityState.Added or EntityState.Modified).Cast<EntityEntry>().ToList();
        int total = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        foreach (EntityEntry entrada in tocadas)
        {
            await entrada.ReloadAsync(cancellationToken);
        }

        return total;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        List<EntityEntry> tocadas = ChangeTracker.Entries<BaseEntity>().Where(e => e.State is EntityState.Added or EntityState.Modified).Cast<EntityEntry>().ToList();
        int total = base.SaveChanges(acceptAllChangesOnSuccess);
        foreach (EntityEntry entrada in tocadas)
        {
            entrada.Reload();
        }

        return total;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<EntidadeDeTeste>(e =>
        {
            e.ToTable("Entidades");
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
        });
    }
}

/// <summary>
/// Banco SQLite em memória (Template A de rules/testing.md). O SQLite não tem rowversion: o fixture
/// o simula com triggers (randomblob). O comportamento real é provado no /test (ConcorrenciaSqlServerTests).
/// </summary>
internal sealed class BancoDeTestes : IDisposable
{
    public BancoDeTestes()
    {
        Conexao = new SqliteConnection("DataSource=:memory:");
        Conexao.Open();

        using AppDbContextDeTeste contexto = NovoContexto();
        contexto.Database.EnsureCreated();
        contexto.Database.ExecuteSqlRaw(
            "CREATE TRIGGER Entidades_rv_insert AFTER INSERT ON Entidades BEGIN UPDATE Entidades SET RowVersion = randomblob(8) WHERE rowid = NEW.rowid; END;");
        contexto.Database.ExecuteSqlRaw(
            "CREATE TRIGGER Entidades_rv_update AFTER UPDATE ON Entidades BEGIN UPDATE Entidades SET RowVersion = randomblob(8) WHERE rowid = NEW.rowid; END;");
    }

    public SqliteConnection Conexao { get; }

    public UsuarioFalso Usuario { get; } = new();

    public RelogioFalso Relogio { get; } = new();

    public AppDbContextDeTeste NovoContexto() =>
        new(new DbContextOptionsBuilder<AppDbContextDeTeste>()
            .UseSqlite(Conexao, sqlite => sqlite.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options, Usuario, Relogio);

    /// <summary>O contexto de produção na mesma conexão (as migrations pertencem a ele, não ao derivado de teste).</summary>
    public AppDbContext NovoAppDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(Conexao, sqlite => sqlite.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options, Usuario, Relogio);

    public void Dispose() => Conexao.Dispose();
}
