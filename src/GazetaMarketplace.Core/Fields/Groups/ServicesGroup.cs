namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>Serviços (categoria 66): sem preço, até 6 fotos, só o Tipo como ficha. SPEC, Apêndice B.</summary>
internal static class ServicesGroup
{
    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.Services,
        Name = "Serviços",
        HasPrice = false,
        PriceLabel = null,
        MaxPhotos = 6,
        MinPhotosToSubmit = 1,
        TitleMaxLength = 120,
        DescriptionMaxLength = 6000,
        DescriptionLabel = "Informações adicionais",
        Fields =
        [
            new FieldDefinition { Key = "serviceTypeId", Label = "Tipo", Type = FieldType.Select, Required = true, Options = FieldLists.ServiceType }
        ]
    };
}
