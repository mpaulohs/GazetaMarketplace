namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>
/// Produtos em geral: o grupo padrão das categorias sem grupo próprio (55 delas). Condição obrigatória e Tipo de produto opcional,
/// em texto livre de até 60 caracteres com autocomplete (decisão do Product Owner em 2026-10-03; a lista por categoria nunca foi definida).
/// Nenhum filtro específico na v1 (A7 c).
/// </summary>
internal static class GeneralProductsGroup
{
    public const int ProductTypeMaxLength = 60;

    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.GeneralProducts,
        Name = "Produtos em geral",
        Fields =
        [
            new FieldDefinition { Key = "conditionId", Label = "Condição", Type = FieldType.Select, Required = true, Options = FieldLists.ProductCondition },
            new FieldDefinition
            {
                Key = "productType",
                Label = "Tipo de produto",
                Type = FieldType.Text,
                MaxLength = ProductTypeMaxLength,
                Suggestions = SuggestionSource.ProductTypes
            }
        ]
    };
}
