using System;
using GazetaMarketplace.Core.Configuracao;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Infrastructure.Configuracao;

/// <summary>Registro das opções tipadas (ADR-011).</summary>
public static class OpcoesExtensions
{
    /// <summary>
    /// Liga as opções às seções de configuração. Em Production valida na partida: faltando um valor,
    /// o site não sobe, em vez de funcionar pela metade (ADR-011).
    /// </summary>
    public static IServiceCollection AddOpcoes(this IServiceCollection services, IConfiguration configuration, bool producao)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        Validar(services.AddOptions<DatabaseOptions>()
            .Configure(o => o.DefaultConnection = configuration.GetConnectionString("DefaultConnection")), producao);
        Validar(services.AddOptions<PhotoStorageOptions>().Bind(configuration.GetSection(PhotoStorageOptions.SectionName)), producao);
        Validar(services.AddOptions<LogStorageOptions>().Bind(configuration.GetSection(LogStorageOptions.SectionName)), producao);
        Validar(services.AddOptions<KeyStorageOptions>().Bind(configuration.GetSection(KeyStorageOptions.SectionName)), producao);
        Validar(services.AddOptions<SendGridOptions>().Bind(configuration.GetSection(SendGridOptions.SectionName)), producao);
        services.AddOptions<BootstrapOptions>().Bind(configuration.GetSection(BootstrapOptions.SectionName));

        return services;
    }

    private static void Validar<T>(OptionsBuilder<T> builder, bool producao) where T : class
    {
        if (producao)
        {
            builder.ValidateDataAnnotations().ValidateOnStart();
        }
    }
}
