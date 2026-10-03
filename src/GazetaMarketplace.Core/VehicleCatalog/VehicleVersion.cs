namespace GazetaMarketplace.Core.VehicleCatalog;

/// <summary>Versão de um modelo num ano. Chave (<see cref="Id"/>, <see cref="Kind"/>); o ano é (<see cref="ModelId"/>, <see cref="Year"/>, <see cref="Kind"/>).</summary>
public class VehicleVersion
{
    public int Id { get; set; }

    public string Kind { get; set; }

    public int ModelId { get; set; }

    public int Year { get; set; }

    public string Name { get; set; }

    public string Source { get; set; }
}
