namespace GazetaMarketplace.Core.Ads;

/// <summary>As situações do filtro como aparecem no endereço (<c>?situacao=em-revisao</c>) e os limites da lista do painel.</summary>
public static class PanelAdListFilters
{
    /// <summary>Anúncios por página no painel (SPEC, US-012): uma lista de trabalho mais densa que a do site público.</summary>
    public const int PageSize = 20;

    /// <summary>Tamanho máximo do termo de busca; o que passar disso é cortado.</summary>
    public const int SearchMaxLength = 100;

    public static string StatusSlug(byte status) => status switch
    {
        AdStatus.Draft => "rascunho",
        AdStatus.InReview => "em-revisao",
        AdStatus.Published => "publicado",
        AdStatus.Rejected => "rejeitado",
        AdStatus.Archived => "arquivado",
        _ => null
    };

    /// <summary>A situação de um texto de endereço; texto vazio ou desconhecido não filtra (nulo).</summary>
    public static byte? ParseStatus(string slug)
    {
        foreach (byte status in AdStatus.All)
        {
            if (string.Equals(StatusSlug(status), slug, System.StringComparison.Ordinal))
            {
                return status;
            }
        }

        return null;
    }
}
