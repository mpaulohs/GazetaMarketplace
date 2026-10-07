using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

    public static IApplicationBuilder UseSecureForwarding(this IApplicationBuilder app, IConfiguration configuration)
    {
        return ConfiguredProxies(configuration).Length == 0 ? app : app.UseForwardedHeaders();
    }
}
