namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>
/// Barcos e aeronaves (categoria 37). Embarcação não mede quilometragem: o campo é <b>Horas de uso</b>, obrigatório (achado do levantamento,
/// SPEC Apêndice B). Sem filtros específicos na v1 (A7 c). Comprimento, largura e altura em metros, 2 casas.
/// </summary>
internal static class BoatsAndAircraftGroup
{
    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.BoatsAndAircraft,
        Name = "Barcos e aeronaves",
        Fields =
        [
            new FieldDefinition { Key = "modelYear", Label = "Ano do modelo", Type = FieldType.ModelYear, Required = true, Min = ModelYearRules.MinYear },
            new FieldDefinition
            {
                Key = "hoursOfUse",
                Label = "Horas de uso",
                Type = FieldType.Integer,
                Required = true,
                Min = 0,
                Max = FieldLimits.MaxHoursOfUse
            },
            new FieldDefinition { Key = "vehicleTypeId", Label = "Tipo", Type = FieldType.Select, Required = true, Options = FieldLists.BoatType },
            new FieldDefinition { Key = "fuelId", Label = "Combustível", Type = FieldType.Select, Options = FieldLists.CarFuel },
            Meters("lengthMeters", "Comprimento (m)"),
            Meters("widthMeters", "Largura (m)"),
            Meters("heightMeters", "Altura (m)"),
            new FieldDefinition { Key = "additionalInfoIds", Label = "Informações adicionais", Type = FieldType.MultiSelect, Options = FieldLists.BoatAdditionalInfo }
        ]
    };

    private static FieldDefinition Meters(string key, string label) => new()
    {
        Key = key,
        Label = label,
        Type = FieldType.Decimal,
        DecimalPlaces = 2,
        Min = 0.01m,
        Max = FieldLimits.MaxMeters
    };
}
