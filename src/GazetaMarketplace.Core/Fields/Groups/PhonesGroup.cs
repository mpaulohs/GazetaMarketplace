namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>Celulares (categoria 43): Marca, Modelo e Condição obrigatórios. O Modelo é texto livre de até 60 caracteres (aprovado em 2026-10-03).</summary>
internal static class PhonesGroup
{
    public const int ModelMaxLength = 60;

    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.Phones,
        Name = "Celulares",
        Fields =
        [
            new FieldDefinition { Key = "brandId", Label = "Marca", Type = FieldType.Select, Required = true, Options = FieldLists.PhoneBrand },
            new FieldDefinition { Key = "model", Label = "Modelo", Type = FieldType.Text, Required = true, MaxLength = ModelMaxLength },
            new FieldDefinition { Key = "conditionId", Label = "Condição", Type = FieldType.Select, Required = true, Options = FieldLists.ProductCondition },
            new FieldDefinition { Key = "storageId", Label = "Armazenamento", Type = FieldType.Select, Options = FieldLists.PhoneStorage },
            new FieldDefinition { Key = "colorId", Label = "Cor", Type = FieldType.Select, Options = FieldLists.PhoneColor },
            new FieldDefinition { Key = "batteryHealthId", Label = "Saúde da bateria", Type = FieldType.Select, Options = FieldLists.PhoneBatteryHealth }
        ]
    };
}
