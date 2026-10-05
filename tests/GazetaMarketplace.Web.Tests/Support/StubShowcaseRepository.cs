using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Showcase;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>
/// O repositório de leitura da vitrine nos testes do site (SQLite): o de verdade fala T-SQL com Dapper (<c>OUTER APPLY</c>, <c>OFFSET/FETCH</c>) e só roda no SQL Server
/// (provado na integração). Este devolve as linhas que o teste pôs em <see cref="Rows"/> <b>sem aplicar filtro nenhum</b> (a regra "só publicados" e a ordem são da consulta) e
/// guarda cada pedido recebido, para o teste conferir o que o serviço pediu (quantos, quais categorias, qual página).
/// </summary>
internal sealed class StubShowcaseRepository : IShowcaseReadRepository
{
    public List<ShowcaseRow> Rows { get; } = [];

    /// <summary>Total devolvido por <see cref="ByCategoryAsync"/>; nulo = o número de linhas.</summary>
    public int? Total { get; set; }

    /// <summary>Os pedidos de "mais recentes" (o <c>take</c> de cada um).</summary>
    public List<int> RecentRequests { get; } = [];

    /// <summary>Os pedidos de categoria: as categorias, a página e o tamanho.</summary>
    public List<(int[] CategoryIds, int Page, int PageSize)> CategoryRequests { get; } = [];

    /// <summary>Quando preenchido, toda leitura falha com esta exceção (US-001-S06).</summary>
    public Exception Failure { get; set; }

    public Task<IReadOnlyList<ShowcaseRow>> RecentAsync(int take, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        RecentRequests.Add(take);
        return Task.FromResult<IReadOnlyList<ShowcaseRow>>([.. Rows.Take(take)]);
    }

    /// <summary>Os pedidos por ids (cada lista de ids).</summary>
    public List<int[]> IdRequests { get; } = [];

    // Devolve as linhas pedidas que o teste pôs em Rows (as que "estão publicadas"), fora da ordem pedida de propósito, para o serviço provar que ordena
    public Task<IReadOnlyList<ShowcaseRow>> ByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        IdRequests.Add([.. ids]);
        return Task.FromResult<IReadOnlyList<ShowcaseRow>>([.. Rows.Where(r => ids.Contains(r.Id)).OrderBy(r => r.Id)]);
    }

    public Task<ShowcaseRows> ByCategoryAsync(IReadOnlyList<int> categoryIds, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        CategoryRequests.Add(([.. categoryIds], page, pageSize));
        IReadOnlyList<ShowcaseRow> slice = [.. Rows.Skip((page - 1) * pageSize).Take(pageSize)];
        return Task.FromResult(new ShowcaseRows(slice, Total ?? Rows.Count));
    }
}
