using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace CitiesImport;

/// <summary>Um município como veio do arquivo do IBGE, antes de qualquer conferência.</summary>
internal sealed record RawCity(int? Code, string Name, string Uf);

/// <summary>
/// Lê o JSON oficial do IBGE (<c>servicodados.ibge.gov.br/api/v1/localidades/municipios</c>): uma lista de objetos com <c>id</c>, <c>nome</c> e a UF em
/// <c>microrregiao.mesorregiao.UF.sigla</c> (ou, nos municípios sem microrregião, em <c>regiao-imediata.regiao-intermediaria.UF.sigla</c>). Este é o único
/// lugar que conhece o formato do arquivo.
/// </summary>
internal static class IbgeReader
{
    /// <exception cref="InvalidOperationException">O arquivo não é um JSON do formato esperado (uma lista de objetos).</exception>
    public static IReadOnlyList<RawCity> Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("O arquivo do IBGE precisa ser uma lista JSON de municípios.");
            }

            List<RawCity> cities = [];
            foreach (JsonElement item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException("Cada município do arquivo do IBGE precisa ser um objeto JSON.");
                }

                cities.Add(new RawCity(Code(item), Text(item, "nome"), Uf(item)));
            }

            return cities;
        }
        catch (JsonException error)
        {
            throw new InvalidOperationException("O arquivo do IBGE não é um JSON válido: " + error.Message, error);
        }
    }

    private static int? Code(JsonElement item)
    {
        if (!item.TryGetProperty("id", out JsonElement id))
        {
            return null;
        }

        return id.ValueKind switch
        {
            JsonValueKind.Number when id.TryGetInt32(out int number) => number,
            JsonValueKind.String when int.TryParse(id.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) => parsed,
            _ => null
        };
    }

    private static string Uf(JsonElement item) =>
        Path(item, "microrregiao", "mesorregiao", "UF", "sigla") ?? Path(item, "regiao-imediata", "regiao-intermediaria", "UF", "sigla");

    private static string Path(JsonElement element, params string[] names)
    {
        foreach (string name in names)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out element))
            {
                return null;
            }
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    private static string Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
