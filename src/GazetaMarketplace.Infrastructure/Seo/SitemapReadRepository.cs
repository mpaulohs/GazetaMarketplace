using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using GazetaMarketplace.Core.Seo;
using GazetaMarketplace.Infrastructure.Data;

namespace GazetaMarketplace.Infrastructure.Seo;

/// <inheritdoc cref="ISitemapReadRepository"/>
/// <remarks>Dapper com o <see cref="SqlBuilder"/> (ADR-004): o filtro "só publicados" é o único de <see cref="SqlFragments.OnlyPublished"/> e o limite vai como parâmetro. Lê só id, título, data e categoria: nada do autor.</remarks>
public sealed class SitemapReadRepository(IDbConnection connection) : ISitemapReadRepository
{
    private const int CommandTimeoutSeconds = 10;

    private static readonly IReadOnlyDictionary<string, string> NoUserSort = new Dictionary<string, string>();

    public async Task<IReadOnlyList<int>> PublishedCategoryIdsAsync(CancellationToken cancellationToken)
    {
        SqlQuery query = new SqlBuilder().Select("DISTINCT a.CategoryId").From("Ads a").OnlyPublished().Where("a.CategoryId IS NOT NULL").Build();
        IEnumerable<int> ids = await connection.QueryAsync<int>(new CommandDefinition(query.Sql, query.Parameters, commandTimeout: CommandTimeoutSeconds, cancellationToken: cancellationToken));
        return [.. ids];
    }

    public async Task<IReadOnlyList<SitemapAd>> PublishedAdsAsync(int take, CancellationToken cancellationToken)
    {
        if (take <= 0)
        {
            return [];
        }

        // TOP com parâmetro: o Page do SqlBuilder é para páginas de tela (no máximo 100) e o mapa lê até 50.000
        SqlQuery query = new SqlBuilder().Select("TOP (@Take) a.Id, a.Title, a.PublishedAt").From("Ads a").OnlyPublished()
            .Parameter("Take", take)
            .OrderBy(null, null, NoUserSort, "a.PublishedAt DESC, a.Id DESC")
            .Build();
        IEnumerable<SitemapAd> rows = await connection.QueryAsync<SitemapAd>(new CommandDefinition(query.Sql, query.Parameters, commandTimeout: CommandTimeoutSeconds, cancellationToken: cancellationToken));
        return [.. rows];
    }
}
