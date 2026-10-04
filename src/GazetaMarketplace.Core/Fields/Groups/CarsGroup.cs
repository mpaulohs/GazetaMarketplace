namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>Carros, vans e utilitários (categoria 33). Marca → Modelo → Ano → Versão vêm do catálogo (tarefa 2.5). SPEC, Apêndice B.</summary>
internal static class CarsGroup
{
    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.Cars,
        Name = "Carros, vans e utilitários",
        Fields =
        [
            .. CatalogFields.Chain(CatalogKind.Car),
            new FieldDefinition
            {
                Key = "km",
                Label = "Quilometragem",
                Type = FieldType.Integer,
                Required = true, RequiredMessage = "Informe a quilometragem",
                Min = 0,
                Max = FieldLimits.MaxKm,
                Filter = FieldFilter.Range
            },
            new FieldDefinition { Key = "transmissionId", Label = "Câmbio", Type = FieldType.Select, Options = FieldLists.CarTransmission },
            new FieldDefinition { Key = "doorsId", Label = "Portas", Type = FieldType.Select, Options = FieldLists.CarDoor },
            new FieldDefinition { Key = "fuelId", Label = "Combustível", Type = FieldType.Select, Options = FieldLists.CarFuel },
            new FieldDefinition { Key = "steeringId", Label = "Direção", Type = FieldType.Select, Options = FieldLists.CarSteeringGear },
            new FieldDefinition { Key = "vehicleTypeId", Label = "Tipo", Type = FieldType.Select, Options = FieldLists.VehicleType },
            new FieldDefinition { Key = "enginePowerId", Label = "Potência", Type = FieldType.Select, Options = FieldLists.CarEnginePower },
            new FieldDefinition { Key = "colorId", Label = "Cor", Type = FieldType.Select, Options = FieldLists.CarColor },
            new FieldDefinition { Key = "optionalItemIds", Label = "Opcionais", Type = FieldType.MultiSelect, Options = FieldLists.OptionalItem },
            new FieldDefinition { Key = "additionalInfoIds", Label = "Informações adicionais do veículo", Type = FieldType.MultiSelect, Options = FieldLists.AdditionalInfo }
        ]
    };
}
