using System;

namespace GazetaMarketplace.Core.Formatting;

/// <summary>
/// Encontra o fuso de São Paulo em qualquer servidor (R-05). O id IANA <c>America/Sao_Paulo</c> existe no Linux e no Windows com ICU; o Windows sem ICU só conhece
/// o id próprio <c>E. South America Standard Time</c>. Se nenhum dos dois existir, cai num fuso fixo de UTC−03:00: o Brasil não tem horário de verão desde 2019,
/// então o deslocamento é o mesmo o ano todo para as datas que o site grava. Antes disto, o primeiro uso lançava <c>TimeZoneNotFoundException</c> dentro de um
/// inicializador estático e o tipo inteiro ficava inutilizável (500 em todas as telas com data) até o pool reciclar.
/// </summary>
public static class SaoPauloTimeZone
{
    public const string IanaId = "America/Sao_Paulo";

    public const string WindowsId = "E. South America Standard Time";

    /// <summary>Id do fuso de reserva, para o log e os testes reconhecerem o fuso fixo.</summary>
    public const string FixedOffsetId = "America/Sao_Paulo (UTC-03:00 fixo)";

    public static TimeZoneInfo Resolve() => Resolve(TimeZoneInfo.FindSystemTimeZoneById);

    /// <summary>Versão com o buscador injetado, para provar os três caminhos sem depender do servidor.</summary>
    public static TimeZoneInfo Resolve(Func<string, TimeZoneInfo> find)
    {
        ArgumentNullException.ThrowIfNull(find);

        foreach (string id in new[] { IanaId, WindowsId })
        {
            try
            {
                return find(id);
            }
            catch (Exception error) when (error is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                // tenta o próximo id
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(FixedOffsetId, TimeSpan.FromHours(-3), FixedOffsetId, FixedOffsetId);
    }
}
