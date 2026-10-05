using System.Threading;
using Dapper;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Infrastructure.Data;

namespace GazetaMarketplace.Infrastructure.Ads;

/// <summary>
/// O que as consultas públicas de lista (vitrine e busca) têm em comum: as colunas do card, a capa (foto de menor posição, uma só linha por anúncio mesmo com posições
/// repetidas) e o tempo limite de 10 s (RC-15). Nenhuma lê nome, e-mail ou qualquer dado do autor.
/// </summary>
internal static class AdCardSql
{
    public const int CommandTimeoutSeconds = 10;

    public const string Columns =
        "a.Id, a.Title, a.CategoryId, a.PriceCents, a.Attributes, a.City, a.Uf, c.Id AS CoverPhotoId, c.Width AS CoverWidth, c.Height AS CoverHeight";

    public const string CoverJoin =
        "OUTER APPLY (SELECT TOP (@CoverCount) p.Id, p.Width, p.Height FROM AdPhotos p WHERE p.AdId = a.Id ORDER BY p.SortOrder, p.Id) c";

    public static ShowcaseRow ToRow(Row r) => new(r.Id, r.Title, r.CategoryId, r.PriceCents, r.Attributes, r.City, r.Uf, r.CoverPhotoId, r.CoverWidth, r.CoverHeight);

    public static CommandDefinition Command(SqlQuery query, CancellationToken cancellationToken) =>
        new(query.Sql, query.Parameters, commandTimeout: CommandTimeoutSeconds, cancellationToken: cancellationToken);

    public sealed class Row
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
