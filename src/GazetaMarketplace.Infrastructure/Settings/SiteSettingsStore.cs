using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Infrastructure.Caching;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Infrastructure.Settings;

/// <inheritdoc cref="ISiteSettings"/>
public sealed class SiteSettingsStore : ISiteSettings
{
    /// <summary>Quanto tempo as configurações ficam em cache: o mesmo da árvore de categorias.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly ExpiringCache<IReadOnlyDictionary<string, string>> _cache;

    public SiteSettingsStore(IServiceScopeFactory scopes, TimeProvider time) =>
        _cache = new ExpiringCache<IReadOnlyDictionary<string, string>>(time, Lifetime, cancellationToken => LoadAsync(scopes, cancellationToken));

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> all = await _cache.GetAsync(cancellationToken);
        return all.GetValueOrDefault(key);
    }

    public Task<string> GetPhoneAsync(CancellationToken cancellationToken) => GetAsync(SiteSettingKeys.Phone, cancellationToken);

    public async Task<bool> IsPhoneConfiguredAsync(CancellationToken cancellationToken) =>
        !string.IsNullOrEmpty(await GetPhoneAsync(cancellationToken));

    public void Invalidate() => _cache.Invalidate();

    // Todas as configurações numa consulta só: são poucas linhas
    private static async Task<IReadOnlyDictionary<string, string>> LoadAsync(IServiceScopeFactory scopes, CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopes.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await context.SiteSettings.AsNoTracking().Select(s => new { s.Key, s.Value }).ToArrayAsync(cancellationToken))
            .ToDictionary(s => s.Key, s => s.Value, StringComparer.Ordinal);
    }
}
