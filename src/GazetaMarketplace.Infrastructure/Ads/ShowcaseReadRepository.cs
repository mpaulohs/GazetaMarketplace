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
    public const int CommandTimeoutSeconds = 10;

    private const string Columns =
        "a.Id, a.Title, a.CategoryId, a.PriceCents, a.Attributes, a.City, a.Uf, c.Id AS CoverPhotoId, c.Width AS CoverWidth, c.Height AS CoverHeight";

    private const string CoverJoin =
        "OUTER APPLY (SELECT TOP (@CoverCount) p.Id, p.Width, p.Height FROM AdPhotos p WHERE p.AdId = a.Id ORDER BY p.SortOrder, p.Id) c";

    private const string Order = "a.PublishedAt DESC, a.Id DESC";

    private static readonly IReadOnlyDictionary<string, string> NoUserSort = new Dictionary<string, string>();

    public async Task<IReadOnlyList<ShowcaseRow>> RecentAsync(int take, CancellationToken cancellationToken)
    {
        SqlQuery query = Page(new SqlBuilder().Select(Columns).From("Ads a").Join(CoverJoin).OnlyPublished(), 1, take);
        IEnumerable<Row> rows = await connection.QueryAsync<Row>(Command(query, cancellationToken));
        return [.. rows.Select(ToRow)];
    }

    public async Task<ShowcaseRows> ByCategoryAsync(IReadOnlyList<int> categoryIds, int page, int pageSize, CancellationToken cancellationToken)
    {
        SqlQuery count = Filtered(new SqlBuilder().Select("COUNT(*)").From("Ads a"), categoryIds).Build();
        SqlQuery list = Page(Filtered(new SqlBuilder().Select(Columns).From("Ads a").Join(CoverJoin), categoryIds), page, pageSize);

        int total = await connection.ExecuteScalarAsync<int>(Command(count, cancellationToken));
        IEnumerable<Row> rows = await connection.QueryAsync<Row>(Command(list, cancellationToken));
        return new ShowcaseRows([.. rows.Select(ToRow)], total);
    }

    private static SqlBuilder Filtered(SqlBuilder builder, IReadOnlyList<int> categoryIds) =>
        builder.OnlyPublished().Where("a.CategoryId IN @CategoryIds").Parameter("CategoryIds", categoryIds);

    private static SqlQuery Page(SqlBuilder builder, int page, int size) =>
        builder.Parameter("CoverCount", 1).OrderBy(null, null, NoUserSort, Order).Page(page, size).Build();

    private static ShowcaseRow ToRow(Row r) => new(r.Id, r.Title, r.CategoryId, r.PriceCents, r.Attributes, r.City, r.Uf, r.CoverPhotoId, r.CoverWidth, r.CoverHeight);

    private static CommandDefinition Command(SqlQuery query, CancellationToken cancellationToken) =>
        new(query.Sql, query.Parameters, commandTimeout: CommandTimeoutSeconds, cancellationToken: cancellationToken);

    private sealed class Row
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public int? CategoryId { get; set; }

        public long? PriceCents { get; set; }

        public string Attributes { get; set; }

        public string City { get; set; }

        public string Uf { get; set; }

        public int? CoverPhotoId { get; set; }

        public int? CoverWidth { get; set; }

        public int? CoverHeight { get; set; }
    }
}
