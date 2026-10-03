namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>
/// Máquinas (89, 92, 93, 97). Só a Condição é obrigatória. Marca: texto livre de até 60 caracteres com autocomplete; Ano de fabricação: de 1950 até o
/// ano atual; Horas de uso: de 0 a 999.999 (limites aprovados em 2026-10-03).
/// </summary>
internal static class MachineryGroup
{
    public const int BrandMaxLength = 60;

    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.Machinery,
        Name = "Máquinas",
        Fields =
        [
            new FieldDefinition { Key = "conditionId", Label = "Condição", Type = FieldType.Select, Required = true, Options = FieldLists.ProductCondition },
            new FieldDefinition { Key = "brand", Label = "Marca", Type = FieldType.Text, MaxLength = BrandMaxLength, Suggestions = SuggestionSource.Brands },
            new FieldDefinition { Key = "manufactureYear", Label = "Ano de fabricação", Type = FieldType.ManufactureYear },
            new FieldDefinition { Key = "hoursOfUse", Label = "Horas de uso", Type = FieldType.Integer, Min = 0, Max = FieldLimits.MaxHoursOfUse }
        ]
    };
}
