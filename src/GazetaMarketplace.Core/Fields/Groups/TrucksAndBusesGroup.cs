using System.Collections.Generic;

namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>
/// Caminhões (34) e Ônibus (35). Mesmos campos; só mudam as listas de Tipo, Opcionais (por categoria). Ano do modelo e quilometragem
/// obrigatórios; filtros de ano e km (A7 c). SPEC, Apêndice B.
/// </summary>
internal static class TrucksAndBusesGroup
{
    private const int Trucks = 34;
    private const int Buses = 35;

    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.TrucksAndBuses,
        Name = "Caminhões e ônibus",
        Fields =
        [
            new FieldDefinition { Key = "modelYear", Label = "Ano do modelo", Type = FieldType.ModelYear, Required = true, Min = ModelYearRules.MinYear, Filter = FieldFilter.Range },
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
            new FieldDefinition { Key = "transmissionId", Label = "Câmbio", Type = FieldType.Select, Options = FieldLists.CarTransmission },
            new FieldDefinition { Key = "fuelId", Label = "Combustível", Type = FieldType.Select, Options = FieldLists.CarFuel },
            new FieldDefinition { Key = "steeringId", Label = "Direção", Type = FieldType.Select, Options = FieldLists.VehicleSteeringGear },
            new FieldDefinition
            {
                Key = "vehicleTypeId",
                Label = "Tipo",
                Type = FieldType.Select,
                OptionsByCategory = new Dictionary<int, FieldList> { [Trucks] = FieldLists.TruckType, [Buses] = FieldLists.BusType }
            },
            new FieldDefinition
            {
                Key = "optionalItemIds",
                Label = "Opcionais",
                Type = FieldType.MultiSelect,
                OptionsByCategory = new Dictionary<int, FieldList> { [Trucks] = FieldLists.TruckOptionalItem, [Buses] = FieldLists.BusOptionalItem }
            },
            new FieldDefinition { Key = "additionalInfoIds", Label = "Informações adicionais do veículo", Type = FieldType.MultiSelect, Options = FieldLists.VehicleAdditionalInfo }
        ]
    };
}
