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
public sealed partial class MascaramentoEnricher : ILogEventEnricher
{
    private const string Mascara = "***";

    private static readonly string[] NomesSensiveis =
    [
        "password", "senha", "token", "authorization", "secret", "apikey", "cookie", "resetlink", "linkderedefinicao"
    ];

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (KeyValuePair<string, LogEventPropertyValue> propriedade in logEvent.Properties.ToList())
        {
            logEvent.AddOrUpdateProperty(new LogEventProperty(propriedade.Key, Sanitizar(propriedade.Key, propriedade.Value)));
        }
    }

    private static bool NomeSensivel(string nome) =>
        NomesSensiveis.Any(s => nome.Contains(s, StringComparison.OrdinalIgnoreCase));

    private static LogEventPropertyValue Sanitizar(string nome, LogEventPropertyValue valor)
    {
        if (NomeSensivel(nome))
        {
            return new ScalarValue(Mascara);
        }

        return valor switch
        {
            ScalarValue { Value: string texto } => new ScalarValue(MascararTexto(texto)),
            StructureValue estrutura => new StructureValue(
                estrutura.Properties.Select(p => new LogEventProperty(p.Name, Sanitizar(p.Name, p.Value))), estrutura.TypeTag),
            SequenceValue sequencia => new SequenceValue(sequencia.Elements.Select(e => Sanitizar(string.Empty, e))),
            DictionaryValue dicionario => new DictionaryValue(dicionario.Elements.Select(par =>
                new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                    par.Key,
                    par.Key.Value is string chave ? Sanitizar(chave, par.Value) : Sanitizar(string.Empty, par.Value)))),
            _ => valor
        };
    }

    private static string MascararTexto(string texto)
    {
        string semEmail = Email().Replace(texto, m => m.Groups[1].Value + "***@" + m.Groups[2].Value);
        return SegredoNaUrl().Replace(semEmail, m => m.Groups[1].Value + "=" + Mascara);
    }

    [GeneratedRegex(@"([A-Za-z0-9._%+\-])[A-Za-z0-9._%+\-]*@([A-Za-z0-9.\-]+\.[A-Za-z]{2,})")]
    private static partial Regex Email();

    [GeneratedRegex(@"\b(token|code|access_token|key)=[^&\s""']+", RegexOptions.IgnoreCase)]
    private static partial Regex SegredoNaUrl();
}
