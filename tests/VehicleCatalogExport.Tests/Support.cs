using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VehicleCatalogExport;

namespace VehicleCatalogExport.Tests;

internal static class Repo
{
    public static string Path(params string[] parts)
    {
        DirectoryInfo folder = new(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(System.IO.Path.Combine(folder.FullName, "GazetaMarketplace.slnx")))
        {
            folder = folder.Parent;
        }

        return folder is null
            ? throw new InvalidOperationException("GazetaMarketplace.slnx não encontrado acima de " + AppContext.BaseDirectory)
            : System.IO.Path.Combine([folder.FullName, .. parts]);
    }
}

/// <summary>Monta catálogos pequenos de um tipo, na mão, para os testes que não precisam de banco.</summary>
internal static class Raw
{
    public static RawCatalog Catalog(
        string kind,
        IEnumerable<RawBrand> brands = null,
        IEnumerable<RawModel> models = null,
        IEnumerable<RawYear> years = null,
        IEnumerable<RawVersion> versions = null) =>
        new(kind, [.. brands ?? []], [.. models ?? []], [.. years ?? []], [.. versions ?? []]);

    /// <summary>Honda → Civic → 2019 → "EX" completo, no tipo pedido.</summary>
    public static RawCatalog Complete(string kind, int id = 1) => Catalog(
        kind,
        [new RawBrand(id, "Honda")],
        [new RawModel(id, id, "Civic")],
        [new RawYear(id, id, 2019)],
        [new RawVersion(id, id, 2019, "EX")]);

    public static string[] Lines(string text) => [.. text.Split('\n').Select(l => l.TrimEnd('\r'))];
}
