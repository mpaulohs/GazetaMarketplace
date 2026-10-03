using System;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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

        services.TryAddSingleton(TimeProvider.System);
        // Sem requisição (a Web registra a versão HTTP antes desta chamada), as ações são do sistema
        services.TryAddScoped<ICurrentUser, SystemUser>();

        // A cadeia é lida só quando o contexto é criado (inclui valores acrescentados depois pelo host)
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));
        services.AddDapper(configuration);
        services.AddScoped<IAuditLog, AuditLog>();
        services.AddScoped<IDatabaseReadiness, EfDatabaseReadiness>();

        return services;
    }
}
