using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace VehicleCatalogExport;

/// <summary>Uma linha da origem que não entrou no catálogo, com o motivo.</summary>
internal sealed record Orphan(string Kind, string Table, string Key, string Reason);

/// <summary>O catálogo conferido e a lista do que foi descartado.</summary>
internal sealed record ValidationResult(CatalogData Data, IReadOnlyList<Orphan> Orphans);

/// <summary>
/// Confere a hierarquia marca → modelo → ano → versão de cada tipo. Descarta o que não tem pai (e, em cascata, os filhos de quem foi descartado),
/// repetições de chave, nomes em branco, ids e anos que o site não aceita, nomes maiores que a coluna do site e acentos corrompidos na origem;
/// nada disso entra no catálogo, e tudo vai para o relatório para alguém decidir. Nada é truncado nem "consertado" em silêncio.
/// </summary>
internal static class CatalogValidator
{
    // Limites das colunas do site (VehicleCatalogConfiguration) e do intervalo de anos que a API aceita (VehicleCatalogController)
    public const int MaxBrandNameLength = 150;
    public const int MaxModelNameLength = 150;
    public const int MaxVersionNameLength = 250;
    public const int MinYear = 1950;
    public const int MaxYear = 2100;

    // "Ã" ou "Â" seguido do que sobra de um byte UTF-8 lido como Windows-1252/Latin-1 (ImÃ³veis, AutomÃ¡tico, Ã‰poca). "ÃO" ou "ÂNGULO" legítimos não casam:
    // depois do Ã ou do Â vem uma letra comum, não um símbolo da faixa 0x80-0xBF nem um dos símbolos que o Windows-1252 põe em 0x80-0x9F.
    private static readonly Regex Corrupted = new(
        "[\u00C2\u00C3][\u0080-\u00BF\u0152\u0153\u0160\u0161\u0178\u017D\u017E\u0192\u02C6\u02DC\u2013-\u203A\u20AC\u2122]",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

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
                string key = brand.Id.ToString(CultureInfo.InvariantCulture);
                if (brand.Id < 1)
                {
                    orphans.Add(new Orphan(kind, "marca", key, "id inválido"));
                }
                else if (NameProblem(brand.Name, MaxBrandNameLength) is { } nameProblem)
                {
                    orphans.Add(new Orphan(kind, "marca", key, nameProblem));
                }
                else if (!brandIds.Add(brand.Id))
                {
                    orphans.Add(new Orphan(kind, "marca", key, "id repetido"));
                }
                else
                {
                    brands.Add(new BrandRow(brand.Id, kind, brand.Name.Trim()));
                }
            }

            HashSet<int> modelIds = [];
            foreach (RawModel model in raw.Models.OrderBy(m => m.Id))
            {
                string key = model.Id.ToString(CultureInfo.InvariantCulture);
                if (model.Id < 1)
                {
                    orphans.Add(new Orphan(kind, "modelo", key, "id inválido"));
                }
                else if (NameProblem(model.Name, MaxModelNameLength) is { } nameProblem)
                {
                    orphans.Add(new Orphan(kind, "modelo", key, nameProblem));
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
                string key = $"{year.ModelId?.ToString(CultureInfo.InvariantCulture)}/{year.Year?.ToString(CultureInfo.InvariantCulture)}";
                if (year.ModelId is not { } modelId || !modelIds.Contains(modelId))
                {
                    orphans.Add(new Orphan(kind, "ano", key, "sem modelo"));
                }
                else if (year.Year is not { } value)
                {
                    // O ano da origem é texto: o que não vira número (por exemplo "Zero km") chega aqui como nulo
                    orphans.Add(new Orphan(kind, "ano", key, "ano inválido"));
                }
                else if (value < MinYear || value > MaxYear)
                {
                    orphans.Add(new Orphan(kind, "ano", key, $"ano fora de {MinYear} a {MaxYear}"));
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
                string key = version.Id.ToString(CultureInfo.InvariantCulture);
                if (version.Id < 1)
                {
                    orphans.Add(new Orphan(kind, "versão", key, "id inválido"));
                }
                else if (NameProblem(version.Name, MaxVersionNameLength) is { } nameProblem)
                {
                    orphans.Add(new Orphan(kind, "versão", key, nameProblem));
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

    // Por que o nome não pode entrar, ou nulo quando está bom. O comprimento vale para o nome já aparado, que é o que será gravado.
    private static string NameProblem(string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "sem nome";
        }

        string trimmed = name.Trim();
        if (trimmed.Length > maxLength)
        {
            return "nome longo demais";
        }

        return Corrupted.IsMatch(trimmed) ? "texto corrompido" : null;
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
