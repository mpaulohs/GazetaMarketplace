using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Infrastructure.Categories;

/// <inheritdoc cref="ICategoryTree"/>
public sealed class CategoryTree(IServiceScopeFactory scopes, TimeProvider time) : ICategoryTree
{
    /// <summary>Quanto tempo a árvore fica em cache (ARCHITECTURE §Cache).</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly SemaphoreSlim _loading = new(1, 1);
    private volatile Entry _cached;
    private int _version;

    public async Task<CategoryTreeSnapshot> GetAsync(CancellationToken cancellationToken)
    {
        Entry current = _cached;
        if (current is not null && time.GetUtcNow() < current.ExpiresAt)
        {
            return current.Snapshot;
        }

        await _loading.WaitAsync(cancellationToken);
        try
        {
            // Quem esperou na fila encontra a árvore que outra requisição acabou de carregar
            current = _cached;
            if (current is not null && time.GetUtcNow() < current.ExpiresAt)
            {
                return current.Snapshot;
            }

            int versionBeforeLoad = Volatile.Read(ref _version);
            CategoryTreeSnapshot snapshot = await LoadAsync(cancellationToken);

            // Se alguém invalidou durante a leitura, o que foi lido pode já estar velho: devolve, mas não guarda
            if (versionBeforeLoad == Volatile.Read(ref _version))
            {
                _cached = new Entry(snapshot, time.GetUtcNow() + Lifetime);
            }

            return snapshot;
        }
        finally
        {
            _loading.Release();
        }
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref _version);
        _cached = null;
    }

    // Uma consulta só, sem rastreamento: a árvore inteira tem cerca de 150 linhas
    private async Task<CategoryTreeSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopes.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        CategoryRow[] rows = await context.Categories.AsNoTracking()
            .Select(c => new CategoryRow(c.Id, c.ParentId, c.Name, c.Slug, c.DisplayOrder, c.IsPostable, c.FieldGroup, c.IsSystem))
            .ToArrayAsync(cancellationToken);
        return CategoryTreeSnapshot.Build(rows);
    }

    private sealed record Entry(CategoryTreeSnapshot Snapshot, DateTimeOffset ExpiresAt);
}
