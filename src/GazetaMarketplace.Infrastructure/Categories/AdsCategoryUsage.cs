using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.Infrastructure.Categories;

/// <summary>
/// Quantos anúncios usam cada categoria, em <b>qualquer situação</b> (rascunho, em revisão, publicado, rejeitado ou arquivado). Usa o mesmo
/// <see cref="AppDbContext"/> do escopo, então, dentro da transação do <c>CategoryManagement</c>, enxerga o que a transação enxerga. A chave estrangeira
/// <c>Ads.CategoryId</c> (sem exclusão em cascata) é a última defesa contra uma corrida entre excluir a categoria e criar o anúncio.
/// </summary>
public sealed class AdsCategoryUsage(AppDbContext context) : ICategoryUsage
{
    public async Task<IReadOnlyDictionary<int, int>> CountAdsAsync(IReadOnlyCollection<int> categoryIds, CancellationToken cancellationToken)
    {
        int[] ids = [.. categoryIds.Distinct()];
        Dictionary<int, int> counts = ids.ToDictionary(id => id, _ => 0);
        if (ids.Length == 0)
        {
            return counts;
        }

        var found = await context.Ads.AsNoTracking()
            .Where(a => a.CategoryId != null && ids.Contains(a.CategoryId.Value))
            .GroupBy(a => a.CategoryId.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        foreach (var row in found)
        {
            counts[row.Id] = row.Count;
        }

        return counts;
    }
}
