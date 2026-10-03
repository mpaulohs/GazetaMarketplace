namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>Motos (categoria 36). Mesmo catálogo encadeado de Carros, com o catálogo de motos. SPEC, Apêndice B.</summary>
internal static class MotorcyclesGroup
{
    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.Motorcycles,
        Name = "Motos",
        Fields =
        [
            .. CatalogFields.Chain(CatalogKind.Motorcycle),
            new FieldDefinition
            {
                Key = "km",
                Label = "Quilometragem",
                Type = FieldType.Integer,
                Required = true,
                Min = 0,
                Max = FieldLimits.MaxKm,
                Filter = FieldFilter.Range
            },
            new FieldDefinition { Key = "displacementId", Label = "Cilindrada", Type = FieldType.Select, Required = true, Options = FieldLists.MotorcycleDisplacement },
            new FieldDefinition { Key = "colorId", Label = "Cor", Type = FieldType.Select, Options = FieldLists.CarColor },
            new FieldDefinition { Key = "optionalItemIds", Label = "Opcionais", Type = FieldType.MultiSelect, Options = FieldLists.MotorcycleOptionalItem },
            new FieldDefinition { Key = "additionalInfoIds", Label = "Informações adicionais do veículo", Type = FieldType.MultiSelect, Options = FieldLists.MotorcycleAdditionalInfo }
        ]
    };
}
