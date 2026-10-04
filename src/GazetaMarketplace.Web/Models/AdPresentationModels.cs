using System.Collections.Generic;
using GazetaMarketplace.Core.Ads;

namespace GazetaMarketplace.Web.Models;

/// <summary>O que a partial <c>_AdValue</c> mostra; <see cref="Large"/> é o tamanho da página de detalhe e da pré-visualização.</summary>
public sealed record AdValueViewModel(AdValue Value, bool Large = false);

/// <summary>O bloco neutro no lugar da foto: "Vaga de emprego" (com a área) ou "Foto indisponível".</summary>
public sealed record AdMediaPlaceholderViewModel(string Title, string Detail = null);

/// <summary>O card pronto para desenhar: a variante já decidida e o nome acessível já montado.</summary>
public sealed class AdCardViewModel
{
    public required AdCardModel Ad { get; init; }

    public required string AccessibleName { get; init; }

    public AdValue Value { get; init; }

    public string Location { get; init; }

    /// <summary>Endereço da miniatura; nulo quando o card mostra o bloco neutro.</summary>
    public string CoverUrl { get; init; }

    public int CoverWidth { get; init; }

    public int CoverHeight { get; init; }

    /// <summary>"Vaga de emprego" ou "Foto indisponível"; só quando não há <see cref="CoverUrl"/>.</summary>
    public AdMediaPlaceholderViewModel Placeholder { get; init; }

    /// <summary>Verdadeiro na primeira linha de cards: a imagem carrega já (<c>loading="eager"</c>); nas demais, <c>lazy</c>.</summary>
    public bool EagerImage { get; init; }
}

/// <summary>Uma linha "rótulo: valor" das características do anúncio, já em texto.</summary>
public sealed record AdSpec(string Label, string Value);

/// <summary>O corpo textual do anúncio (título, valor, local, descrição e características). A galeria fica de fora: é da página de detalhe.</summary>
public sealed class AdBodyViewModel
{
    public required string Title { get; init; }

    public AdValue Value { get; init; }

    public string Location { get; init; }

    public string DescriptionLabel { get; init; } = "Descrição";

    public string Description { get; init; }

    public IReadOnlyList<AdSpec> Specs { get; init; } = [];

    /// <summary>Nível do título (1 na página do anúncio; mais baixo quando o corpo aparece dentro de outra página, como a pré-visualização).</summary>
    public int HeadingLevel { get; init; } = 1;
}
