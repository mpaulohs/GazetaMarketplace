using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields;

namespace GazetaMarketplace.Core.Showcase;

/// <inheritdoc cref="IShowcase"/>
/// <remarks>
/// As categorias vêm da árvore em memória (<see cref="ICategoryTree"/>); os anúncios, do repositório de leitura, já só os publicados. O serviço traduz cada linha no
/// <see cref="AdCardModel"/> (variante do grupo de campos, tipo do serviço, área da vaga e capa) e decide o que é "categoria que não existe" (<c>Category</c> nulo)
/// e o que é "categoria sem anúncios" (<c>Total</c> zero).
/// </remarks>
public sealed class ShowcaseService(IShowcaseReadRepository repository, ICategoryTree tree) : IShowcase
{
    public async Task<ShowcaseHome> HomeAsync(CancellationToken cancellationToken)
    {
        CategoryTreeSnapshot snapshot = await tree.GetAsync(cancellationToken);
        IReadOnlyList<ShowcaseRow> rows = await repository.RecentAsync(ShowcaseFilters.RecentCount, cancellationToken);
        return new ShowcaseHome(snapshot.Roots, [.. rows.Select(row => Card(row, snapshot))]);
    }

    public async Task<ShowcaseCategoryPage> CategoryAsync(string slug, int page, CancellationToken cancellationToken)
    {
        CategoryTreeSnapshot snapshot = await tree.GetAsync(cancellationToken);
        // Qualquer texto vira uma busca no dicionário de slugs da árvore: o que não existe dá nulo (o tamanho do endereço já é limitado pelo servidor)
        CategoryNode category = snapshot.FindBySlug(slug);
        if (category is null)
        {
            return new ShowcaseCategoryPage(null, [], [], snapshot.Roots, [], 0, 1, ShowcaseFilters.PageSize);
        }

        // A categoria lista os anúncios dela e de todas as descendentes (US-001-S02)
        int[] ids = [category.Id, .. snapshot.DescendantsOf(category.Id).Select(c => c.Id)];
        int current = Math.Clamp(page, 1, ShowcaseFilters.MaxPage);
        ShowcaseRows result = await repository.ByCategoryAsync(ids, current, ShowcaseFilters.PageSize, cancellationToken);
        if (result.Rows.Count == 0 && result.Total > 0 && current > 1)
        {
            // Página além do fim (endereço digitado à mão): mostra a última
            current = (result.Total + ShowcaseFilters.PageSize - 1) / ShowcaseFilters.PageSize;
            result = await repository.ByCategoryAsync(ids, current, ShowcaseFilters.PageSize, cancellationToken);
        }

        return new ShowcaseCategoryPage(
            category,
            snapshot.PathTo(category.Id),
            snapshot.ChildrenOf(category.Id),
            snapshot.Roots,
            [.. result.Rows.Select(row => Card(row, snapshot))],
            result.Total,
            current,
            ShowcaseFilters.PageSize);
    }

    private static AdCardModel Card(ShowcaseRow row, CategoryTreeSnapshot snapshot)
    {
        FieldGroup group = (row.CategoryId is { } categoryId ? FieldGroupRegistry.Resolve(snapshot, categoryId) : null) ?? FieldGroupRegistry.Default;
        AdAttributes.TryParse(row.Attributes, out AdAttributes attributes);

        string serviceType = null;
        string jobArea = null;
        if (row.CategoryId is { } category && attributes is not null)
        {
            serviceType = group.Key == FieldGroupKeys.Services ? LabelOf(group, FieldKeys.ServiceType, category, attributes.TryGetInt(FieldKeys.ServiceType, out int type) ? type : null) : null;
            jobArea = group.Key == FieldGroupKeys.Jobs ? LabelOf(group, FieldKeys.JobAreas, category, attributes.GetInts(FieldKeys.JobAreas) is [int first, ..] ? first : null) : null;
        }

        AdCardCover cover = row.CoverPhotoId is { } photoId && row.CoverWidth is { } width && row.CoverHeight is { } height ? AdCardCover.FromStored(photoId, width, height) : null;
        return new AdCardModel(row.Id, row.Title, group.Key, row.PriceCents, serviceType, jobArea, cover, row.City, row.Uf, AdRoutes.Detail(row.Id, row.Title));
    }

    private static string LabelOf(FieldGroup group, string fieldKey, int categoryId, int? optionId) =>
        optionId is { } id ? group.Field(fieldKey)?.OptionsFor(categoryId)?.Find(id)?.Label : null;
}
