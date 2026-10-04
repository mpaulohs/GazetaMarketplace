using System.Collections.Generic;

namespace GazetaMarketplace.Core.Fields.Groups;

/// <summary>
/// Imóveis para venda ou aluguel: Apartamentos (26), Casas (27), Terrenos, sítios e fazendas (30) e Comércio e indústria (31).
/// Aluguel de quartos (28) e Temporada (29) têm grupos próprios (tarefa 2.4). Os campos mudam por categoria (<c>AppliesToCategories</c>
/// e <c>OptionsByCategory</c>), como no GazetaOnline.
/// </summary>
internal static class RealEstateGroup
{
    private const int Apartments = 26;
    private const int Houses = 27;
    private const int Land = 30;
    private const int Commercial = 31;

    public static FieldGroup Create() => new()
    {
        Key = FieldGroupKeys.RealEstate,
        Name = "Imóveis",
        Fields =
        [
            new FieldDefinition
            {
                Key = "propertyTypeId",
                Label = "Tipo",
                Type = FieldType.Select,
                Required = true, RequiredMessage = "Informe o tipo do imóvel",
                OptionsByCategory = new Dictionary<int, FieldList>
                {
                    [Apartments] = FieldLists.ApartmentType,
                    [Houses] = FieldLists.HouseType,
                    [Land] = FieldLists.LandType,
                    [Commercial] = FieldLists.CommercialType
                }
            },
            new FieldDefinition { Key = "transactionTypeId", Label = "Vender ou alugar", Type = FieldType.Select, Required = true, RequiredMessage = "Informe se o imóvel é para vender ou alugar", Options = FieldLists.PropertyTransactionType },
            new FieldDefinition
            {
                Key = "bedrooms",
                Label = "Quartos",
                Type = FieldType.Integer,
                Required = true, RequiredMessage = "Informe o número de quartos",
                Min = 0, // 0 = kitnet
                Max = FieldLimits.MaxCount,
                AppliesToCategories = new HashSet<int> { Apartments, Houses }
            },
            new FieldDefinition
            {
                Key = "bathrooms",
                Label = "Banheiros",
                Type = FieldType.Integer,
                Min = 0,
                Max = FieldLimits.MaxCount,
                AppliesToCategories = new HashSet<int> { Apartments, Houses }
            },
            new FieldDefinition
            {
                Key = "areaM2",
                Label = "Área (m²)",
                Type = FieldType.Decimal,
                DecimalPlaces = 2,
                Min = 0.01m,
                Max = FieldLimits.MaxAreaM2,
                Filter = FieldFilter.Range,
                RequiredForCategories = new HashSet<int> { Land }, RequiredMessage = "Informe a área" // ninguém compra terreno sem saber o tamanho; nas outras categorias segue opcional
            },
            new FieldDefinition
            {
                Key = "parkingSpaces",
                Label = "Vagas de garagem",
                Type = FieldType.Integer,
                Min = 0,
                Max = FieldLimits.MaxCount,
                AppliesToCategories = new HashSet<int> { Apartments, Houses, Commercial }
            },
            new FieldDefinition { Key = "condoFeeCents", Label = "Condomínio", Type = FieldType.Money, Min = 0, Max = FieldLimits.MaxMoneyCents },
            new FieldDefinition { Key = "propertyTaxCents", Label = "IPTU", Type = FieldType.Money, Min = 0, Max = FieldLimits.MaxMoneyCents },
            new FieldDefinition
            {
                Key = "propertyFeatureIds",
                Label = "Características",
                Type = FieldType.MultiSelect,
                OptionsByCategory = new Dictionary<int, FieldList>
                {
                    [Apartments] = FieldLists.ApartmentFeature,
                    [Houses] = FieldLists.ApartmentFeature, // Casas reaproveita a lista de Apartamentos (GazetaOnline)
                    [Land] = FieldLists.LandFeature,
                    [Commercial] = FieldLists.CommercialFeature
                }
            },
            new FieldDefinition
            {
                Key = "condoFeatureIds",
                Label = "Características do condomínio",
                Type = FieldType.MultiSelect,
                AppliesToCategories = new HashSet<int> { Apartments, Houses },
                OptionsByCategory = new Dictionary<int, FieldList>
                {
                    [Apartments] = FieldLists.ApartmentCondoFeature,
                    [Houses] = FieldLists.HouseCondoFeature
                }
            }
        ]
    };
}
