using System;
using System.Collections.Generic;
using System.Linq;

namespace VehicleCatalogExport;

/// <summary>Uma linha da origem que não entrou no catálogo, com o motivo.</summary>
internal sealed record Orphan(string Kind, string Table, string Key, string Reason);

/// <summary>O catálogo conferido e a lista do que foi descartado.</summary>
internal sealed record ValidationResult(CatalogData Data, IReadOnlyList<Orphan> Orphans);

/// <summary>
/// Confere a hierarquia marca → modelo → ano → versão de cada tipo. Descarta o que não tem pai (e, em cascata, os filhos de quem foi descartado),
/// repetições de chave e nomes em branco; nada disso entra no catálogo, e tudo vai para o relatório para alguém decidir.
/// </summary>
internal static class CatalogValidator
{
    public static ValidationResult Validate(IEnumerable<RawCatalog> catalogs)
    {
        List<BrandRow> brands = [];
        List<ModelRow> models = [];
        List<YearRow> years = [];
        List<VersionRow> versions = [];
        List<Orphan> orphans = [];

        foreach (RawCatalog raw in catalogs)
        {
            string kind = raw.Kind;
            HashSet<int> brandIds = [];
            foreach (RawBrand brand in raw.Brands.OrderBy(b => b.Id))
            {
                if (string.IsNullOrWhiteSpace(brand.Name))
                {
                    orphans.Add(new Orphan(kind, "marca", brand.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), "sem nome"));
                }
                else if (!brandIds.Add(brand.Id))
                {
                    orphans.Add(new Orphan(kind, "marca", brand.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), "id repetido"));
                }
                else
                {
                    brands.Add(new BrandRow(brand.Id, kind, brand.Name.Trim()));
                }
            }

            HashSet<int> modelIds = [];
            foreach (RawModel model in raw.Models.OrderBy(m => m.Id))
            {
                string key = model.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    orphans.Add(new Orphan(kind, "modelo", key, "sem nome"));
                }
                else if (model.BrandId is not { } brandId || !brandIds.Contains(brandId))
                {
                    orphans.Add(new Orphan(kind, "modelo", key, "sem marca"));
                }
                else if (!modelIds.Add(model.Id))
                {
                    orphans.Add(new Orphan(kind, "modelo", key, "id repetido"));
                }
                else
                {
                    models.Add(new ModelRow(model.Id, kind, brandId, model.Name.Trim()));
                }
            }

            HashSet<(int, int)> yearKeys = [];
            foreach (RawYear year in raw.Years.OrderBy(y => y.Id))
            {
                string key = $"{year.ModelId?.ToString(System.Globalization.CultureInfo.InvariantCulture)}/{year.Year?.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                if (year.ModelId is not { } modelId || year.Year is not { } value || !modelIds.Contains(modelId))
                {
                    orphans.Add(new Orphan(kind, "ano", key, "sem modelo"));
                }
                else if (yearKeys.Add((modelId, value)))
                {
                    years.Add(new YearRow(modelId, value, kind));
                }
                else
                {
                    orphans.Add(new Orphan(kind, "ano", key, "ano repetido no modelo"));
                }
            }

            HashSet<int> versionIds = [];
            foreach (RawVersion version in raw.Versions.OrderBy(v => v.Id))
            {
                string key = version.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(version.Name))
                {
                    orphans.Add(new Orphan(kind, "versão", key, "sem nome"));
                }
                else if (version.ModelId is not { } modelId || version.Year is not { } value || !yearKeys.Contains((modelId, value)))
                {
                    orphans.Add(new Orphan(kind, "versão", key, "sem ano"));
                }
                else if (!versionIds.Add(version.Id))
                {
                    orphans.Add(new Orphan(kind, "versão", key, "id repetido"));
                }
                else
                {
                    versions.Add(new VersionRow(version.Id, kind, modelId, value, version.Name.Trim()));
                }
            }
        }

        return new ValidationResult(new CatalogData(brands, models, years, versions), orphans);
    }

    public static string Report(ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Orphans.Count == 0)
        {
            return "Nenhum registro descartado.\n";
        }

        IEnumerable<string> lines = result.Orphans.Select(o => $"{o.Kind}\t{o.Table}\t{o.Key}\t{o.Reason}");
        return $"Registros descartados: {result.Orphans.Count}\ntipo\ttabela\tchave\tmotivo\n{string.Join('\n', lines)}\n";
    }
}
