using System;
using GazetaMarketplace.Core.Ads;

namespace GazetaMarketplace.Core.Seo;

/// <summary>
/// Os textos fixos de título e descrição das páginas públicas (NFR-21), em português e sem editor na equipe (decisão do Product Owner, 2026-10-05). A página do anúncio usa o título
/// e a descrição do próprio anúncio (<see cref="AdMetaDescription"/>); aqui ficam o início e as categorias. Toda descrição tem no máximo <see cref="AdMetaDescription.MaxLength"/> caracteres.
/// </summary>
public static class SeoTexts
{
    public const string HomeTitle = "Classificados de veículos, imóveis, serviços e vagas";

    public const string HomeDescription = "Encontre anúncios de veículos, imóveis, serviços, vagas de emprego e produtos perto de você. Veja os mais recentes no GazetaMarketplace.";

    /// <summary>
    /// "Anúncios de Imóveis" para uma categoria principal e "Anúncios de Casas · Imóveis" para uma subcategoria (o pai desfaz o empate entre nomes iguais, como "Serviços" dentro de "Serviços");
    /// da segunda página em diante, "… — página 2" (cada página da lista tem o seu título).
    /// </summary>
    public static string CategoryTitle(string categoryName, string parentName, int page)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);
        string title = string.IsNullOrWhiteSpace(parentName) ? $"Anúncios de {categoryName}" : $"Anúncios de {categoryName} · {parentName}";
        return page > 1 ? $"{title} — página {page}" : title;
    }

    public static string CategoryDescription(string categoryName, string parentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);
        string where = string.IsNullOrWhiteSpace(parentName) ? categoryName : $"{categoryName} ({parentName})";
        return AdMetaDescription.Truncate($"Veja os anúncios de {where} no GazetaMarketplace: fotos, preços e o contato para fechar negócio.");
    }
}
