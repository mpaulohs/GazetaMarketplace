using GazetaMarketplace.Core.Categories;

namespace GazetaMarketplace.Core.Ads;

/// <summary>Os endereços públicos do anúncio, num lugar só (ARCHITECTURE, NFR-21): <c>/anuncio/{id}/{slug}</c>. A página de detalhe responde por esse endereço (tarefa 5.2).</summary>
public static class AdRoutes
{
    /// <summary>Tamanho máximo do pedaço legível do endereço; o título inteiro não precisa caber.</summary>
    public const int SlugMaxLength = 80;

    public static string Detail(int id, string title)
    {
        string slug = Slug(title);
        return $"/anuncio/{id}/{slug}";
    }

    private static string Slug(string title)
    {
        string slug = SlugGenerator.Slugify(title);
        if (slug.Length <= SlugMaxLength)
        {
            return slug;
        }

        // Corta no último hífen antes do limite para não deixar meia palavra
        string cut = slug[..SlugMaxLength];
        int hyphen = cut.LastIndexOf('-');
        return (hyphen > 0 ? cut[..hyphen] : cut).TrimEnd('-');
    }
}
