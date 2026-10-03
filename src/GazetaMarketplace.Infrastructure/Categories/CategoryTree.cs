using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Infrastructure.Caching;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Infrastructure.Categories;

/// <inheritdoc cref="ICategoryTree"/>
public sealed class CategoryTree : ICategoryTree
{
    /// <summary>Quanto tempo a árvore fica em cache (ARCHITECTURE §Cache).</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly ExpiringCache<CategoryTreeSnapshot> _cache;

    public CategoryTree(IServiceScopeFactory scopes, TimeProvider time) =>
        _cache = new ExpiringCache<CategoryTreeSnapshot>(time, Lifetime, cancellationToken => LoadAsync(scopes, cancellationToken));

    public Task<CategoryTreeSnapshot> GetAsync(CancellationToken cancellationToken) => _cache.GetAsync(cancellationToken);

    public void Invalidate() => _cache.Invalidate();

    // Uma consulta só, sem rastreamento: a árvore inteira tem cerca de 150 linhas
    private static async Task<CategoryTreeSnapshot> LoadAsync(IServiceScopeFactory scopes, CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopes.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        CategoryRow[] rows = await context.Categories.AsNoTracking()
            .Select(c => new CategoryRow(c.Id, c.ParentId, c.Name, c.Slug, c.DisplayOrder, c.IsPostable, c.FieldGroup, c.IsSystem))
            .ToArrayAsync(cancellationToken);
        return CategoryTreeSnapshot.Build(rows);
    }
}
