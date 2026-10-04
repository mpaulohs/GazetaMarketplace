using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Formatting;

namespace GazetaMarketplace.Core.Fields;

/// <summary>
/// Lê o que a pessoa digitou ou escolheu em um campo do grupo e grava no <see cref="AdAttributes"/> <b>com o tipo certo</b> (ADR-002, US-008-S08):
/// número é número, nunca texto. Texto num campo numérico, número fora do formato brasileiro, opção que não está na lista e tipo errado são
/// recusados com a mensagem do campo; nada é "adivinhado". Valor vazio apaga o campo (rascunho aceita campo em branco).
/// </summary>
public static partial class FieldValueParser
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    [GeneratedRegex(@"^(\d{1,3}(\.\d{3})+|\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex IntegerShape();

    [GeneratedRegex(@"^(\d{1,3}(\.\d{3})+|\d+)(,\d+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex DecimalShape();

    public const string InvalidOption = "Escolha uma das opções da lista";

    public const string NotAWholeNumber = "Informe apenas números, sem letras nem vírgula";

    public const string NotANumber = "Informe um número válido, como 450,75";

    public const string NotMoney = "Informe um valor em reais, como 1.250,00";

    /// <summary>
    /// Aplica o valor bruto do campo ao <paramref name="target"/>. Devolve <c>null</c> quando deu certo (inclusive vazio, que apaga o campo) ou a
    /// mensagem de recusa. Os campos de catálogo só têm o inteiro conferido aqui; a cadeia marca → modelo → ano → versão é do serviço.
    /// </summary>
    public static string Apply(FieldDefinition field, int categoryId, IReadOnlyList<string> raw, AdAttributes target, int currentYear)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(target);

        string[] values = [.. (raw ?? []).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim())];
        if (values.Length == 0)
        {
            target.Remove(field.Key);
            return null;
        }

        if (field.Type != FieldType.MultiSelect && values.Length > 1)
        {
            return field.Type is FieldType.Text ? Invalid(field.Key, target) : InvalidOption;
        }

        return field.Type switch
        {
            FieldType.Text => ApplyText(field, values[0], target),
            FieldType.Integer => ApplyInteger(field, values[0], target),
            FieldType.Decimal => ApplyDecimal(field, values[0], target),
            FieldType.Money => ApplyMoney(field, values[0], target),
            FieldType.Select => ApplySelect(field, categoryId, values[0], target),
            FieldType.MultiSelect => ApplyMultiSelect(field, categoryId, values, target),
            FieldType.ModelYear => ApplyYear(field, values[0], ModelYearRules.MinYear, ModelYearRules.MaxYear(currentYear), target),
            FieldType.ManufactureYear => ApplyYear(field, values[0], ManufactureYearRules.MinYear, ManufactureYearRules.MaxYear(currentYear), target),
            FieldType.CatalogItem => ApplyCatalogId(field, values[0], target),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field.Type, null)
        };
    }

    private static string Invalid(string key, AdAttributes target)
    {
        target.Remove(key);
        return InvalidOption;
    }

    private static string ApplyText(FieldDefinition field, string text, AdAttributes target)
    {
        if (field.MaxLength is { } max && text.Length > max)
        {
            return $"Use no máximo {max} caracteres";
        }

        target.Set(field.Key, text);
        return null;
    }

    private static string ApplyInteger(FieldDefinition field, string text, AdAttributes target)
    {
        if (!IntegerShape().IsMatch(text)
            || !decimal.TryParse(text, NumberStyles.AllowThousands, PtBr, out decimal number)
            || number > int.MaxValue)
        {
            return NotAWholeNumber;
        }

        string range = OutOfRange(field, number);
        if (range is not null)
        {
            return range;
        }

        target.Set(field.Key, (int)number);
        return null;
    }

    private static string ApplyDecimal(FieldDefinition field, string text, AdAttributes target)
    {
        if (!DecimalShape().IsMatch(text)
            || !decimal.TryParse(text, NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, PtBr, out decimal number))
        {
            return NotANumber;
        }

        int places = text.Contains(',', StringComparison.Ordinal) ? text.Length - text.IndexOf(',', StringComparison.Ordinal) - 1 : 0;
        if (places > field.DecimalPlaces)
        {
            return field.DecimalPlaces == 0 ? NotAWholeNumber : $"Use no máximo {field.DecimalPlaces} casas depois da vírgula";
        }

        string range = OutOfRange(field, number);
        if (range is not null)
        {
            return range;
        }

        target.Set(field.Key, number);
        return null;
    }

    private static string ApplyMoney(FieldDefinition field, string text, AdAttributes target)
    {
        if (PriceText.TryParse(text, out long cents) != PriceParse.Ok)
        {
            return NotMoney;
        }

        if (cents > FieldLimits.MaxMoneyCents)
        {
            return $"Informe um valor de até {CurrencyAndDateFormatter.FormatCurrency(FieldLimits.MaxMoneyCents)}";
        }

        target.Set(field.Key, cents);
        return null;
    }

    private static string ApplySelect(FieldDefinition field, int categoryId, string text, AdAttributes target)
    {
        FieldList list = field.OptionsFor(categoryId);
        if (list is null || !TryId(text, out int id) || !list.Contains(id))
        {
            return InvalidOption;
        }

        target.Set(field.Key, id);
        return null;
    }

    private static string ApplyMultiSelect(FieldDefinition field, int categoryId, string[] values, AdAttributes target)
    {
        FieldList list = field.OptionsFor(categoryId);
        List<int> ids = [];
        foreach (string value in values)
        {
            if (list is null || !TryId(value, out int id) || !list.Contains(id))
            {
                return InvalidOption;
            }

            if (!ids.Contains(id))
            {
                ids.Add(id);
            }
        }

        target.Set(field.Key, ids);
        return null;
    }

    private static string ApplyYear(FieldDefinition field, string text, int min, int max, AdAttributes target)
    {
        if (!TryId(text, out int year) || year < min || year > max)
        {
            return $"Escolha um ano entre {min} e {max}";
        }

        target.Set(field.Key, year);
        return null;
    }

    private static string ApplyCatalogId(FieldDefinition field, string text, AdAttributes target)
    {
        if (!TryId(text, out int id))
        {
            return InvalidOption;
        }

        target.Set(field.Key, id);
        return null;
    }

    private static bool TryId(string text, out int id) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out id);

    private static string OutOfRange(FieldDefinition field, decimal number)
    {
        if (field.Min is { } min && number < min)
        {
            return RangeMessage(field);
        }

        return field.Max is { } max && number > max ? RangeMessage(field) : null;
    }

    private static string RangeMessage(FieldDefinition field)
    {
        string min = (field.Min ?? 0m).ToString("N" + field.DecimalPlaces.ToString(CultureInfo.InvariantCulture), PtBr);
        return field.Max is { } max
            ? $"Informe um valor entre {min} e {max.ToString("N" + field.DecimalPlaces.ToString(CultureInfo.InvariantCulture), PtBr)}"
            : $"Informe um valor de {min} em diante";
    }
}
