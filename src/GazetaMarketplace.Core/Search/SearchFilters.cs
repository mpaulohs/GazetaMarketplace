using System;
using System.Collections.Generic;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.VehicleCatalog;

namespace GazetaMarketplace.Core.Search;

/// <summary>Os filtros específicos do bem que a busca sabe aplicar (A7 c).</summary>
[Flags]
public enum SearchFilter
{
    None = 0,
    Brand = 1,
    Model = 2,
    Year = 4,
    Km = 8,
    Area = 16
}

/// <summary>
/// Que filtros específicos a categoria escolhida oferece (ADR-006, decisão de 2026-10-05): <b>quem decide é o grupo de campos</b> da categoria, pelos campos marcados com
/// <see cref="FieldDefinition.Filter"/>. A busca sabe aplicar cinco colunas (marca, modelo, ano, quilometragem e área); um teste confere que todo campo filtrável do
/// registro está aqui, para um filtro novo no grupo não ficar sem tela nem consulta.
/// </summary>
public static class SearchFilters
{
    /// <summary>Chave do campo no grupo → o filtro da busca que o atende.</summary>
    public static IReadOnlyDictionary<string, SearchFilter> ByFieldKey { get; } = new Dictionary<string, SearchFilter>(StringComparer.Ordinal)
    {
        ["brandId"] = SearchFilter.Brand,
        ["modelId"] = SearchFilter.Model,
        ["modelYear"] = SearchFilter.Year,
        ["km"] = SearchFilter.Km,
        ["areaM2"] = SearchFilter.Area
    };

    /// <summary>Os filtros específicos da categoria (o grupo resolvido pela árvore); <see cref="SearchFilter.None"/> quando o grupo não tem campo filtrável.</summary>
    public static SearchFilter For(FieldGroup group, int categoryId)
    {
        SearchFilter filters = SearchFilter.None;
        if (group is null)
        {
            return filters;
        }

        foreach (FieldDefinition field in group.FilterableFieldsFor(categoryId))
        {
            if (ByFieldKey.TryGetValue(field.Key, out SearchFilter filter))
            {
                filters |= filter;
            }
        }

        return filters;
    }

    /// <summary>O tipo de catálogo do grupo (marca e modelo vêm dele): <c>car</c> para Carros, <c>moto</c> para Motos, nulo nos demais.</summary>
    public static string KindOf(FieldGroup group) => group?.Key switch
    {
        FieldGroupKeys.Cars => VehicleKinds.Car,
        FieldGroupKeys.Motorcycles => VehicleKinds.Moto,
        _ => null
    };

    /// <summary>Os nomes dos filtros ligados, para a tela (<c>data-filters="brand model year"</c>).</summary>
    public static string Names(SearchFilter filters)
    {
        List<string> names = [];
        foreach (SearchFilter filter in new[] { SearchFilter.Brand, SearchFilter.Model, SearchFilter.Year, SearchFilter.Km, SearchFilter.Area })
        {
            if (filters.HasFlag(filter))
            {
                names.Add(filter.ToString().ToLowerInvariant());
            }
        }

        return string.Join(' ', names);
    }
}
