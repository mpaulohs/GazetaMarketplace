namespace GazetaMarketplace.Core.VehicleCatalog;

/// <summary>Marca do catálogo (ADR-008). Chave (<see cref="Id"/>, <see cref="Kind"/>); o id vem da origem, nunca é gerado aqui.</summary>
public class VehicleBrand
{
    public int Id { get; set; }

    public string Kind { get; set; }

    public string Name { get; set; }

    /// <summary>De onde o registro veio (por exemplo <c>gazetaonline-2026-09</c>), para trocar de fonte sem mudar código (A5).</summary>
    public string Source { get; set; }
}
