using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Infrastructure;

/// <summary>Registro das implementações de infraestrutura (dados, identidade, fotos, CEP, e-mail).</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registra a infraestrutura. Cada tarefa seguinte acrescenta os seus serviços aqui.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services;
    }
}
