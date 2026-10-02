using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Suporte;

/// <summary>Sink de teste: guarda os eventos de log em memória.</summary>
internal sealed class ColetorSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _eventos = new();

    public IReadOnlyList<LogEvent> Eventos => _eventos.ToList();

    public void Emit(LogEvent logEvent) => _eventos.Enqueue(logEvent);

    public static string Texto(LogEvent e) => e.RenderMessage();

    public static string TudoComoTexto(LogEvent e) =>
        e.RenderMessage() + " " + string.Join(" ", e.Properties.Select(p => p.Key + "=" + p.Value)) + " " + e.Exception;
}
