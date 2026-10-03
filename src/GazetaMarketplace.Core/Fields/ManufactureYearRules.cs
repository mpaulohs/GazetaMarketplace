using System;

namespace GazetaMarketplace.Core.Fields;

/// <summary>
/// Ano de fabricação (Máquinas): de 1950 até o <b>ano atual</b>, sem passar dele. Não é o ano do modelo de veículos (<see cref="ModelYearRules"/>,
/// que aceita o ano seguinte): uma máquina já fabricada não é do futuro. "Ano atual" é o do relógio do site no fuso de São Paulo.
/// </summary>
public static class ManufactureYearRules
{
    public const int MinYear = 1950;

    public static int MaxYear(int currentYear) => currentYear;

    public static bool IsValid(int year, TimeProvider time) => year >= MinYear && year <= MaxYear(ModelYearRules.CurrentYear(time));
}
