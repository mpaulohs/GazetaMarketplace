using System.Collections.Generic;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace GazetaMarketplace.Infrastructure.Logging;

/// <summary>Configuração do Serilog (ADR-010): arquivo JSON diário fora da raiz do site, 14 dias de retenção.</summary>
public static class SerilogConfiguration
{
    public const int RetencaoEmDias = 14;

    public static LoggerConfiguration Configurar(
        LoggerConfiguration configuracao,
        string pastaDosLogs,
        bool producao,
        IEnumerable<ILogEventSink> sinksExtras = null)
    {
        configuracao
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With(new MascaramentoEnricher());

        bool temPasta = !string.IsNullOrWhiteSpace(pastaDosLogs);
        if (temPasta)
        {
            configuracao.WriteTo.File(
                new CompactJsonFormatter(),
                Path.Combine(pastaDosLogs, "gazeta-.json"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: RetencaoEmDias);
        }

        // Em produção o stdout não é coletado (ADR-010); sem pasta configurada, o console é a única
        // chance de ver por que o site não subiu.
        if (!producao || !temPasta)
        {
            configuracao.WriteTo.Console();
        }

        if (sinksExtras is not null)
        {
            foreach (ILogEventSink sink in sinksExtras)
            {
                configuracao.WriteTo.Sink(sink);
            }
        }

        return configuracao;
    }
}
