using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;

namespace GazetaMarketplace.Core.Seo;

/// <inheritdoc cref="ISitemap"/>
public sealed class SitemapService(ISitemapReadRepository repository, ICategoryTree tree) : ISitemap
{
    private const string HomePath = "/";

    public async Task<IReadOnlyList<SitemapEntry>> EntriesAsync(CancellationToken cancellationToken)
    {
        CategoryTreeSnapshot snapshot = await tree.GetAsync(cancellationToken);
        IReadOnlyList<int> used = await repository.PublishedCategoryIdsAsync(cancellationToken);

        // A página de uma categoria mostra também os anúncios das descendentes: o ancestral entra no mapa junto
        HashSet<int> withAds = [];
        foreach (int id in used)
        {
            if (snapshot.Find(id) is null)
            {
                continue;
            }

            withAds.Add(id);
            foreach (CategoryNode ancestor in snapshot.AncestorsOf(id))
            {
                withAds.Add(ancestor.Id);
            }
        }

        List<SitemapEntry> entries = [new(HomePath, null)];
        entries.AddRange(snapshot.All.Where(c => withAds.Contains(c.Id)).Select(c => new SitemapEntry("/categoria/" + c.Slug, null)));

        IReadOnlyList<SitemapAd> ads = await repository.PublishedAdsAsync(SitemapDocument.MaxUrls - entries.Count, cancellationToken);
        entries.AddRange(ads.Select(a => new SitemapEntry(AdRoutes.Detail(a.Id, a.Title), a.PublishedAt)));
        return entries;
    }
}
