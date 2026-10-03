using System;
using System.Collections.Generic;

namespace GazetaMarketplace.Core.Fields;

/// <summary>Como o campo é preenchido e guardado no JSON do anúncio.</summary>
public enum FieldType
{
    /// <summary>Texto curto (<see cref="FieldDefinition.MaxLength"/>).</summary>
    Text = 1,

    /// <summary>Número inteiro entre <see cref="FieldDefinition.Min"/> e <see cref="FieldDefinition.Max"/>.</summary>
    Integer = 2,

    /// <summary>Número com <see cref="FieldDefinition.DecimalPlaces"/> casas, guardado como <c>decimal</c> (área em m²).</summary>
    Decimal = 3,

    /// <summary>Dinheiro em <b>centavos</b> (<c>long</c>), com o mesmo teto do preço (<see cref="FieldLimits.MaxMoneyCents"/>).</summary>
    Money = 4,

    /// <summary>Uma opção de uma lista; guarda o id.</summary>
    Select = 5,

    /// <summary>Várias opções de uma lista; guarda os ids.</summary>
    MultiSelect = 6
}

/// <summary>Como a busca usa o campo, quando usa (A7 c).</summary>
public enum FieldFilter
{
    None = 0,
    Exact = 1,

    /// <summary>Faixa mínimo-máximo; aceita decimais.</summary>
    Range = 2
}

/// <summary>De onde vêm as sugestões de um campo de texto com autocomplete (valores já usados em anúncios).</summary>
public enum SuggestionSource
{
    None = 0,

    /// <summary><c>GET /api/v1/brands/suggest</c>.</summary>
    Brands = 1,

    /// <summary><c>GET /api/v1/product-types/suggest</c>.</summary>
    ProductTypes = 2
}

/// <summary>Limites compartilhados por todos os campos.</summary>
public static class FieldLimits
{
    /// <summary>R$ 99.999.999,99 em centavos: o teto do preço (S28) vale também para Condomínio e IPTU.</summary>
    public const long MaxMoneyCents = 9_999_999_999L;

    /// <summary>Quartos, Banheiros e Vagas: de 0 a 20. Suposição aprovada em 2026-10-03; revisar se aparecer caso real de mais de 20.</summary>
    public const int MaxCount = 20;

    /// <summary>Área em m² (<c>decimal(12,2)</c> no banco): teto prático de 99.999.999,99 m², bem acima de qualquer fazenda da região.</summary>
    public const decimal MaxAreaM2 = 99_999_999.99m;
}

/// <summary>
/// Um campo específico de um grupo (ADR-002). A <see cref="Key"/> é o nome estável no JSON (<c>conditionId</c>, <c>propertyTypeId</c>…):
/// mudar a chave de um campo em uso exige migração dos anúncios. Obrigatório = obrigatório para <i>enviar à revisão</i>, e só vale nas
/// categorias a que o campo se aplica.
/// </summary>
public sealed class FieldDefinition
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public required FieldType Type { get; init; }

    public bool Required { get; init; }

    /// <summary>Lista de opções comum a todas as categorias do grupo (Select e MultiSelect).</summary>
    public FieldList Options { get; init; }

    /// <summary>Lista própria de algumas categorias (por exemplo, o Tipo de imóvel muda em Apartamentos e Terrenos). Vence <see cref="Options"/>.</summary>
    public IReadOnlyDictionary<int, FieldList> OptionsByCategory { get; init; }

    /// <summary>Categorias em que o campo existe; nulo = todas as do grupo.</summary>
    public IReadOnlySet<int> AppliesToCategories { get; init; }

    public int? MaxLength { get; init; }

    public decimal? Min { get; init; }

    public decimal? Max { get; init; }

    public int DecimalPlaces { get; init; }

    public FieldFilter Filter { get; init; }

    public SuggestionSource Suggestions { get; init; }

    public string HelpText { get; init; }

    public bool AppliesTo(int categoryId) => AppliesToCategories is null || AppliesToCategories.Contains(categoryId);

    /// <summary>A lista de opções do campo para esta categoria; nula se o campo não tem lista.</summary>
    public FieldList OptionsFor(int categoryId) =>
        OptionsByCategory is not null && OptionsByCategory.TryGetValue(categoryId, out FieldList own) ? own : Options;
}
