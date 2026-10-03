using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;

namespace GazetaMarketplace.Infrastructure.Categories;

/// <summary>
/// Implementação provisória de <see cref="ICategoryUsage"/>: a tabela de anúncios só nasce na tarefa 3.1, então nenhuma categoria tem anúncios.
/// A 3.1 troca esta classe pela consulta real em <c>Ads</c>; o teste S08 passa a rodar contra anúncios de verdade.
/// </summary>
public sealed class PendingAdsCategoryUsage : ICategoryUsage
{
    public Task<IReadOnlyDictionary<int, int>> CountAdsAsync(IReadOnlyCollection<int> categoryIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<int, int>>(categoryIds.Distinct().ToDictionary(id => id, _ => 0));
}
