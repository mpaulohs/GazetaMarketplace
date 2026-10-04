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
    MultiSelect = 6,

    /// <summary>Ano do modelo (<c>int</c>; o id é o próprio ano). A lista depende do ano atual: <see cref="FieldLists.ModelYears"/> e <see cref="ModelYearRules"/>.</summary>
    ModelYear = 7,

    /// <summary>Um nível da cadeia do catálogo de veículos (marca → modelo → ano → versão), descrito por <see cref="FieldDefinition.Catalog"/>. As consultas chegam na tarefa 2.5.</summary>
    CatalogItem = 8,

    /// <summary>Ano de fabricação (<c>int</c>; o id é o próprio ano), de 1950 até o ano atual: <see cref="FieldLists.ManufactureYears"/> e <see cref="ManufactureYearRules"/>.</summary>
    ManufactureYear = 9
}

/// <summary>Qual catálogo de veículos o campo consulta.</summary>
public enum CatalogKind
{
    Car = 1,
    Motorcycle = 2
}

/// <summary>Em que nível da cadeia marca → modelo → ano → versão o campo está.</summary>
public enum CatalogLevel
{
    Brand = 1,
    Model = 2,
    Year = 3,
    Version = 4
}

/// <summary>A que parte do catálogo um campo <see cref="FieldType.CatalogItem"/> se liga.</summary>
public sealed record CatalogReference(CatalogKind Kind, CatalogLevel Level);

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

    /// <summary>Quilometragem: de 0 a 9.999.999 km. Suposição aprovada em 2026-10-03; revisar se aparecer caso real fora da faixa.</summary>
    public const int MaxKm = 9_999_999;

    /// <summary>Horas de uso (barcos, aeronaves, máquinas): de 0 a 999.999. Suposição aprovada em 2026-10-03.</summary>
    public const int MaxHoursOfUse = 999_999;

    /// <summary>Comprimento, largura e altura em metros: de 0,01 a 999,99, com 2 casas. Suposição aprovada em 2026-10-03.</summary>
    public const decimal MaxMeters = 999.99m;

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

    /// <summary>Categorias em que o campo é obrigatório mesmo com <see cref="Required"/> falso (a Área só é obrigatória em Terrenos). Nulo = nenhuma.</summary>
    public IReadOnlySet<int> RequiredForCategories { get; init; }

    /// <summary>
    /// A frase da lista de pendências quando o campo obrigatório está vazio ("Informe a quilometragem"). Uma frase própria por campo, e não "Informe {rótulo}",
    /// porque o artigo muda com o gênero ("a quilometragem", "o modelo"). Todo campo obrigatório tem uma (um teste confere).
    /// </summary>
    public string RequiredMessage { get; init; }

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

    /// <summary>Só em campos <see cref="FieldType.CatalogItem"/>.</summary>
    public CatalogReference Catalog { get; init; }

    /// <summary>Se o campo é obrigatório para enviar à revisão nesta categoria (e só vale onde <see cref="AppliesTo"/> é verdadeiro).</summary>
    public bool IsRequiredFor(int categoryId) => Required || (RequiredForCategories is not null && RequiredForCategories.Contains(categoryId));

    public bool AppliesTo(int categoryId) => AppliesToCategories is null || AppliesToCategories.Contains(categoryId);

    /// <summary>A lista de opções do campo para esta categoria; nula se o campo não tem lista.</summary>
    public FieldList OptionsFor(int categoryId) =>
        OptionsByCategory is not null && OptionsByCategory.TryGetValue(categoryId, out FieldList own) ? own : Options;
}
