namespace GazetaMarketplace.Web.Models;

/// <summary>
/// O que vai no <c>&lt;head&gt;</c> de uma página pública além do título (NFR-21): descrição, endereço canônico, a regra para os robôs e o mínimo do Open Graph (a prévia no WhatsApp).
/// A página que <b>não</b> traz um <see cref="SeoModel"/> sai com <c>noindex, follow</c>: só entra no índice de buscadores quem pede (início, categoria e anúncio publicado).
/// </summary>
public sealed class SeoModel
{
    public const string ViewDataKey = "Seo";

    /// <summary>Valor de <c>&lt;meta name="robots"&gt;</c> de quem não pede para ser indexado.</summary>
    public const string NoIndexFollow = "noindex, follow";

    public const string NoIndexOnly = "noindex";

    public string Description { get; init; }

    /// <summary>Endereço completo e definitivo da página.</summary>
    public string CanonicalUrl { get; init; }

    /// <summary>Nulo = a página é indexável e nada é escrito; senão o texto de <c>robots</c>.</summary>
    public string Robots { get; init; }

    public string OgTitle { get; init; }

    public string OgDescription { get; init; }

    /// <summary>Endereço completo da imagem (a capa do anúncio); nulo = sem <c>og:image</c>.</summary>
    public string OgImage { get; init; }

    public static SeoModel Indexable(string description, string canonicalUrl) => new() { Description = description, CanonicalUrl = canonicalUrl };

    public static SeoModel NoIndex(bool follow = true) => new() { Robots = follow ? NoIndexFollow : NoIndexOnly };
}
