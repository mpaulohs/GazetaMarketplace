using System;
using System.Collections.Generic;
using System.Text;

namespace GazetaMarketplace.Core.Settings;

/// <summary>Resultado de ler um telefone digitado.</summary>
public enum PhoneParseResult
{
    Valid = 0,

    /// <summary>Em branco (US-015-S05).</summary>
    Empty = 1,

    /// <summary>Não é um telefone brasileiro com DDD (US-015-S04).</summary>
    Invalid = 2
}

/// <summary>
/// Telefone brasileiro com DDD (US-015). Aceita o número com ou sem formatação e com ou sem <c>+55</c>/<c>55</c>, e guarda <b>só os dígitos, sem o
/// código do país</b>. Rigor total: DDD na lista oficial da Anatel; com 11 dígitos, o primeiro depois do DDD é 9 (celular, o que o WhatsApp
/// exige); com 10 dígitos (fixo ou número legado), o primeiro depois do DDD é de 2 a 9.
/// </summary>
public static class PhoneNumber
{
    /// <summary>Os 67 DDDs em uso no Brasil (Anatel).</summary>
    public static readonly IReadOnlySet<string> AreaCodes = new HashSet<string>(StringComparer.Ordinal)
    {
        "11", "12", "13", "14", "15", "16", "17", "18", "19",
        "21", "22", "24", "27", "28",
        "31", "32", "33", "34", "35", "37", "38",
        "41", "42", "43", "44", "45", "46", "47", "48", "49",
        "51", "53", "54", "55",
        "61", "62", "63", "64", "65", "66", "67", "68", "69",
        "71", "73", "74", "75", "77", "79",
        "81", "82", "83", "84", "85", "86", "87", "88", "89",
        "91", "92", "93", "94", "95", "96", "97", "98", "99"
    };

    /// <summary>Lê o que a pessoa digitou. Em <see cref="PhoneParseResult.Valid"/>, <paramref name="digits"/> traz 10 ou 11 dígitos.</summary>
    public static PhoneParseResult TryNormalize(string input, out string digits)
    {
        digits = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return PhoneParseResult.Empty;
        }

        string text = input.Trim();
        bool plus = text.StartsWith('+');
        StringBuilder only = new(text.Length);
        for (int i = plus ? 1 : 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c is >= '0' and <= '9')
            {
                only.Append(c);
            }
            else if (!(char.IsWhiteSpace(c) || c is '(' or ')' or '-' or '.'))
            {
                return PhoneParseResult.Invalid; // letra, símbolo ou "+" fora do começo
            }
        }

        string number = only.ToString();

        // Código do país: "+55 ..." ou "55..." com 12 ou 13 dígitos (um número nacional nunca tem esse tamanho)
        if (plus)
        {
            if (!number.StartsWith("55", StringComparison.Ordinal))
            {
                return PhoneParseResult.Invalid;
            }

            number = number[2..];
        }
        else if (number.Length is 12 or 13 && number.StartsWith("55", StringComparison.Ordinal))
        {
            number = number[2..];
        }

        if (number.Length is not (10 or 11) || !AreaCodes.Contains(number[..2]))
        {
            return PhoneParseResult.Invalid;
        }

        char first = number[2];
        bool valid = number.Length == 11 ? first == '9' : first is >= '2' and <= '9';
        if (!valid)
        {
            return PhoneParseResult.Invalid;
        }

        digits = number;
        return PhoneParseResult.Valid;
    }

    /// <summary>
    /// <c>(11) 91234-5678</c> (11 dígitos) ou <c>(11) 3456-7890</c> (10 dígitos). Recebe o que <see cref="TryNormalize"/> devolveu; texto de
    /// outro tamanho volta como veio.
    /// </summary>
    public static string Format(string digits) => digits switch
    {
        { Length: 11 } => $"({digits[..2]}) {digits[2..7]}-{digits[7..]}",
        { Length: 10 } => $"({digits[..2]}) {digits[2..6]}-{digits[6..]}",
        _ => digits
    };
}
