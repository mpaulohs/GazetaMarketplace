using System.Collections.Generic;

namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>
/// Peças (Autopeças e as filhas 38 a 42). O grupo fica gravado em Autopeças (id 3); as filhas herdam (A7 a), e categoria nova sob Autopeças
/// também. A lista de Tipo de peça muda por categoria. Sem filtros específicos (A7 c).
/// </summary>
internal static class PartsGroup
{
    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.Parts,
        Name = "Peças",
        Fields =
        [
            new FieldDefinition { Key = "conditionId", Label = "Condição", Type = FieldType.Select, Required = true, RequiredMessage = "Informe a condição", Options = FieldLists.PartCondition },
            new FieldDefinition
            {
                Key = "partTypeId",
                Label = "Tipo de peça",
                Type = FieldType.Select,
                OptionsByCategory = new Dictionary<int, FieldList>
                {
                    [38] = FieldLists.AutoPartType, // carros, vans e utilitários
                    [39] = FieldLists.AutoPartType, // caminhões
                    [40] = FieldLists.MotorcyclePartType,
                    [41] = FieldLists.BoatPartType,
                    [42] = FieldLists.AutoPartType // ônibus
                }
            },
            new FieldDefinition { Key = "colorId", Label = "Cor", Type = FieldType.Select, Options = FieldLists.PartColor }
        ]
    };
}
