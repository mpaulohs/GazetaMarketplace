using System.Linq;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields;

namespace GazetaMarketplace.Core.Showcase;

/// <summary>Traduz uma linha da consulta no card do anúncio (variante do grupo de campos, tipo do serviço, área da vaga e capa); a vitrine e a busca usam a mesma.</summary>
internal static class ShowcaseCards
{
    internal static AdCardModel Create(ShowcaseRow row, CategoryTreeSnapshot snapshot)
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
