using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.VehicleCatalog;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Infrastructure.VehicleCatalog;

/// <inheritdoc cref="IVehicleCatalog"/>
public sealed class VehicleCatalog(IServiceScopeFactory scopes, TimeProvider time) : IVehicleCatalog
{
    /// <summary>Quanto tempo cada lista fica em cache: o mesmo da árvore de categorias e das configurações.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    // Uma entrada por consulta já feita com sucesso. Só o que existe entra (um id inventado nunca vira entrada), então o tamanho
    // máximo é o do próprio catálogo; o limite de pedidos por IP já protege o banco das consultas que não encontram nada.
    private readonly ConcurrentDictionary<string, Entry> _cache = new(StringComparer.Ordinal);

    public Task<IReadOnlyList<CatalogItem>> BrandsAsync(string kind, CancellationToken cancellationToken)
    {
        RequireKind(kind);
        return CachedAsync($"brands|{kind}", async db =>
            await db.Set<VehicleBrand>().AsNoTracking().Where(b => b.Kind == kind)
                .OrderBy(b => b.Name).ThenBy(b => b.Id)
                .Select(b => new CatalogItem(b.Id, b.Name)).ToListAsync(cancellationToken), cancellationToken);
    }

    public Task<IReadOnlyList<CatalogItem>> ModelsAsync(string kind, int brandId, CancellationToken cancellationToken)
    {
        RequireKind(kind);
        return CachedAsync($"models|{kind}|{brandId}", async db =>
        {
            if (!await db.Set<VehicleBrand>().AnyAsync(b => b.Kind == kind && b.Id == brandId, cancellationToken))
            {
                throw new NotFoundException("Marca", brandId);
            }

            return await db.Set<VehicleModel>().AsNoTracking().Where(m => m.Kind == kind && m.BrandId == brandId)
                .OrderBy(m => m.Name).ThenBy(m => m.Id)
                .Select(m => new CatalogItem(m.Id, m.Name)).ToListAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> YearsAsync(string kind, int modelId, CancellationToken cancellationToken)
    {
        RequireKind(kind);
        IReadOnlyList<CatalogItem> years = await CachedAsync($"years|{kind}|{modelId}", async db =>
        {
            if (!await db.Set<VehicleModel>().AnyAsync(m => m.Kind == kind && m.Id == modelId, cancellationToken))
            {
                throw new NotFoundException("Modelo", modelId);
            }

            return await db.Set<VehicleModelYear>().AsNoTracking().Where(y => y.Kind == kind && y.ModelId == modelId)
                .OrderByDescending(y => y.Year)
                .Select(y => new CatalogItem(y.Year, string.Empty)).ToListAsync(cancellationToken);
        }, cancellationToken);
        return [.. years.Select(y => y.Id)];
    }

    public Task<IReadOnlyList<CatalogItem>> VersionsAsync(string kind, int modelId, int year, CancellationToken cancellationToken)
    {
        RequireKind(kind);
        return CachedAsync($"versions|{kind}|{modelId}|{year}", async db =>
        {
            if (!await db.Set<VehicleModelYear>().AnyAsync(y => y.Kind == kind && y.ModelId == modelId && y.Year == year, cancellationToken))
            {
                throw new NotFoundException($"Ano {year} do modelo {modelId} não encontrado.");
            }

            return await db.Set<VehicleVersion>().AsNoTracking().Where(v => v.Kind == kind && v.ModelId == modelId && v.Year == year)
                .OrderBy(v => v.Name).ThenBy(v => v.Id)
                .Select(v => new CatalogItem(v.Id, v.Name)).ToListAsync(cancellationToken);
        }, cancellationToken);
    }

    public void Invalidate() => _cache.Clear();

    private static void RequireKind(string kind)
    {
        if (!VehicleKinds.IsValid(kind))
        {
            throw new ArgumentException("O tipo do catálogo é 'car' ou 'moto'.", nameof(kind));
        }
    }

    private async Task<IReadOnlyList<CatalogItem>> CachedAsync(string key, Func<AppDbContext, Task<List<CatalogItem>>> load, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(key, out Entry entry) && time.GetUtcNow() < entry.ExpiresAt)
        {
            return entry.Items;
        }

        using IServiceScope scope = scopes.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        IReadOnlyList<CatalogItem> items = await load(db);
        _cache[key] = new Entry(items, time.GetUtcNow() + Lifetime);
        return items;
    }

    private sealed record Entry(IReadOnlyList<CatalogItem> Items, DateTimeOffset ExpiresAt);
}
