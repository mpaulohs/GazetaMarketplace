using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Fields.Groups;

namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// O título do bloco de características do anúncio (US-003-S04): "Características do veículo" nas categorias de veículo, "Características do terreno" em Terrenos, sítios e
/// fazendas e "Características" nas demais. É o grupo de campos (e, em Imóveis, a categoria) que decide, nunca o texto do rótulo.
/// </summary>
public static class AdSpecTitles
{
    public const string Vehicle = "Características do veículo";

    public const string Land = "Características do terreno";

    public const string General = "Características";

    public static string For(string groupKey, int? categoryId) => groupKey switch
    {
        FieldGroupKeys.Cars or FieldGroupKeys.Motorcycles or FieldGroupKeys.TrucksAndBuses or FieldGroupKeys.BoatsAndAircraft => Vehicle,
        FieldGroupKeys.RealEstate when categoryId == RealEstateGroup.Land => Land,
        _ => General
    };
}
