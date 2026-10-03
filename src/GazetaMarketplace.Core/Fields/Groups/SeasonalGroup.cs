namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>
/// Temporada (categoria 29). Tipo, Quartos e "Acomoda quantas pessoas" são obrigatórios. Limites aprovados em 2026-10-03: Quartos, Banheiros e Vagas de
/// 0 a 20 (0 = estúdio) e Pessoas de 1 a 50 (a SPEC não define as faixas).
/// </summary>
internal static class SeasonalGroup
{
    public const int MaxGuests = 50;

    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.Seasonal,
        Name = "Temporada",
        Fields =
        [
            new FieldDefinition { Key = "seasonalTypeId", Label = "Tipo", Type = FieldType.Select, Required = true, Options = FieldLists.SeasonalType },
            Count("bedrooms", "Quartos", required: true),
            new FieldDefinition { Key = "guests", Label = "Acomoda quantas pessoas", Type = FieldType.Integer, Required = true, Min = 1, Max = MaxGuests },
            Count("bathrooms", "Banheiros", required: false),
            Count("parkingSpaces", "Vagas", required: false),
            new FieldDefinition { Key = "paymentTypeId", Label = "Forma de pagamento", Type = FieldType.Select, Options = FieldLists.SeasonalPaymentType },
            new FieldDefinition { Key = "seasonalFeatureIds", Label = "Características", Type = FieldType.MultiSelect, Options = FieldLists.SeasonalFeature }
        ]
    };

    private static FieldDefinition Count(string key, string label, bool required) =>
        new() { Key = key, Label = label, Type = FieldType.Integer, Required = required, Min = 0, Max = FieldLimits.MaxCount };
}
