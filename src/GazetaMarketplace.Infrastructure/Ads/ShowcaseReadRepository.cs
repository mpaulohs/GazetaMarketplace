using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Infrastructure.Data;

namespace GazetaMarketplace.Infrastructure.Ads;

/// <inheritdoc cref="IShowcaseReadRepository"/>
/// <remarks>
/// Dapper com o <see cref="SqlBuilder"/> (ADR-004): só fragmentos fixos e todo valor como parâmetro. O filtro "só publicados" é o único de <see cref="SqlFragments.OnlyPublished"/>;
/// a ordem é fixa (data de publicação e id, do mais novo ao mais antigo) e nenhuma parte dela vem do pedido. A capa é a foto de menor posição (<c>OUTER APPLY</c> com
/// <c>TOP (@CoverCount)</c>: uma só linha por anúncio mesmo que haja posições repetidas). Não lê nome, e-mail nem qualquer dado do autor.
/// </remarks>
public sealed class ShowcaseReadRepository(IDbConnection connection) : IShowcaseReadRepository
{
    /// <summary>Tempo limite da consulta (RC-15): passou disso, a página mostra o erro com "Tentar novamente".</summary>
    public const int CommandTimeoutSeconds = AdCardSql.CommandTimeoutSeconds;

    private const string Columns = AdCardSql.Columns;

    private const string CoverJoin = AdCardSql.CoverJoin;

    private const string Order = "a.PublishedAt DESC, a.Id DESC";

    private static readonly IReadOnlyDictionary<string, string> NoUserSort = new Dictionary<string, string>();

    public async Task<IReadOnlyList<ShowcaseRow>> RecentAsync(int take, CancellationToken cancellationToken)
    {
        SqlQuery query = Page(new SqlBuilder().Select(Columns).From("Ads a").Join(CoverJoin).OnlyPublished(), 1, take);
        IEnumerable<AdCardSql.Row> rows = await connection.QueryAsync<AdCardSql.Row>(Command(query, cancellationToken));
        return [.. rows.Select(ToRow)];
    }

    public async Task<ShowcaseRows> ByCategoryAsync(IReadOnlyList<int> categoryIds, int page, int pageSize, CancellationToken cancellationToken)
    {
        SqlQuery count = Filtered(new SqlBuilder().Select("COUNT(*)").From("Ads a"), categoryIds).Build();
        SqlQuery list = Page(Filtered(new SqlBuilder().Select(Columns).From("Ads a").Join(CoverJoin), categoryIds), page, pageSize);

        int total = await connection.ExecuteScalarAsync<int>(Command(count, cancellationToken));
        IEnumerable<AdCardSql.Row> rows = await connection.QueryAsync<AdCardSql.Row>(Command(list, cancellationToken));
        return new ShowcaseRows([.. rows.Select(ToRow)], total);
    }

    private static SqlBuilder Filtered(SqlBuilder builder, IReadOnlyList<int> categoryIds) =>
        builder.OnlyPublished().Where("a.CategoryId IN @CategoryIds").Parameter("CategoryIds", categoryIds);

    private static SqlQuery Page(SqlBuilder builder, int page, int size) =>
        builder.Parameter("CoverCount", 1).OrderBy(null, null, NoUserSort, Order).Page(page, size).Build();

    private static ShowcaseRow ToRow(AdCardSql.Row r) => AdCardSql.ToRow(r);

    private static CommandDefinition Command(SqlQuery query, CancellationToken cancellationToken) => AdCardSql.Command(query, cancellationToken);
}
