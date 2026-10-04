using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Core.Search;

namespace CitiesImport;

/// <summary>Um município conferido, pronto para virar linha de <c>Cities</c>. <see cref="NameSearch"/> vem do <c>Normalizer</c> do site.</summary>
internal sealed record CityRow(int Code, string Name, string Uf, string NameSearch);

internal sealed record ValidationResult(IReadOnlyList<CityRow> Cities, IReadOnlyList<string> Problems)
{
    public bool IsValid => Problems.Count == 0 && Cities.Count > 0;
}

/// <summary>
/// Confere o arquivo do IBGE antes de gerar qualquer coisa. Qualquer problema recusa a carga inteira: um município torto numa lista oficial indica
/// arquivo errado, e uma lista pela metade enganaria o preenchimento manual.
/// </summary>
internal static class CityValidator
{
    public static ValidationResult Validate(IReadOnlyList<RawCity> raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        List<string> problems = [];
        List<CityRow> rows = [];

        if (raw.Count == 0)
        {
            problems.Add("O arquivo não tem nenhum município.");
        }

        for (int i = 0; i < raw.Count; i++)
        {
            RawCity city = raw[i];
            string where = string.Create(CultureInfo.InvariantCulture, $"item {i + 1}");
            string name = city.Name?.Trim();
            State state = BrazilianStates.Find(city.Uf);

            if (city.Code is not { } code || code is < 1_000_000 or > 9_999_999)
            {
                problems.Add($"{where}: código do IBGE ausente ou fora de 7 dígitos.");
                continue;
            }

            where = string.Create(CultureInfo.InvariantCulture, $"{where} (código {code})");
            if (string.IsNullOrWhiteSpace(name) || name.Length > 80)
            {
                problems.Add($"{where}: nome vazio ou com mais de 80 caracteres.");
                continue;
            }

            if (state is null)
            {
                problems.Add($"{where}: UF '{city.Uf}' não é uma das 27 UFs.");
                continue;
            }

            if (BrazilianStates.FromMunicipalityCode(code)?.Uf != state.Uf)
            {
                problems.Add($"{where}: os dois primeiros dígitos do código não são os da UF {state.Uf}.");
                continue;
            }

            rows.Add(new CityRow(code, name, state.Uf, Normalizer.Normalize(name)));
        }

        foreach (IGrouping<int, CityRow> group in rows.GroupBy(r => r.Code).Where(g => g.Count() > 1))
        {
            problems.Add(string.Create(CultureInfo.InvariantCulture, $"código {group.Key} repetido {group.Count()} vezes."));
        }

        foreach (IGrouping<(string Uf, string NameSearch), CityRow> group in rows.GroupBy(r => (r.Uf, r.NameSearch)).Where(g => g.Select(r => r.Code).Distinct().Count() > 1))
        {
            problems.Add($"{group.Key.Uf}: o nome '{group.First().Name}' aparece em mais de um código ({string.Join(", ", group.Select(r => r.Code))}).");
        }

        return new ValidationResult([.. rows.OrderBy(r => r.Code)], problems);
    }

    public static string Report(ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Problems.Count == 0
            ? string.Create(CultureInfo.InvariantCulture, $"Municípios válidos: {result.Cities.Count}.\n")
            : $"Carga recusada. {result.Problems.Count} problema(s):\n" + string.Join("\n", result.Problems.Select(p => "- " + p)) + "\n";
    }
}
