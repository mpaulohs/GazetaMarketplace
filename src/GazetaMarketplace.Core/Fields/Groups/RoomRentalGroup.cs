namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>Aluguel de quartos (categoria 28): só as Características (várias). Sem campo obrigatório específico e sem filtro (A7 c).</summary>
internal static class RoomRentalGroup
{
    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.RoomRental,
        Name = "Aluguel de quartos",
        Fields =
        [
            new FieldDefinition { Key = "roomFeatureIds", Label = "Características", Type = FieldType.MultiSelect, Options = FieldLists.RoomFeature }
        ]
    };
}
