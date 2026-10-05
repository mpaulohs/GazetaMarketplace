using System.Collections.Generic;
using GazetaMarketplace.Core.Ads;

namespace GazetaMarketplace.Web.Areas.Panel.Models;

/// <summary>Uma linha da lista com o endereço para onde o título leva (edição, leitura ou pré-visualização, conforme a situação e o papel).</summary>
public sealed record PanelAdListRowViewModel(PanelAdListItem Item, string Href);

/// <summary>A lista de anúncios do painel (US-012).</summary>
public sealed class PanelAdListViewModel
{
    public required bool IsAdministrator { get; init; }

    public required PanelAdListPage Page { get; init; }

    public IReadOnlyList<PanelAdListRowViewModel> Rows { get; init; } = [];

    /// <summary>Total da fila de revisão para a aba do Administrador; nulo para o Redator.</summary>
    public int? QueueCount { get; init; }

    /// <summary>Aviso de sucesso da ação anterior ("Anúncio arquivado").</summary>
    public string Message { get; init; }

    public string Heading => IsAdministrator ? "Anúncios" : "Meus anúncios";

    /// <summary>A situação escolhida em texto de endereço; vazio = todas (exceto arquivados).</summary>
    public string SituationSlug => Page.Status is { } status ? PanelAdListFilters.StatusSlug(status) : string.Empty;

    /// <summary>Os filtros que a paginação preserva nos links.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Query
    {
        get
        {
            List<KeyValuePair<string, string>> query = [];
            if (!string.IsNullOrEmpty(Page.Search))
            {
                query.Add(new("q", Page.Search));
            }

            if (Page.Status is not null)
            {
                query.Add(new("situacao", SituationSlug));
            }

            return query;
        }
    }
}
