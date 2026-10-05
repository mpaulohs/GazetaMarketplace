using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>
/// O repositório de leitura da lista do painel nos testes do site (SQLite): o de verdade fala T-SQL com Dapper e só roda no SQL Server (provado na integração). Este devolve as
/// linhas que o teste pôs em <see cref="Rows"/>, fatiadas pela página pedida, <b>sem aplicar filtro nenhum</b> (não reimplementa a regra de filtro) e guarda a última
/// consulta recebida, para o teste conferir o que o serviço pediu (autoria, arquivados, termo normalizado, página).
/// </summary>
internal sealed class StubPanelAdListRepository : IPanelAdListReadRepository
{
    public List<PanelAdListRow> Rows { get; } = [];

    public PanelAdListQuery LastQuery { get; private set; }

    public List<PanelAdListQuery> Queries { get; } = [];

    public int InReviewCount { get; set; }

    /// <summary>Quando preenchido, a consulta falha com esta exceção (US-012-S08).</summary>
    public Exception Failure { get; set; }

    public Task<PanelAdListRows> ListAsync(PanelAdListQuery query, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        LastQuery = query;
        Queries.Add(query);
        IReadOnlyList<PanelAdListRow> page = [.. Rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)];
        return Task.FromResult(new PanelAdListRows(page, Rows.Count));
    }

    public Task<int> CountByStatusAsync(byte status, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult(InReviewCount);
    }
}
