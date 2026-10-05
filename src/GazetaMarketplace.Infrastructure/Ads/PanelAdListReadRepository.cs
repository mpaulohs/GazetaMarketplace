using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Infrastructure.Data;

namespace GazetaMarketplace.Infrastructure.Ads;

/// <inheritdoc cref="IPanelAdListReadRepository"/>
/// <remarks>
/// Dapper com o <see cref="SqlBuilder"/> (ADR-004): só fragmentos fixos, todo valor como parâmetro nomeado, ordem fixa (alterado em e id, do mais novo ao mais antigo; nenhuma ordenação vem
/// do pedido). A busca usa <c>CHARINDEX</c> sobre <c>TitleSearch</c>, que não tem curinga: <c>%</c>, <c>_</c> e <c>[</c> digitados valem como texto, sem escape. O termo chega
/// já normalizado pelo mesmo <see cref="GazetaMarketplace.Core.Search.Normalizer"/> que gravou a coluna. O nome do autor vem da junção com <c>AspNetUsers</c>; a categoria
/// é resolvida pelo serviço, pela árvore em memória.
/// </remarks>
public sealed class PanelAdListReadRepository(IDbConnection connection) : IPanelAdListReadRepository
{
    /// <summary>Tempo limite da consulta (RC-15): passou disso, a tela mostra o erro com "Tentar novamente".</summary>
    public const int CommandTimeoutSeconds = 10;

    private const string Changed = "COALESCE(a.UpdatedAt, a.CreatedAt)";

    private static readonly IReadOnlyDictionary<string, string> NoUserSort = new Dictionary<string, string>();

    public async Task<PanelAdListRows> ListAsync(PanelAdListQuery query, CancellationToken cancellationToken)
    {
        SqlQuery count = Filtered(new SqlBuilder().Select("COUNT(*)").From("Ads a"), query).Build();
        SqlQuery page = Filtered(
                new SqlBuilder()
                    .Select("a.Id, a.Title, a.AuthorId, u.FullName AS AuthorName, a.CategoryId, a.Status, " + Changed + " AS ChangedAt, a.RejectionReason")
                    .From("Ads a")
                    .Join("INNER JOIN AspNetUsers u ON u.Id = a.AuthorId"),
                query)
            .OrderBy(null, null, NoUserSort, Changed + " DESC, a.Id DESC")
            .Page(query.Page, query.PageSize)
            .Build();

        int total = await connection.ExecuteScalarAsync<int>(Command(count, cancellationToken));
        IEnumerable<Row> rows = await connection.QueryAsync<Row>(Command(page, cancellationToken));
        return new PanelAdListRows([.. rows.Select(r => new PanelAdListRow(r.Id, r.Title, r.AuthorId, r.AuthorName, r.CategoryId, r.Status, r.ChangedAt, r.RejectionReason))], total);
    }

    public async Task<int> CountByStatusAsync(byte status, CancellationToken cancellationToken)
    {
        SqlQuery count = new SqlBuilder().Select("COUNT(*)").From("Ads a").Where(SqlFragments.StatusIs).Parameter(SqlFragments.StatusParameter, status).Build();
        return await connection.ExecuteScalarAsync<int>(Command(count, cancellationToken));
    }

    private static SqlBuilder Filtered(SqlBuilder builder, PanelAdListQuery query)
    {
        if (query.AuthorId is { } author)
        {
            builder.Where("a.AuthorId = @AuthorId").Parameter("AuthorId", author);
        }

        if (query.Status is { } status)
        {
            builder.Where(SqlFragments.StatusIs).Parameter(SqlFragments.StatusParameter, status);
        }

        if (query.ExcludeArchived)
        {
            builder.Where(SqlFragments.NotArchived).Parameter(SqlFragments.ArchivedStatusParameter, AdStatus.Archived);
        }

        if (!string.IsNullOrEmpty(query.Search))
        {
            // Posição 1 em diante = o termo aparece em algum ponto do título
            builder.Where("CHARINDEX(@Search, a.TitleSearch) >= @FirstPosition").Parameter("Search", query.Search).Parameter("FirstPosition", 1);
        }

        return builder;
    }

    private static CommandDefinition Command(SqlQuery query, CancellationToken cancellationToken) =>
        new(query.Sql, query.Parameters, commandTimeout: CommandTimeoutSeconds, cancellationToken: cancellationToken);

    private sealed class Row
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public int AuthorId { get; set; }

        public string AuthorName { get; set; }

        public int? CategoryId { get; set; }

        public byte Status { get; set; }

        public System.DateTime ChangedAt { get; set; }

        public string RejectionReason { get; set; }
    }
}
