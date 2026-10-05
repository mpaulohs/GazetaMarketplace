using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using GazetaMarketplace.Core.Search;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Infrastructure.Ads;
using GazetaMarketplace.Infrastructure.Data;

namespace GazetaMarketplace.Infrastructure.Search;

/// <inheritdoc cref="ISearchReadRepository"/>
/// <remarks>
/// Dapper com o <see cref="SqlBuilder"/> (ADR-004 e ADR-006): só fragmentos fixos e todo valor como parâmetro nomeado, inclusive o termo (<c>CHARINDEX(@Word0, a.TitleSearch)</c>, sem
/// <c>LIKE</c>, então <c>%</c>, <c>_</c> e <c>[</c> do texto valem como texto comum e nada precisa de escape). A ordem sai de um <c>switch</c> sobre o enum, com texto fixo: nada do
/// pedido entra no SQL. O filtro "só publicados" é o único de <see cref="SqlFragments.OnlyPublished"/>. Preço ou área informados excluem naturalmente os anúncios sem valor
/// (Serviços, A6), e os sem preço vão para o fim das duas ordenações por preço. Não lê nome, e-mail nem qualquer dado do autor.
/// </remarks>
public sealed class SearchReadRepository(IDbConnection connection) : ISearchReadRepository
{
    // O SqlBuilder recusa número solto no SQL (um valor concatenado seria um literal): os números que a consulta precisa vão como parâmetros
    private const string NoMatch = "NoMatch";
    private const string First = "First";
    private const string Last = "Last";

    private const string PriceLast = "CASE WHEN a.PriceCents IS NULL THEN @Last ELSE @First END";

    public async Task<ShowcaseRows> SearchAsync(SearchCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        SqlQuery count = Filtered(new SqlBuilder().Select("COUNT(*)").From("Ads a"), criteria).Build();
        SqlBuilder list = Filtered(new SqlBuilder().Select(AdCardSql.Columns).From("Ads a").Join(AdCardSql.CoverJoin), criteria)
            .Parameter("CoverCount", 1)
            .Parameter(First, 0)
            .Parameter(Last, 1)
            .OrderBy(null, null, new Dictionary<string, string>(), OrderOf(criteria.Order))
            .Page(criteria.Page, criteria.PageSize);

        int total = await connection.ExecuteScalarAsync<int>(AdCardSql.Command(count, cancellationToken));
        IEnumerable<AdCardSql.Row> rows = await connection.QueryAsync<AdCardSql.Row>(AdCardSql.Command(list.Build(), cancellationToken));
        return new ShowcaseRows([.. rows.Select(AdCardSql.ToRow)], total);
    }

    // Texto fixo por ordenação, sempre com o id decrescente para a ordem ser estável
    private static string OrderOf(SearchOrder order) => order switch
    {
        SearchOrder.PriceAscending => PriceLast + ", a.PriceCents ASC, a.Id DESC",
        SearchOrder.PriceDescending => PriceLast + ", a.PriceCents DESC, a.Id DESC",
        _ => "a.PublishedAt DESC, a.Id DESC"
    };

    private static SqlBuilder Filtered(SqlBuilder builder, SearchCriteria criteria)
    {
        builder.OnlyPublished().Parameter(NoMatch, 0);

        // Cada palavra precisa aparecer no título ou na descrição (colunas já normalizadas pela aplicação)
        for (int i = 0; i < criteria.Words.Count; i++)
        {
            string name = "Word" + i;
            builder.Where($"(CHARINDEX(@{name}, a.TitleSearch) > @{NoMatch} OR CHARINDEX(@{name}, a.DescriptionSearch) > @{NoMatch})").Parameter(name, criteria.Words[i]);
        }

        if (criteria.CategoryIds.Count > 0)
        {
            builder.Where("a.CategoryId IN @CategoryIds").Parameter("CategoryIds", criteria.CategoryIds);
        }

        if (criteria.Uf is not null)
        {
            builder.Where("a.Uf = @Uf").Parameter("Uf", criteria.Uf);
        }

        if (criteria.City is not null)
        {
            builder.Where("a.City = @City").Parameter("City", criteria.City);
        }

        Range(builder, "a.PriceCents", "Price", criteria.PriceMinCents, criteria.PriceMaxCents);

        if (criteria.BrandId is { } brand)
        {
            builder.Where("a.VehicleBrandId = @BrandId").Parameter("BrandId", brand);
        }

        if (criteria.ModelId is { } model)
        {
            builder.Where("a.VehicleModelId = @ModelId").Parameter("ModelId", model);
        }

        Range(builder, "a.ModelYear", "Year", criteria.YearFrom, criteria.YearTo);

        if (criteria.KmMax is { } km)
        {
            builder.Where("a.Km <= @KmMax").Parameter("KmMax", km);
        }

        Range(builder, "a.AreaM2", "Area", criteria.AreaMin, criteria.AreaMax);
        return builder;
    }

    // Faixa por parâmetros; coluna sem valor (NULL) nunca passa numa faixa informada
    private static void Range<T>(SqlBuilder builder, string column, string name, T? min, T? max)
        where T : struct
    {
        if (min is { } low)
        {
            builder.Where($"{column} >= @{name}Min").Parameter(name + "Min", low);
        }

        if (max is { } high)
        {
            builder.Where($"{column} <= @{name}Max").Parameter(name + "Max", high);
        }
    }
}
