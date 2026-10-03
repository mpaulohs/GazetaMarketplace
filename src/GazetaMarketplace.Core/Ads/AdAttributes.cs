using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// Os campos específicos do grupo da categoria (ADR-002), em um objeto JSON. A leitura é estrita no tipo: um número lido como texto (ou o inverso)
/// é "ausente", nunca um valor adivinhado. Os nomes das chaves são os do grupo (<c>brandId</c>, <c>km</c>, <c>areaM2</c>…).
/// </summary>
public sealed class AdAttributes
{
    public const string EmptyJson = "{}";

    private readonly JsonObject _values;

    public AdAttributes()
        : this(new JsonObject())
    {
    }

    private AdAttributes(JsonObject values) => _values = values;

    /// <summary>Lê o JSON gravado; devolve falso se não for um objeto JSON válido.</summary>
    public static bool TryParse(string json, out AdAttributes attributes)
    {
        attributes = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            if (JsonNode.Parse(json) is JsonObject values)
            {
                attributes = new AdAttributes(values);
                return true;
            }
        }
        catch (JsonException)
        {
        }

        return false;
    }

    public IEnumerable<string> Keys => _values.Select(p => p.Key);

    public bool Contains(string key) => _values.ContainsKey(key);

    public AdAttributes Set(string key, int value) => Put(key, JsonValue.Create(value));

    public AdAttributes Set(string key, decimal value) => Put(key, JsonValue.Create(value));

    public AdAttributes Set(string key, string value) => Put(key, value is null ? null : JsonValue.Create(value));

    public AdAttributes Set(string key, IEnumerable<int> values) =>
        Put(key, values is null ? null : new JsonArray([.. values.Select(v => (JsonNode)JsonValue.Create(v))]));

    public AdAttributes Remove(string key)
    {
        _values.Remove(key);
        return this;
    }

    public bool TryGetInt(string key, out int value)
    {
        value = 0;
        return _values[key] is JsonValue node && node.TryGetValue(out value);
    }

    public bool TryGetDecimal(string key, out decimal value)
    {
        value = 0;
        return _values[key] is JsonValue node && node.TryGetValue(out value);
    }

    /// <summary>O texto do campo, ou nulo se ausente ou se o valor não é texto.</summary>
    public string GetString(string key) => _values[key] is JsonValue node && node.TryGetValue(out string text) ? text : null;

    /// <summary>Os números de um campo de várias opções; vazio se ausente ou se algum item não é inteiro.</summary>
    public IReadOnlyList<int> GetInts(string key)
    {
        if (_values[key] is not JsonArray array)
        {
            return [];
        }

        List<int> result = [];
        foreach (JsonNode item in array)
        {
            if (item is JsonValue node && node.TryGetValue(out int number))
            {
                result.Add(number);
            }
            else
            {
                return [];
            }
        }

        return result;
    }

    /// <summary>O JSON como é gravado em <c>Ads.Attributes</c>.</summary>
    public string ToJson() => _values.ToJsonString();

    private AdAttributes Put(string key, JsonNode value)
    {
        if (value is null)
        {
            _values.Remove(key);
        }
        else
        {
            _values[key] = value;
        }

        return this;
    }
}
