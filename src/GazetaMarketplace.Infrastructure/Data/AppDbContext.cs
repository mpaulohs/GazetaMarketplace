using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entidades;
using GazetaMarketplace.Core.Excecoes;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Identidade;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GazetaMarketplace.Infrastructure.Data;

/// <summary>
/// Contexto de escrita e de migrations (ADR-004), com as tabelas do Identity (ADR-003, chave <c>int</c>).
/// Auditoria automática em UTC, concorrência otimista por rowversion e auditoria de ações só de acréscimo.
/// </summary>
public class AppDbContext : IdentityDbContext<UsuarioIdentity, PapelIdentity, int>
{
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly TimeProvider _tempo;

    public AppDbContext(DbContextOptions<AppDbContext> options, IUsuarioAtual usuarioAtual, TimeProvider tempo)
        : this((DbContextOptions)options, usuarioAtual, tempo)
    {
    }

    protected AppDbContext(DbContextOptions options, IUsuarioAtual usuarioAtual, TimeProvider tempo)
        : base(options)
    {
        _usuarioAtual = usuarioAtual;
        _tempo = tempo;
    }

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Constrói as tabelas do Identity antes das configurações do projeto
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Toda entidade editável tem rowversion (conflito vira ConflictException)
        foreach (IMutableEntityType tipo in modelBuilder.Model.GetEntityTypes().Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(tipo.ClrType).Property(nameof(BaseEntity.RowVersion)).IsRowVersion();
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Datas são gravadas e lidas em UTC (NFR-20)
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>().HaveColumnType("datetime2");
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcNullableDateTimeConverter>().HaveColumnType("datetime2");
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        Preparar();
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("O registro foi alterado por outra pessoa. Recarregue a página e tente de novo.");
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        Preparar();
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("O registro foi alterado por outra pessoa. Recarregue a página e tente de novo.");
        }
    }

    private void Preparar()
    {
        DateTime agora = _tempo.GetUtcNow().UtcDateTime;
        int? usuario = _usuarioAtual.UsuarioId;

        foreach (EntityEntry<BaseEntity> entrada in ChangeTracker.Entries<BaseEntity>())
        {
            if (entrada.State == EntityState.Added)
            {
                entrada.Entity.CreatedAt = agora;
                entrada.Entity.CreatedBy = usuario;
            }
            else if (entrada.State == EntityState.Modified)
            {
                // A criação nunca muda, mesmo que quem chama tente
                entrada.Property(e => e.CreatedAt).IsModified = false;
                entrada.Property(e => e.CreatedBy).IsModified = false;
                entrada.Entity.UpdatedAt = agora;
                entrada.Entity.UpdatedBy = usuario;
            }
        }

        // RC-16: a auditoria de ações só recebe acréscimos
        if (ChangeTracker.Entries<AuditEntry>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("AuditEntries é só de acréscimo: não é permitido alterar nem apagar entradas (RC-16).");
        }
    }
}
