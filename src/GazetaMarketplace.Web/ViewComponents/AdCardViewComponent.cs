using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.ViewComponents;

/// <summary>
/// O card do anúncio em três variantes (design-system §5.2): padrão (capa + preço), Serviços (capa + Tipo no lugar do preço) e Vagas de emprego
/// (bloco neutro no lugar da capa + Salário). Usado pela home, pela busca, pelas categorias e pela pré-visualização do painel.
/// </summary>
public sealed class AdCardViewComponent : ViewComponent
{
    /// <param name="ad">O anúncio, já lido do banco.</param>
    /// <param name="eagerImage">Verdadeiro na primeira linha de cards da página: a imagem carrega já; nas demais, só quando chega perto da tela.</param>
    /// <param name="favorite">O controle de favoritos do card: <c>heart</c> (coração, o padrão), <c>remove</c> ("Remover", na página Meus favoritos) ou <c>none</c> (a pré-visualização do painel).</param>
    public IViewComponentResult Invoke(AdCardModel ad, bool eagerImage = false, string favorite = AdCardFavorite.Heart)
    {
        FieldGroup group = FieldGroupRegistry.Get(ad.GroupKey) ?? FieldGroupRegistry.Default;
        AdValue value = AdPresentation.ValueOf(group, ad.PriceCents, ad.ServiceType);

        // Vagas não têm foto (A4); sem capa, ou com a imagem que não carrega, entra o bloco neutro "Foto indisponível"
        bool showsCover = AdPresentation.HasPhotos(group) && ad.Cover is not null;
        AdMediaPlaceholderViewModel placeholder = showsCover
            ? null
            : AdPresentation.HasPhotos(group)
                ? new AdMediaPlaceholderViewModel(AdPresentation.PhotoUnavailable)
                : new AdMediaPlaceholderViewModel(AdPresentation.JobBlockTitle, string.IsNullOrWhiteSpace(ad.JobArea) ? null : ad.JobArea.Trim());

        return View(new AdCardViewModel
        {
            Ad = ad,
            AccessibleName = AdPresentation.AccessibleName(ad.Title, value, ad.City, ad.Uf),
            Value = value,
            Location = AdPresentation.Location(ad.City, ad.Uf),
            CoverUrl = showsCover ? ad.Cover.Url ?? AdPresentation.CoverUrl(ad.Id, ad.Cover.PhotoId) : null,
            CoverWidth = showsCover ? ad.Cover.Width : 0,
            CoverHeight = showsCover ? ad.Cover.Height : 0,
            Placeholder = placeholder,
            EagerImage = eagerImage,
            Favorite = favorite
        });
    }
}
