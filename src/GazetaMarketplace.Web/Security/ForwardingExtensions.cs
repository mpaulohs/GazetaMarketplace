using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Security;

/// <summary>
/// IP do cliente atrás do proxy do provedor (RC-10, SEC-01). Fail-closed: sem proxies configurados em
/// <c>ForwardedHeaders:KnownProxies</c>, o middleware nem entra no pipeline. Com as listas de origem vazias,
/// o <c>UseForwardedHeaders</c> confiaria em qualquer remetente de X-Forwarded-For.
/// </summary>
public static class ForwardingExtensions
{
    private const string Section = "ForwardedHeaders:KnownProxies";

    public static string[] ConfiguredProxies(IConfiguration configuration) =>
        configuration.GetSection(Section).Get<string[]>()?.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray() ?? [];

    public static IServiceCollection AddSecureForwarding(this IServiceCollection services)
    {
        // Lazy: lê a configuração só quando as opções são usadas (inclui o que o host de teste acrescenta)
        services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (string proxy in ConfiguredProxies(configuration))
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        });

        return services;
    }

    /// <summary>Texto do aviso de partida (também usado pelos testes e pelo runbook).</summary>
    public const string UnknownProxiesWarning =
        "ForwardedHeaders:KnownProxies não está configurado: atrás do proxy do provedor o IP de todos os visitantes é o mesmo, então os limites por IP valem para o site e a equipe inteiros "
        + "(enquanto isso, entrar, 'esqueci' e 'redefinir' aceitam 20 tentativas por 15 minutos em vez de 5). Peça o IP do proxy ao SmarterASP (SEC-01) e grave ForwardedHeaders__KnownProxies__0 no web.Production.config";

    /// <summary>Registra um <c>Warning</c> na partida se não há proxies conhecidos. O comportamento não muda: sem a lista o site ignora <c>X-Forwarded-For</c> (fail-closed).</summary>
    public static void WarnIfProxiesAreUnknown(this Microsoft.AspNetCore.Builder.WebApplication app)
    {
        if (ConfiguredProxies(app.Configuration).Length == 0)
        {
            app.Logger.LogWarning(UnknownProxiesWarning);
        }
    }

    public static IApplicationBuilder UseSecureForwarding(this IApplicationBuilder app, IConfiguration configuration)
    {
        return ConfiguredProxies(configuration).Length == 0 ? app : app.UseForwardedHeaders();
    }
}
