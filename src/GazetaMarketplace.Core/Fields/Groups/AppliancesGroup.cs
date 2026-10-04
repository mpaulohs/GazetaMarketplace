using System.Collections.Generic;

namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>
/// Eletro (128 a 134). Tipo, Marca, Voltagem e Condição são obrigatórios; Capacidade só existe em Ar-condicionados (128). O Tipo tem uma lista por
/// categoria (dos tipos do GazetaOnline). A <b>Marca</b> é texto livre de até 60 caracteres com autocomplete (PL-01, decisão de 2026-10-03): as listas
/// de marca por tipo do GazetaOnline não são usadas.
/// </summary>
internal static class AppliancesGroup
{
    public const int BrandMaxLength = 60;

    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.Appliances,
        Name = "Eletro",
        Fields =
        [
            new FieldDefinition
            {
                Key = "typeId",
                Label = "Tipo",
                Type = FieldType.Select,
                Required = true, RequiredMessage = "Informe o tipo",
                OptionsByCategory = new Dictionary<int, FieldList>
                {
                    [128] = FieldLists.AirConditionerType,
                    [129] = FieldLists.FanType,
                    [130] = FieldLists.RefrigeratorType,
                    [131] = FieldLists.StoveType,
                    [132] = FieldLists.WasherType,
                    [133] = FieldLists.KitchenApplianceType,
                    [134] = FieldLists.PersonalCareApplianceType
                }
            },
            new FieldDefinition { Key = "brand", Label = "Marca", Type = FieldType.Text, Required = true, RequiredMessage = "Informe a marca", MaxLength = BrandMaxLength, Suggestions = SuggestionSource.Brands },
            new FieldDefinition { Key = "voltageId", Label = "Voltagem", Type = FieldType.Select, Required = true, RequiredMessage = "Informe a voltagem", Options = FieldLists.ApplianceVoltage },
            new FieldDefinition { Key = "conditionId", Label = "Condição", Type = FieldType.Select, Required = true, RequiredMessage = "Informe a condição", Options = FieldLists.ProductCondition },
            new FieldDefinition
            {
                Key = "capacityId",
                Label = "Capacidade",
                Type = FieldType.Select,
                Options = FieldLists.AirConditionerCapacity,
                AppliesToCategories = new HashSet<int> { 128 }
            }
        ]
    };
}
