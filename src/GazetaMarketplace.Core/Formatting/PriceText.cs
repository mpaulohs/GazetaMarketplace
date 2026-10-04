using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GazetaMarketplace.Core.Formatting;

/// <summary>Resultado da leitura de um preço digitado.</summary>
public enum PriceParse
{
    /// <summary>Campo vazio: o anúncio fica sem preço.</summary>
    Empty = 0,

    Ok = 1,

    /// <summary>Texto que não é um valor em reais ("abc", "1,2,3", "12.5").</summary>
    Invalid = 2
}

/// <summary>
/// O único leitor de preço do site. O campo sempre envia a notação brasileira ("R$ 62.000,00" ou "62.000,00"); um número <b>sem vírgula</b> são
/// reais ("62000" e "62.000" valem R$ 62.000,00). O ponto só é aceito como separador de milhar (grupos de 3 dígitos); "12.5" é recusado em vez de
/// virar R$ 125,00. O resultado em centavos nunca passa por <c>double</c>.
/// </summary>
public static partial class PriceText
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    [GeneratedRegex(@"^(?<whole>\d{1,3}(\.\d{3})+|\d+)(,(?<cents>\d{1,2}))?$", RegexOptions.CultureInvariant)]
    private static partial Regex Shape();

    /// <summary>Lê o texto; <paramref name="cents"/> só vale quando o resultado é <see cref="PriceParse.Ok"/>. Valor acima do limite do <c>long</c> vira <c>long.MaxValue</c> (a faixa é conferida por quem chama).</summary>
    public static PriceParse TryParse(string text, out long cents)
    {
        cents = 0;
        string cleaned = Clean(text);
        if (cleaned.Length == 0)
        {
            return PriceParse.Empty;
        }

        Match match = Shape().Match(cleaned);
        if (!match.Success)
        {
            return PriceParse.Invalid;
        }

        string whole = match.Groups["whole"].Value.Replace(".", string.Empty, StringComparison.Ordinal);
        string fraction = match.Groups["cents"].Success ? match.Groups["cents"].Value.PadRight(2, '0') : "00";
        if (!long.TryParse(whole, NumberStyles.None, CultureInfo.InvariantCulture, out long reais) || reais > (long.MaxValue - 99) / 100)
        {
            cents = long.MaxValue;
            return PriceParse.Ok;
        }

        cents = reais * 100 + long.Parse(fraction, NumberStyles.None, CultureInfo.InvariantCulture);
        return PriceParse.Ok;
    }

    /// <summary>"62.000,00": como o preço volta para o campo ao reabrir o formulário (sempre com duas casas, sem o "R$").</summary>
    public static string Format(long cents)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cents);
        return (cents / 100m).ToString("N2", PtBr);
    }

    private static string Clean(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string result = text.Trim();
        if (result.StartsWith("R$", StringComparison.OrdinalIgnoreCase))
        {
            result = result[2..];
        }

        return result.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(' '.ToString(), string.Empty, StringComparison.Ordinal)
            .Replace(' '.ToString(), string.Empty, StringComparison.Ordinal);
    }
}
