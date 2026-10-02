using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Core;

/// <summary>Registro dos serviços de domínio e aplicação do Core.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registra os serviços do Core. Cada módulo acrescenta o seu registro aqui.</summary>
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        return services;
    }
}
