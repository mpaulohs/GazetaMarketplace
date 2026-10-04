using System.Collections.Generic;

namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>
/// Roupas e calçados (64, 65, 68, 69, 72, 75, 76, 77). Condição e Tamanho obrigatórios; Gênero opcional. O Tamanho muda por categoria (PL-01):
/// roupas (64, 68, 72, 76) de PP a XGG, calçados adultos (65, 69) de 34 a 45 e calçados infantis e de bebê (75, 77) de 16 a 33.
/// </summary>
internal static class ClothingAndShoesGroup
{
    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.ClothingAndShoes,
        Name = "Roupas e calçados",
        Fields =
        [
            new FieldDefinition { Key = "conditionId", Label = "Condição", Type = FieldType.Select, Required = true, RequiredMessage = "Informe a condição", Options = FieldLists.ProductCondition },
            new FieldDefinition
            {
                Key = "sizeId",
                Label = "Tamanho",
                Type = FieldType.Select,
                Required = true, RequiredMessage = "Informe o tamanho",
                OptionsByCategory = new Dictionary<int, FieldList>
                {
                    [64] = FieldLists.ClothingSize, // roupas esportivas
                    [65] = FieldLists.AdultShoeSize, // calçados esportivos
                    [68] = FieldLists.ClothingSize,
                    [69] = FieldLists.AdultShoeSize,
                    [72] = FieldLists.ClothingSize, // roupas infantis
                    [75] = FieldLists.ChildShoeSize, // calçados infantis
                    [76] = FieldLists.ClothingSize, // roupas para bebês
                    [77] = FieldLists.ChildShoeSize // calçados para bebês
                }
            },
            new FieldDefinition { Key = "genderId", Label = "Gênero", Type = FieldType.Select, Options = FieldLists.Gender }
        ]
    };
}
