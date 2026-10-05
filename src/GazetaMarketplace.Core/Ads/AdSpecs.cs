using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GazetaMarketplace.Core.Fields;

namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// As características do anúncio como o visitante as lê ("Quilometragem: 45.000 km"): uma linha por campo do grupo da categoria, na ordem do grupo, só dos campos
/// preenchidos. Regra pura, sem banco: os nomes do catálogo de veículos (marca, modelo, versão) chegam prontos em <c>catalogLabels</c>, porque só o catálogo sabe
/// o nome de um id. Serve à pré-visualização do painel e, depois, à página pública do anúncio.
/// </summary>
public static class AdSpecs
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public static IReadOnlyList<AdSpec> Build(AdAttributes attributes, FieldGroup group, int categoryId, IReadOnlyDictionary<string, string> catalogLabels = null)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        ArgumentNullException.ThrowIfNull(group);

        List<AdSpec> specs = [];
        foreach (FieldDefinition field in group.FieldsFor(categoryId))
        {
            string value = ValueOf(attributes, field, categoryId, catalogLabels);
            if (!string.IsNullOrWhiteSpace(value))
            {
                specs.Add(new AdSpec(field.Label, value));
            }
        }

        return specs;
    }

    private static string ValueOf(AdAttributes attributes, FieldDefinition field, int categoryId, IReadOnlyDictionary<string, string> catalogLabels)
    {
        switch (field.Type)
        {
            case FieldType.Text:
                return attributes.GetString(field.Key)?.Trim();
            case FieldType.Integer:
                return attributes.TryGetInt(field.Key, out int number) ? number.ToString("N0", PtBr) + UnitOf(field) : null;
            case FieldType.Decimal:
                return attributes.TryGetDecimal(field.Key, out decimal amount) ? amount.ToString("#,##0.##", PtBr) + UnitOf(field) : null;
            case FieldType.Money:
                return attributes.TryGetLong(field.Key, out long cents) ? AdPresentation.FormatMoney(cents) : null;
            case FieldType.Select:
                return attributes.TryGetInt(field.Key, out int id) ? field.OptionsFor(categoryId)?.Find(id)?.Label : null;
            case FieldType.MultiSelect:
                {
                    FieldList list = field.OptionsFor(categoryId);
                    IEnumerable<string> labels = attributes.GetInts(field.Key).Select(i => list?.Find(i)?.Label).Where(l => l is not null);
                    return string.Join(", ", labels);
                }

            case FieldType.ModelYear:
                return attributes.TryGetInt(field.Key, out int modelYear) ? (modelYear <= ModelYearRules.MinYear ? "1950 ou anterior" : modelYear.ToString(CultureInfo.InvariantCulture)) : null;
            case FieldType.ManufactureYear:
                return attributes.TryGetInt(field.Key, out int year) ? year.ToString(CultureInfo.InvariantCulture) : null;
            case FieldType.CatalogItem:
                return catalogLabels is not null && catalogLabels.TryGetValue(field.Key, out string label) ? label : null;
            default:
                return null;
        }
    }

    // Só a quilometragem e as horas de uso levam a unidade no valor; área e medidas já a trazem no rótulo ("Área (m²)", "Comprimento (m)")
    private static string UnitOf(FieldDefinition field) => field.Key switch
    {
        "km" => " km",
        "hoursOfUse" => " h",
        _ => string.Empty
    };
}
