using System.Collections.Generic;

namespace VehicleCatalogExport;

// Linhas como a origem as entrega: o pai pode faltar (nulo) ou apontar para algo que não existe. O validador decide o que é órfão.
internal sealed record RawBrand(int Id, string Name);

internal sealed record RawModel(int Id, int? BrandId, string Name);

internal sealed record RawYear(int Id, int? ModelId, int? Year);

internal sealed record RawVersion(int Id, int? ModelId, int? Year, string Name);

/// <summary>O que a origem tem de um tipo de veículo (<c>car</c> ou <c>moto</c>), sem nenhuma conferência.</summary>
internal sealed record RawCatalog(
    string Kind,
    IReadOnlyList<RawBrand> Brands,
    IReadOnlyList<RawModel> Models,
    IReadOnlyList<RawYear> Years,
    IReadOnlyList<RawVersion> Versions);

// Linhas já conferidas, com o tipo em toda chave: é o formato das tabelas do site.
internal sealed record BrandRow(int Id, string Kind, string Name);

internal sealed record ModelRow(int Id, string Kind, int BrandId, string Name);

internal sealed record YearRow(int ModelId, int Year, string Kind);

internal sealed record VersionRow(int Id, string Kind, int ModelId, int Year, string Name);

/// <summary>O catálogo conferido (sem órfãos), pronto para virar script ou carga em lote.</summary>
internal sealed record CatalogData(
    IReadOnlyList<BrandRow> Brands,
    IReadOnlyList<ModelRow> Models,
    IReadOnlyList<YearRow> Years,
    IReadOnlyList<VersionRow> Versions);
