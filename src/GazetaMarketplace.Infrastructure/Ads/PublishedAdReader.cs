using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.Infrastructure.Ads;

/// <inheritdoc cref="IPublishedAdReader"/>
/// <remarks>Consulta por chave primária, sem rastreamento e <b>com o filtro "Publicado" na própria consulta</b>: um anúncio em outra situação nunca chega ao código da página.</remarks>
public sealed class PublishedAdReader(AppDbContext context) : IPublishedAdReader
{
    public Task<Ad> FindPublishedAsync(int id, CancellationToken cancellationToken) =>
        context.Ads.AsNoTracking().Where(a => a.Id == id && a.Status == AdStatus.Published).SingleOrDefaultAsync(cancellationToken);

    public Task<int?> FindArchivedCategoryIdAsync(int id, CancellationToken cancellationToken) =>
        context.Ads.AsNoTracking().Where(a => a.Id == id && a.Status == AdStatus.Archived).Select(a => a.CategoryId).SingleOrDefaultAsync(cancellationToken);
}
