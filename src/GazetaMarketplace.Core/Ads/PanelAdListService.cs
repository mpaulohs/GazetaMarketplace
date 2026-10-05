using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Search;

namespace GazetaMarketplace.Core.Ads;

/// <inheritdoc cref="IPanelAdList"/>
public sealed class PanelAdListService(IPanelAdListReadRepository repository, ICategoryTree tree, ICurrentUser currentUser) : IPanelAdList
{
    public async Task<PanelAdListPage> ListAsync(string search, string situation, int page, CancellationToken cancellationToken)
    {
        string typed = (search ?? string.Empty).Trim();
        if (typed.Length > PanelAdListFilters.SearchMaxLength)
        {
            typed = typed[..PanelAdListFilters.SearchMaxLength].TrimEnd();
        }

        byte? status = PanelAdListFilters.ParseStatus(situation);
        // O Redator só vê os próprios anúncios; o autor vem da sessão e nenhum valor do pedido o troca
        int? authorId = currentUser.IsAdministrator ? null : currentUser.UserId ?? throw new InvalidOperationException("Lista do painel sem usuário.");
        PanelAdListQuery query = new(authorId, status, ExcludeArchived: status != AdStatus.Archived, Normalizer.Normalize(typed), Math.Max(page, 1), PanelAdListFilters.PageSize);

        PanelAdListRows result = await repository.ListAsync(query, cancellationToken);
        if (result.Rows.Count == 0 && result.Total > 0 && query.Page > 1)
        {
            // Página além do fim (uma página que sumiu depois de arquivar, ou um endereço digitado à mão): mostra a última
            int last = (result.Total + query.PageSize - 1) / query.PageSize;
            query = query with { Page = last };
            result = await repository.ListAsync(query, cancellationToken);
        }

        CategoryTreeSnapshot snapshot = await tree.GetAsync(cancellationToken);
        IReadOnlyList<PanelAdListItem> items =
        [
            .. result.Rows.Select(row => new PanelAdListItem(
                row.Id, row.Title, row.AuthorName, row.CategoryId is { } id ? snapshot.Find(id)?.Name : null, row.Status, row.ChangedAt,
                row.Status == AdStatus.Rejected ? row.RejectionReason : null))
        ];
        return new PanelAdListPage(items, result.Total, query.Page, query.PageSize, typed, status);
    }

    public Task<int> CountInReviewAsync(CancellationToken cancellationToken) => repository.CountByStatusAsync(AdStatus.InReview, cancellationToken);
}
