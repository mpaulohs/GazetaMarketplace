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

    /// <summary>
    /// Teto de cada arquivo (SC-04): sem ele o Serilog para em 1 GB por dia e descarta os eventos seguintes, inclusive os de login e acesso negado (RC-16).
    /// Passando do teto o arquivo rola para o próximo (<c>gazeta-AAAAMMDD_001.json</c>); o limite de 14 arquivos vale para todos eles.
    /// </summary>
    public const long DefaultFileSizeLimitBytes = 100L * 1024 * 1024;

    public static LoggerConfiguration Configure(
        LoggerConfiguration configuration,
        string logsFolder,
        bool production,
        IEnumerable<ILogEventSink> extraSinks = null,
        long fileSizeLimitBytes = DefaultFileSizeLimitBytes)
    {
        configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            // O log padrão do HttpClient grava a URL inteira: a do ViaCEP leva o CEP do vendedor (SC-12); o do SendGrid, o caminho da API
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With(new MaskingEnricher());

        bool hasFolder = !string.IsNullOrWhiteSpace(logsFolder);
        if (hasFolder)
        {
            configuration.WriteTo.File(
                new CompactJsonFormatter(),
                Path.Combine(logsFolder, "gazeta-.json"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: fileSizeLimitBytes,
                rollOnFileSizeLimit: true,
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
