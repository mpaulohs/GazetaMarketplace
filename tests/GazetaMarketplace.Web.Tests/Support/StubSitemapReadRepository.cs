using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Seo;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>
/// A leitura do mapa do site nos testes do site (SQLite): a de verdade é T-SQL com Dapper e só roda no SQL Server (provada na integração). Este devolve o que o teste pôs em <see cref="Ads"/> e
/// <see cref="CategoryIds"/> <b>sem filtro nenhum</b> (a regra "só publicados" é da consulta) e guarda o <c>take</c> de cada pedido.
/// </summary>
internal sealed class StubSitemapReadRepository : ISitemapReadRepository
{
    public List<SitemapAd> Ads { get; } = [];

    public List<int> CategoryIds { get; } = [];

    public List<int> TakeRequests { get; } = [];

    public Exception Failure { get; set; }

    public Task<IReadOnlyList<int>> PublishedCategoryIdsAsync(CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult<IReadOnlyList<int>>([.. CategoryIds.Distinct()]);
    }

    public Task<IReadOnlyList<SitemapAd>> PublishedAdsAsync(int take, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        TakeRequests.Add(take);
        return Task.FromResult<IReadOnlyList<SitemapAd>>([.. Ads.Take(take)]);
    }
}
