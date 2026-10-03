namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>Smartwatches (categoria 46): Marca e Condição obrigatórias.</summary>
internal static class SmartwatchesGroup
{
    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.Smartwatches,
        Name = "Smartwatches",
        Fields =
        [
            new FieldDefinition { Key = "brandId", Label = "Marca", Type = FieldType.Select, Required = true, Options = FieldLists.SmartwatchBrand },
            new FieldDefinition { Key = "conditionId", Label = "Condição", Type = FieldType.Select, Required = true, Options = FieldLists.ProductCondition }
        ]
    };
}
