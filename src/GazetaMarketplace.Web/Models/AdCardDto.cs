using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Showcase;

namespace GazetaMarketplace.Web.Models;

/// <summary>
/// O card de anúncio como a API <c>GET /api/v1/ads?ids=</c> o devolve (contrato <c>AdCard</c>). <c>PriceCents</c> é nulo em Serviços (A6) e <c>CoverUrl</c> é nulo em Vagas de emprego (A4) e em anúncio sem foto.
/// Nenhum campo é dado do autor.
/// </summary>
public sealed record AdCardDto(
    int Id,
    string Title,
    long? PriceCents,
    string PriceLabel,
    string ServiceType,
    string City,
    string Uf,
    string CategoryName,
    string FieldGroup,
    string CoverUrl,
    string Url)
{
    public static AdCardDto From(ShowcaseAdCard item)
    {
        AdCardModel card = item.Card;
        FieldGroup group = FieldGroupRegistry.Get(card.GroupKey) ?? FieldGroupRegistry.Default;
        AdValue value = AdPresentation.ValueOf(group, card.PriceCents, card.ServiceType);
        bool hasCover = AdPresentation.HasPhotos(group) && card.Cover is not null;

        return new AdCardDto(
            card.Id,
            card.Title,
            group.HasPrice ? card.PriceCents : null,
            value?.Kind is AdValueKind.Price or AdValueKind.Salary ? value.Label : null,
            value?.Kind == AdValueKind.ServiceType ? value.Text : null,
            card.City,
            card.Uf,
            item.CategoryName,
            card.GroupKey,
            hasCover ? card.Cover.Url ?? AdPresentation.CoverUrl(card.Id, card.Cover.PhotoId) : null,
            card.Href);
    }
}
