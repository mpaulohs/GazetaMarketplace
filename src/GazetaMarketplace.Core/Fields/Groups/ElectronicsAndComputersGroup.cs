namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>
/// Eletrônicos e informática (102 a 127, em cinco categorias-mãe). Condição e Marca obrigatórias; a Marca é texto livre de até 60 caracteres com
/// autocomplete (PL-01) e o Modelo, texto de até 60 caracteres (aprovado em 2026-10-03).
/// </summary>
internal static class ElectronicsAndComputersGroup
{
    public const int TextMaxLength = 60;

    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.ElectronicsAndComputers,
        Name = "Eletrônicos e informática",
        Fields =
        [
            new FieldDefinition { Key = "conditionId", Label = "Condição", Type = FieldType.Select, Required = true, RequiredMessage = "Informe a condição", Options = FieldLists.ProductCondition },
            new FieldDefinition { Key = "brand", Label = "Marca", Type = FieldType.Text, Required = true, RequiredMessage = "Informe a marca", MaxLength = TextMaxLength, Suggestions = SuggestionSource.Brands },
            new FieldDefinition { Key = "model", Label = "Modelo", Type = FieldType.Text, MaxLength = TextMaxLength }
        ]
    };
}
