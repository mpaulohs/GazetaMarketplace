using System;
using GazetaMarketplace.Core.Formatting;

namespace GazetaMarketplace.Core.Fields;

/// <summary>
/// Ano do modelo de um veículo: de 1950 ("1950 ou anterior") até o <b>ano atual + 1</b>, porque os fabricantes lançam o modelo do ano seguinte
/// no fim do ano anterior. "Ano atual" é o do relógio do site no fuso de São Paulo (vira à meia-noite de 1º de janeiro de lá, não do UTC).
/// </summary>
public static class ModelYearRules
{
    public const int MinYear = 1950;

    public static int MaxYear(int currentYear) => currentYear + 1;

    /// <summary>O ano atual no fuso de São Paulo, pelo relógio do site.</summary>
    public static int CurrentYear(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        return TimeZoneInfo.ConvertTime(time.GetUtcNow(), CurrencyAndDateFormatter.DisplayTimeZone).Year;
    }

    public static bool IsValid(int year, TimeProvider time) => year >= MinYear && year <= MaxYear(CurrentYear(time));
}
