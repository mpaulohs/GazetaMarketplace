using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Seo;

/// <summary>Leitura, só dos anúncios <b>publicados</b>, do que o mapa do site precisa.</summary>
public interface ISitemapReadRepository
{
    /// <summary>As categorias em que há pelo menos um anúncio publicado (só as que o anúncio usa, sem os ancestrais).</summary>
    Task<IReadOnlyList<int>> PublishedCategoryIdsAsync(CancellationToken cancellationToken);

    /// <summary>Os publicados mais recentes primeiro, no máximo <paramref name="take"/>.</summary>
    Task<IReadOnlyList<SitemapAd>> PublishedAdsAsync(int take, CancellationToken cancellationToken);
}

public sealed record SitemapAd(int Id, string Title, System.DateTime? PublishedAt);

public interface ISitemap
{
    /// <summary>Início, as categorias com pelo menos um anúncio publicado (inclusive as que só têm anúncio em uma subcategoria) e todos os anúncios publicados, até <see cref="SitemapDocument.MaxUrls"/>.</summary>
    Task<IReadOnlyList<SitemapEntry>> EntriesAsync(CancellationToken cancellationToken);
}
