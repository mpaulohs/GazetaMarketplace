using System;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Core.VehicleCatalog;
using GazetaMarketplace.Infrastructure.Ads;
using GazetaMarketplace.Infrastructure.Categories;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Email;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Infrastructure.Location;
using GazetaMarketplace.Infrastructure.Recovery;
using GazetaMarketplace.Infrastructure.Settings;
using GazetaMarketplace.Infrastructure.VehicleCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
        services.AddSingleton<CategoryTree>();
        services.AddSingleton<ICategoryTree>(provider => provider.GetRequiredService<CategoryTree>());
        services.AddScoped<ICategoryUsage, AdsCategoryUsage>();
        services.AddScoped<IAdService, AdService>();

        // CEP (ADR-007): uma tentativa de até 5 s por chamada; a nova tentativa é da tela. O endereço base só muda nos testes de ponta a ponta
        services.AddHttpClient<ICepLookup, ViaCepLookup>((provider, http) =>
        {
            ViaCepOptions options = provider.GetRequiredService<IOptions<ViaCepOptions>>().Value;
            http.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/");
            http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddScoped<ICityDirectory, CityDirectory>();
        services.AddScoped<ICepService, CepService>();
        services.AddScoped<ICategoryManagement, CategoryManagement>();
        services.AddSingleton<SiteSettingsStore>();
        services.AddSingleton<ISiteSettings>(provider => provider.GetRequiredService<SiteSettingsStore>());
        services.AddScoped<ISiteSettingsManagement, SiteSettingsManagement>();
        services.AddSingleton<VehicleCatalog.VehicleCatalog>();
        services.AddSingleton<IVehicleCatalog>(provider => provider.GetRequiredService<VehicleCatalog.VehicleCatalog>());
        services.AddDapper(configuration);
        services.AddScoped<IAuditLog, AuditLog>();
        services.AddScoped<IUserManagement, UserManagement>();
        services.AddScoped<IDatabaseReadiness, EfDatabaseReadiness>();

        // Recuperação de senha (US-007): o envio sai da fila depois da resposta (RC-13)
        services.AddSingleton<PasswordRecoveryQueue>();
        services.AddHostedService<PasswordRecoveryWorker>();
        services.AddSingleton<PasswordRecoveryCleanupService>();
        services.AddHostedService(provider => provider.GetRequiredService<PasswordRecoveryCleanupService>());
        services.AddScoped<PasswordRecoveryMailer>();
        services.AddScoped<IPasswordRecovery, PasswordRecoveryService>();

        return services;
    }

    /// <summary>
    /// Escolhe o remetente de e-mail: em Production, só o SendGrid; nos demais ambientes, o remetente de console
    /// (que mostra o link só em Development).
    /// </summary>
    public static IServiceCollection AddEmailSender(this IServiceCollection services, bool production)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (production)
        {
            services.AddHttpClient<IEmailSender, SendGridEmailSender>(http => http.Timeout = TimeSpan.FromSeconds(15));
        }
        else
        {
            services.TryAddSingleton<IEmailSender, LogEmailSender>();
        }

        return services;
    }
}
