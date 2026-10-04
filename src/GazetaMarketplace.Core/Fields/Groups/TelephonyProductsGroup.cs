using System.Collections.Generic;

namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>
/// Produtos de telefonia: Acessórios de Celular (44), Peças de Celular (45), Acessórios para Smartwatch (47) e Telefonia Fixa e Sem Fio (48).
/// O Tipo (obrigatório) tem uma lista por categoria; "Marcas compatíveis" só existe em 44 e 45.
/// </summary>
internal static class TelephonyProductsGroup
{
    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.TelephonyProducts,
        Name = "Produtos de telefonia",
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
                    [44] = FieldLists.PhoneAccessoryType,
                    [45] = FieldLists.PhonePartType,
                    [47] = FieldLists.SmartwatchAccessoryType,
                    [48] = FieldLists.LandlinePhoneType
                }
            },
            new FieldDefinition { Key = "conditionId", Label = "Condição", Type = FieldType.Select, Required = true, RequiredMessage = "Informe a condição", Options = FieldLists.ProductCondition },
            new FieldDefinition
            {
                Key = "compatibleBrandIds",
                Label = "Marcas compatíveis",
                Type = FieldType.MultiSelect,
                Options = FieldLists.PhoneBrand,
                AppliesToCategories = new HashSet<int> { 44, 45 }
            }
        ]
    };
}
