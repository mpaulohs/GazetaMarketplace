using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Search;
using GazetaMarketplace.Core.Showcase;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>
/// O repositório de busca nos testes do site (SQLite): o de verdade fala T-SQL com Dapper e só roda no SQL Server (provado na integração). Este devolve as linhas que o teste pôs em
/// <see cref="Rows"/> <b>sem aplicar filtro nenhum</b> e guarda cada pedido (<see cref="Requests"/>), para o teste conferir o que o serviço entendeu do endereço.
/// </summary>
internal sealed class StubSearchReadRepository : ISearchReadRepository
{
    public List<ShowcaseRow> Rows { get; } = [];

    /// <summary>Total devolvido; nulo = o número de linhas.</summary>
    public int? Total { get; set; }

    public List<SearchCriteria> Requests { get; } = [];

    /// <summary>Quando preenchido, toda consulta falha com esta exceção (US-002-S11).</summary>
    public Exception Failure { get; set; }

    public SearchCriteria Last => Requests[^1];

    public Task<ShowcaseRows> SearchAsync(SearchCriteria criteria, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        Requests.Add(criteria);
        IReadOnlyList<ShowcaseRow> slice = [.. Rows.Skip((criteria.Page - 1) * criteria.PageSize).Take(criteria.PageSize)];
        return Task.FromResult(new ShowcaseRows(slice, Total ?? Rows.Count));
    }
}
