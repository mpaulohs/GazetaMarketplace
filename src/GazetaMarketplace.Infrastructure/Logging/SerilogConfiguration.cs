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
    public const int RetentionDays = 14;

    public static LoggerConfiguration Configure(
        LoggerConfiguration configuration,
        string logsFolder,
        bool production,
        IEnumerable<ILogEventSink> extraSinks = null)
    {
        configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With(new MaskingEnricher());

        bool hasFolder = !string.IsNullOrWhiteSpace(logsFolder);
        if (hasFolder)
        {
            configuration.WriteTo.File(
                new CompactJsonFormatter(),
                Path.Combine(logsFolder, "gazeta-.json"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: RetentionDays);
        }

        // Em produção o stdout não é coletado (ADR-010); sem pasta configurada, o console é a única
        // chance de ver por que o site não subiu.
        if (!production || !hasFolder)
        {
            configuration.WriteTo.Console();
        }

        if (extraSinks is not null)
        {
            foreach (ILogEventSink sink in extraSinks)
            {
                configuration.WriteTo.Sink(sink);
            }
        }

        return configuration;
    }
}
