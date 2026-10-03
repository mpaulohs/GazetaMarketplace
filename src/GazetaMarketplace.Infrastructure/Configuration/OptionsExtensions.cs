using System;
using GazetaMarketplace.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Infrastructure.Configuration;

/// <summary>Registro das opções tipadas (ADR-011).</summary>
public static class OptionsExtensions
{
    /// <summary>
    /// Liga as opções às seções de configuração. Em Production valida na partida: faltando um valor,
    /// o site não sobe, em vez de funcionar pela metade (ADR-011).
    /// </summary>
    public static IServiceCollection AddAppOptions(this IServiceCollection services, IConfiguration configuration, bool production)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        Validate(services.AddOptions<DatabaseOptions>()
            .Configure(o => o.DefaultConnection = configuration.GetConnectionString("DefaultConnection")), production);
        Validate(services.AddOptions<PhotoStorageOptions>().Bind(configuration.GetSection(PhotoStorageOptions.SectionName)), production);
        Validate(services.AddOptions<LogStorageOptions>().Bind(configuration.GetSection(LogStorageOptions.SectionName)), production);
        Validate(services.AddOptions<KeyStorageOptions>().Bind(configuration.GetSection(KeyStorageOptions.SectionName)), production);
        Validate(services.AddOptions<SendGridOptions>().Bind(configuration.GetSection(SendGridOptions.SectionName)), production);
        Validate(services.AddOptions<SiteOptions>().Bind(configuration.GetSection(SiteOptions.SectionName)), production);
        services.AddOptions<BootstrapOptions>().Bind(configuration.GetSection(BootstrapOptions.SectionName));

        // Não é segredo e tem padrão: o intervalo é validado em qualquer ambiente
        services.AddOptions<AuthenticationOptions>().Bind(configuration.GetSection(AuthenticationOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();

        return services;
    }

    private static void Validate<T>(OptionsBuilder<T> builder, bool production) where T : class
    {
        if (production)
        {
            builder.ValidateDataAnnotations().ValidateOnStart();
        }
    }
}
