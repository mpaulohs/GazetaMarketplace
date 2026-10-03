namespace GazetaMarketplace.Core.VehicleCatalog;

/// <summary>Modelo de uma marca. Chave (<see cref="Id"/>, <see cref="Kind"/>); a marca é (<see cref="BrandId"/>, <see cref="Kind"/>).</summary>
public class VehicleModel
{
    public int Id { get; set; }

    public string Kind { get; set; }

    public int BrandId { get; set; }

    public string Name { get; set; }

    public string Source { get; set; }
}
