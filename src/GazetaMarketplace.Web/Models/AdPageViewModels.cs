using System.Collections.Generic;
using GazetaMarketplace.Core.Categories;

namespace GazetaMarketplace.Web.Models;

/// <summary>A página pública do anúncio (US-003): o que a tela mostra e o que vai no <c>&lt;head&gt;</c> (descrição e endereço canônico).</summary>
public sealed class AdPageViewModel
{
    public required AdDetail Detail { get; init; }

    /// <summary>Endereço completo e definitivo da página, para <c>&lt;link rel="canonical"&gt;</c>.</summary>
    public required string CanonicalUrl { get; init; }

    public required string MetaDescription { get; init; }
}

/// <summary>A página "Este anúncio não está disponível" (US-003-S06): as categorias principais e, só quando o anúncio foi arquivado, a categoria dele.</summary>
public sealed class AdUnavailableViewModel
{
    public IReadOnlyList<CategoryNode> Roots { get; init; } = [];

    /// <summary>A categoria em que o anúncio arquivado estava; nulo nos demais casos (a resposta é a mesma de um endereço inexistente).</summary>
    public CategoryNode ArchivedCategory { get; init; }
}

/// <summary>As fotos e o título do anúncio para a galeria (a lista não pode ser vazia).</summary>
public sealed record AdGalleryViewModel(IReadOnlyList<GazetaMarketplace.Core.Photos.AdPhotoItem> Photos, string Title);
