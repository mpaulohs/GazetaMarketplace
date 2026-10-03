using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Core.VehicleCatalog;
using GazetaMarketplace.Infrastructure.Data.Configurations;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GazetaMarketplace.Infrastructure.Data;

/// <summary>
/// Contexto de escrita e de migrations (ADR-004), com as tabelas do Identity (ADR-003, chave <c>int</c>).
/// Auditoria automática em UTC, concorrência otimista por rowversion e auditoria de ações só de acréscimo.
/// </summary>
public class AppDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;
    private readonly ICategoryTree _categoryTree;
    private bool _categoriesChanged;

    /// <param name="options">Provedor e conexão.</param>
    /// <param name="currentUser">Quem está agindo, para as colunas de auditoria.</param>
    /// <param name="time">Relógio.</param>
    /// <param name="categoryTree">Cache da árvore de categorias, esvaziado aqui depois de gravar qualquer categoria. Opcional: ferramentas e testes sem cache passam nulo.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser, TimeProvider time, ICategoryTree categoryTree = null)
        : this((DbContextOptions)options, currentUser, time, categoryTree)
    {
    }

    protected AppDbContext(DbContextOptions options, ICurrentUser currentUser, TimeProvider time, ICategoryTree categoryTree = null)
        : base(options)
    {
        _currentUser = currentUser;
        _time = time;
        _categoryTree = categoryTree;
    }

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<PasswordRecoveryAttempt> PasswordRecoveryAttempts => Set<PasswordRecoveryAttempt>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    public DbSet<VehicleBrand> VehicleBrands => Set<VehicleBrand>();

    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();

    public DbSet<VehicleModelYear> VehicleModelYears => Set<VehicleModelYear>();

    public DbSet<VehicleVersion> VehicleVersions => Set<VehicleVersion>();

    public DbSet<Ad> Ads => Set<Ad>();

    public DbSet<AdPhoto> AdPhotos => Set<AdPhoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Constrói as tabelas do Identity antes das configurações do projeto
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Colunas calculadas por JSON e CHECKs só existem no SQL Server; os testes unitários em SQLite rodam sem eles
        if (!Database.IsSqlServer())
        {
            AdConfiguration.RemoveSqlServerOnlyFeatures(modelBuilder);
        }

        // Toda entidade editável tem rowversion (conflito vira ConflictException)
        foreach (IMutableEntityType type in modelBuilder.Model.GetEntityTypes().Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(type.ClrType).Property(nameof(BaseEntity.RowVersion)).IsRowVersion();
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
        Prepare();
        try
        {
            int saved = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            InvalidateCategoryTreeIfNeeded();
            return saved;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("O registro foi alterado por outra pessoa. Recarregue a página e tente de novo.");
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        Prepare();
        try
        {
            int saved = base.SaveChanges(acceptAllChangesOnSuccess);
            InvalidateCategoryTreeIfNeeded();
            return saved;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("O registro foi alterado por outra pessoa. Recarregue a página e tente de novo.");
        }
    }

    // Qualquer gravação de categoria, venha de onde vier (tela, carga, teste), esvazia o cache da árvore. Fica aqui e não num
    // interceptor do EF porque os hosts de teste trocam as opções do contexto e perderiam o interceptor. Atenção: grava-se antes
    // do commit de uma transação explícita; quem usa transação deve chamar ICategoryTree.Invalidate() depois do commit.
    // ExecuteUpdate/ExecuteDelete não passam por aqui e também exigem Invalidate().
    private void InvalidateCategoryTreeIfNeeded()
    {
        if (_categoriesChanged)
        {
            _categoriesChanged = false;
            _categoryTree?.Invalidate();
        }
    }

    private void Prepare()
    {
        _categoriesChanged = ChangeTracker.Entries<Category>().Any(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        DateTime now = _time.GetUtcNow().UtcDateTime;
        int? user = _currentUser.UserId;

        foreach (EntityEntry<BaseEntity> entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = user;
            }
            else if (entry.State == EntityState.Modified)
            {
                // A criação nunca muda, mesmo que quem chama tente
                entry.Property(e => e.CreatedAt).IsModified = false;
                entry.Property(e => e.CreatedBy).IsModified = false;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = user;
            }
        }

        foreach (EntityEntry<AdPhoto> photo in ChangeTracker.Entries<AdPhoto>().Where(e => e.State == EntityState.Added))
        {
            photo.Entity.CreatedAt = now;
        }

        // RC-16: a auditoria de ações só recebe acréscimos
        if (ChangeTracker.Entries<AuditEntry>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("AuditEntries é só de acréscimo: não é permitido alterar nem apagar entradas (RC-16).");
        }
    }
}
