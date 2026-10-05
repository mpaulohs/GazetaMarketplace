using GazetaMarketplace.Core.Search;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Models;

/// <summary>
/// Os parâmetros do endereço da busca (US-002-S09), em português: <c>/busca?q=civic&amp;categoria=carros&amp;uf=SP&amp;cidade=Campinas&amp;precoMin=&amp;precoMax=&amp;marca=&amp;modelo=&amp;anoDe=&amp;anoAte=&amp;kmMax=&amp;areaMin=&amp;areaMax=&amp;ordem=menor-preco&amp;pagina=2</c>.
/// Tudo chega como texto: quem decide o que vale é <see cref="ISearch.PrepareAsync"/>.
/// </summary>
public sealed class SearchQueryModel
{
    [FromQuery(Name = "q")]
    public string Q { get; set; }

    [FromQuery(Name = "categoria")]
    public string Categoria { get; set; }

    [FromQuery(Name = "uf")]
    public string Uf { get; set; }

    [FromQuery(Name = "cidade")]
    public string Cidade { get; set; }

    [FromQuery(Name = "precoMin")]
    public string PrecoMin { get; set; }

    [FromQuery(Name = "precoMax")]
    public string PrecoMax { get; set; }

    [FromQuery(Name = "marca")]
    public string Marca { get; set; }

    [FromQuery(Name = "modelo")]
    public string Modelo { get; set; }

    [FromQuery(Name = "anoDe")]
    public string AnoDe { get; set; }

    [FromQuery(Name = "anoAte")]
    public string AnoAte { get; set; }

    [FromQuery(Name = "kmMax")]
    public string KmMax { get; set; }

    [FromQuery(Name = "areaMin")]
    public string AreaMin { get; set; }

    [FromQuery(Name = "areaMax")]
    public string AreaMax { get; set; }

    [FromQuery(Name = "ordem")]
    public string Ordem { get; set; }

    [FromQuery(Name = "pagina")]
    public string Pagina { get; set; }

    public SearchInput ToInput() => new(Q, Categoria, Uf, Cidade, PrecoMin, PrecoMax, Marca, Modelo, AnoDe, AnoAte, KmMax, AreaMin, AreaMax, Ordem, Pagina);
}

/// <summary>A página de busca: o formulário (sempre presente, com os filtros preservados) e o resultado ou a falha da consulta.</summary>
public sealed class SearchPageViewModel
{
    public required SearchForm Form { get; init; }

    /// <summary>Nulo quando a consulta falhou (<see cref="FailureCode"/> vem preenchido).</summary>
    public SearchResult Result { get; init; }

    /// <summary>O código de referência do log quando a consulta falhou (S11); nulo se deu certo.</summary>
    public string FailureCode { get; init; }

    /// <summary>O endereço da própria página (com os filtros), para "Tentar novamente".</summary>
    public required string SelfUrl { get; init; }
}
