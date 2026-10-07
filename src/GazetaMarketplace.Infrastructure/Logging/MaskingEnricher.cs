using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace GazetaMarketplace.Infrastructure.Logging;

/// <summary>
/// Garante que nenhum log leve senha, token, link de redefinição ou e-mail completo (NFR-18, ADR-010).
/// Propriedades de nome sensível viram "***"; e-mails viram "m***@dominio"; "token=..." em texto vira "token=***".
/// Limite conhecido: o texto da pilha de uma exceção não passa por aqui; não coloque dados pessoais em mensagens de exceção.
/// </summary>
public sealed partial class MaskingEnricher : ILogEventEnricher
{
    private const string Mask = "***";

    private static readonly string[] SensitiveNames =
    [
        "password", "senha", "token", "authorization", "secret", "apikey", "cookie", "resetlink", "linkderedefinicao"
    ];

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (KeyValuePair<string, LogEventPropertyValue> property in logEvent.Properties.ToList())
        {
            logEvent.AddOrUpdateProperty(new LogEventProperty(property.Key, Sanitize(property.Key, property.Value)));
        }
    }

    private static bool IsSensitiveName(string name) =>
        SensitiveNames.Any(s => name.Contains(s, StringComparison.OrdinalIgnoreCase));

    private static LogEventPropertyValue Sanitize(string name, LogEventPropertyValue value)
    {
        if (IsSensitiveName(name))
        {
            return new ScalarValue(Mask);
        }

        return value switch
        {
            ScalarValue { Value: string text } => new ScalarValue(MaskText(text)),
            StructureValue structure => new StructureValue(
                structure.Properties.Select(p => new LogEventProperty(p.Name, Sanitize(p.Name, p.Value))), structure.TypeTag),
            SequenceValue sequence => new SequenceValue(sequence.Elements.Select(e => Sanitize(string.Empty, e))),
            DictionaryValue dictionary => new DictionaryValue(dictionary.Elements.Select(pair =>
                new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                    pair.Key,
                    pair.Key.Value is string key ? Sanitize(key, pair.Value) : Sanitize(string.Empty, pair.Value)))),
            _ => value
        };
    }

    /// <summary>Texto que o mascaramento não conseguiu examinar a tempo (SC-04): vai no lugar do original, que pode esconder e-mail ou segredo.</summary>
    public const string Unmaskable = "[texto longo demais para mascarar]";

    private static string MaskText(string text)
    {
        try
        {
            string withoutEmail = Email().Replace(text, m => m.Groups[1].Value + "***@" + m.Groups[2].Value);
            return UrlSecret().Replace(withoutEmail, m => m.Groups[1].Value + "=" + Mask);
        }
        catch (RegexMatchTimeoutException)
        {
            return Unmaskable;
        }
    }

    // Tempo-limite: um caminho de milhares de letras sem "@" faz a busca de e-mail custar o quadrado do tamanho (SC-04)
    [GeneratedRegex(@"([A-Za-z0-9._%+\-])[A-Za-z0-9._%+\-]*@([A-Za-z0-9.\-]+\.[A-Za-z]{2,})", RegexOptions.None, matchTimeoutMilliseconds: 50)]
    private static partial Regex Email();

    [GeneratedRegex(@"\b(token|code|access_token|key)=[^&\s""']+", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 50)]
    private static partial Regex UrlSecret();
}
