using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GazetaMarketplace.Core.Search;

/// <summary>
/// Número digitado num filtro (preço em reais, área em m²): <c>50000</c>, <c>50.000</c>, <c>50.000,00</c>, <c>50000,5</c> ou <c>50000.50</c>. Ponto de milhar só em
/// grupos de três dígitos; vírgula é sempre a separadora de decimais; ponto seguido de um ou dois dígitos também vale como decimal. Qualquer outra coisa (letra, sinal,
/// três casas decimais, vírgula dupla) é ilegível. Nunca lança.
/// </summary>
public static partial class DecimalInput
{
    /// <summary>Lê o número com no máximo duas casas decimais; <paramref name="value"/> nunca é negativo.</summary>
    public static bool TryParse(string text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string trimmed = text.Trim();
        if (trimmed.StartsWith("R$", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[2..].TrimStart();
        }

        string normalized;
        if (Plain().IsMatch(trimmed))
        {
            normalized = trimmed;
        }
        else if (Thousands().IsMatch(trimmed))
        {
            normalized = trimmed.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        }
        else if (CommaDecimals().IsMatch(trimmed))
        {
            normalized = trimmed.Replace(',', '.');
        }
        else if (DotDecimals().IsMatch(trimmed))
        {
            normalized = trimmed;
        }
        else
        {
            return false;
        }

        return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Reais (com até duas casas) em centavos; falso se ilegível ou acima de <paramref name="maxCents"/>.</summary>
    public static bool TryParseCents(string text, long maxCents, out long cents)
    {
        cents = 0;
        if (!TryParse(text, out decimal reais))
        {
            return false;
        }

        decimal total = reais * 100m;
        if (total > maxCents)
        {
            return false;
        }

        cents = (long)total;
        return true;
    }

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex Plain();

    [GeneratedRegex(@"^\d{1,3}(\.\d{3})+(,\d{1,2})?$")]
    private static partial Regex Thousands();

    [GeneratedRegex(@"^\d+,\d{1,2}$")]
    private static partial Regex CommaDecimals();

    [GeneratedRegex(@"^\d+\.\d{1,2}$")]
    private static partial Regex DotDecimals();
}
