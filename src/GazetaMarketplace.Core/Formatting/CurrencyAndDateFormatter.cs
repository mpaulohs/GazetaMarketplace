using System;
using System.Globalization;

namespace GazetaMarketplace.Core.Formatting;

/// <summary>
/// Formatação de exibição em pt-BR (NFR-20): valores em R$ e datas dd/mm/aaaa no fuso de São Paulo.
/// Não depende da cultura da thread, para o resultado ser o mesmo em qualquer servidor.
/// </summary>
public static class CurrencyAndDateFormatter
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    // Datas são gravadas em UTC e exibidas neste fuso
    public static TimeZoneInfo DisplayTimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    /// <summary>"R$ 1.234" sem centavos; "R$ 1.234,50" quando os centavos são diferentes de zero (S29). O espaço é U+00A0.</summary>
    public static string FormatCurrency(long cents)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cents);

        long actual = cents / 100;
        long rest = cents % 100;
        string integer = actual.ToString("N0", PtBr);
        return rest == 0
            ? "R$ " + integer
            : "R$ " + integer + PtBr.NumberFormat.NumberDecimalSeparator + rest.ToString("00", PtBr);
    }

    /// <summary>"dd/MM/aaaa" no fuso de São Paulo. Data sem <see cref="DateTimeKind"/> é tratada como UTC.</summary>
    public static string FormatDate(DateTime utc) => ToSaoPaulo(utc).ToString("dd/MM/yyyy", PtBr);

    /// <summary>"dd/MM/aaaa HH:mm" no fuso de São Paulo.</summary>
    public static string FormatDateTime(DateTime utc) => ToSaoPaulo(utc).ToString("dd/MM/yyyy HH:mm", PtBr);

    private static DateTime ToSaoPaulo(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), DisplayTimeZone);
}
