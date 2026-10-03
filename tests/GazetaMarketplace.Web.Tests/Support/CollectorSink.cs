using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Sink de teste: guarda os eventos de log em memória.</summary>
internal sealed class CollectorSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();

    public IReadOnlyList<LogEvent> Events => _events.ToList();

    public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);

    public static string Text(LogEvent e) => e.RenderMessage();

    public static string AllAsText(LogEvent e) =>
        e.RenderMessage() + " " + string.Join(" ", e.Properties.Select(p => p.Key + "=" + p.Value)) + " " + e.Exception;
}
