namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>Vagas de emprego (categoria 96): sem fotos, título de até 90, o preço aparece como "Salário". SPEC, Apêndice B.</summary>
internal static class JobsGroup
{
    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.Jobs,
        Name = "Vagas de emprego",
        HasPrice = true,
        PriceLabel = "Salário",
        MaxPhotos = 0,
        MinPhotosToSubmit = 0,
        TitleMaxLength = 90,
        DescriptionMaxLength = 6000,
        DescriptionLabel = "Informações adicionais",
        TitleHelp = "Sugerimos especificar a vaga com clareza. Ex.: Pizzaiolo com experiência, período integral",
        DescriptionPlaceholder = "Escreva aqui por que você está anunciando esta vaga e informações que podem ajudar no processo seletivo.",
        DescriptionHelp = "Inclua detalhes sobre o cargo, responsabilidades, remuneração, localização, ambiente de trabalho, etc",
        Fields =
        [
            new FieldDefinition { Key = FieldKeys.JobAreas, Label = "Área", Type = FieldType.MultiSelect, Required = false, Options = FieldLists.JobArea }
        ]
    };
}
