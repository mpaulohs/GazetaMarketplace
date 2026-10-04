using System;
using System.Collections.Generic;
using System.Linq;

namespace GazetaMarketplace.Core.Fields;

/// <summary>
/// Um grupo de campos (ADR-002, SPEC Apêndice B): os campos específicos <b>e</b> as regras do formulário que mudam de categoria para categoria
/// (preço, fotos, tamanho do título e da descrição). É a <b>única fonte</b> desses números: formulário, validação, detalhe e filtros leem daqui.
/// </summary>
public sealed class FieldGroup
{
    /// <summary>Fotos por anúncio quando o grupo não diz outra coisa (NFR-12).</summary>
    public const int DefaultMaxPhotos = 20;

    /// <summary>Título: o limite da coluna <c>Ads.Title</c>.</summary>
    public const int DefaultTitleMaxLength = 120;

    /// <summary>Descrição: valor do <c>/arch</c> (ARCHITECTURE §6.2); Serviços e Vagas de emprego usam 6000.</summary>
    public const int DefaultDescriptionMaxLength = 5000;

    public required string Key { get; init; }

    public required string Name { get; init; }

    /// <summary>Falso = o anúncio não tem preço (Serviços).</summary>
    public bool HasPrice { get; init; } = true;

    /// <summary>Como o preço é chamado na tela ("Preço"; em Vagas de emprego, "Salário"). Nulo quando não há preço.</summary>
    public string PriceLabel { get; init; } = "Preço";

    public int MaxPhotos { get; init; } = DefaultMaxPhotos;

    /// <summary>Fotos exigidas para enviar à revisão: 1, ou 0 quando o grupo não tem fotos (Vagas de emprego).</summary>
    public int MinPhotosToSubmit { get; init; } = 1;

    public int TitleMaxLength { get; init; } = DefaultTitleMaxLength;

    public int DescriptionMaxLength { get; init; } = DefaultDescriptionMaxLength;

    /// <summary>Nome do campo de descrição na tela ("Descrição" ou "Informações adicionais").</summary>
    public string DescriptionLabel { get; init; } = "Descrição";

    public string TitleHelp { get; init; }

    public string DescriptionPlaceholder { get; init; }

    public string DescriptionHelp { get; init; }

    public IReadOnlyList<FieldDefinition> Fields { get; init; } = [];

    /// <summary>Os campos que existem nesta categoria, na ordem do grupo.</summary>
    public IReadOnlyList<FieldDefinition> FieldsFor(int categoryId) => [.. Fields.Where(f => f.AppliesTo(categoryId))];

    /// <summary>Os campos obrigatórios para enviar à revisão nesta categoria.</summary>
    public IReadOnlyList<FieldDefinition> RequiredFieldsFor(int categoryId) => [.. FieldsFor(categoryId).Where(f => f.IsRequiredFor(categoryId))];

    /// <summary>Os campos que a busca oferece como filtro nesta categoria (A7 c).</summary>
    public IReadOnlyList<FieldDefinition> FilterableFieldsFor(int categoryId) => [.. FieldsFor(categoryId).Where(f => f.Filter != FieldFilter.None)];

    public FieldDefinition Field(string key) => Fields.SingleOrDefault(f => f.Key == key);
}
