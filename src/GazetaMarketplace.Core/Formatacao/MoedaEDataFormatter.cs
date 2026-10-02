using System;
using System.Globalization;

namespace GazetaMarketplace.Core.Formatacao;

/// <summary>
/// Formatação de exibição em pt-BR (NFR-20): valores em R$ e datas dd/mm/aaaa no fuso de São Paulo.
/// Não depende da cultura da thread, para o resultado ser o mesmo em qualquer servidor.
/// </summary>
public static class MoedaEDataFormatter
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    // Datas são gravadas em UTC e exibidas neste fuso
    public static TimeZoneInfo FusoDeExibicao { get; } = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    /// <summary>"R$ 1.234" sem centavos; "R$ 1.234,50" quando os centavos são diferentes de zero (S29). O espaço é U+00A0.</summary>
    public static string FormatarMoeda(long centavos)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(centavos);

        long reais = centavos / 100;
        long resto = centavos % 100;
        string inteiro = reais.ToString("N0", PtBr);
        return resto == 0
            ? "R$ " + inteiro
            : "R$ " + inteiro + PtBr.NumberFormat.NumberDecimalSeparator + resto.ToString("00", PtBr);
    }

    /// <summary>"dd/MM/aaaa" no fuso de São Paulo. Data sem <see cref="DateTimeKind"/> é tratada como UTC.</summary>
    public static string FormatarData(DateTime utc) => ParaSaoPaulo(utc).ToString("dd/MM/yyyy", PtBr);

    /// <summary>"dd/MM/aaaa HH:mm" no fuso de São Paulo.</summary>
    public static string FormatarDataEHora(DateTime utc) => ParaSaoPaulo(utc).ToString("dd/MM/yyyy HH:mm", PtBr);

    private static DateTime ParaSaoPaulo(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), FusoDeExibicao);
}
