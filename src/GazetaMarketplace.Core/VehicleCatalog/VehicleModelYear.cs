namespace GazetaMarketplace.Core.VehicleCatalog;

/// <summary>
/// Ano em que um modelo existe. Tabela própria, e não um <c>DISTINCT</c> das versões, porque há anos sem versões cadastradas.
/// Chave (<see cref="ModelId"/>, <see cref="Year"/>, <see cref="Kind"/>).
/// </summary>
public class VehicleModelYear
{
    public int ModelId { get; set; }

    public int Year { get; set; }

    public string Kind { get; set; }

    public string Source { get; set; }
}
