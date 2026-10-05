using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.Infrastructure.Ads;

/// <summary>
/// Lê a fila de revisão com EF (filtro e ordem fixos; o Dapper fica para as listas com filtros dinâmicos, ADR-004). O mais antigo vem primeiro e, no empate de data,
/// o de menor id, para a ordem ser sempre a mesma.
/// </summary>
public sealed class ReviewQueue(AppDbContext context, ICategoryTree tree) : IReviewQueue
{
    public async Task<IReadOnlyList<ReviewQueueItem>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await (
            from ad in context.Ads.AsNoTracking()
            join author in context.Users.AsNoTracking() on ad.AuthorId equals author.Id
            where ad.Status == AdStatus.InReview
            let sentAt = ad.SentAt ?? ad.CreatedAt
            orderby sentAt, ad.Id
            select new { ad.Id, ad.Title, AuthorName = author.FullName, ad.CategoryId, SentAt = sentAt })
            .ToListAsync(cancellationToken);

        CategoryTreeSnapshot snapshot = await tree.GetAsync(cancellationToken);
        return [.. rows.Select(r => new ReviewQueueItem(
            r.Id, r.Title, r.AuthorName, r.CategoryId is { } id ? snapshot.Find(id)?.Name : null, r.SentAt))];
    }
}
