using System;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Dados;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GazetaMarketplace.Infrastructure;

/// <summary>Registro das implementações de infraestrutura (dados, identidade, fotos, CEP, e-mail).</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registra a infraestrutura. Cada tarefa seguinte acrescenta os seus serviços aqui.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Provisória até a tarefa 0.6; TryAdd deixa a implementação real substituí-la
        services.TryAddSingleton<IProntidaoDoBanco, ProntidaoDoBancoProvisoria>();

        return services;
    }
}
