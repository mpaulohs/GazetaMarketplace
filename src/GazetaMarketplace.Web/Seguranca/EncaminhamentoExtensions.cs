using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Web.Seguranca;

/// <summary>
/// IP do cliente atrás do proxy do provedor (RC-10, SEC-01). Fail-closed: sem proxies configurados em
/// <c>ForwardedHeaders:KnownProxies</c>, o middleware nem entra no pipeline. Com as listas de origem vazias,
/// o <c>UseForwardedHeaders</c> confiaria em qualquer remetente de X-Forwarded-For.
/// </summary>
public static class EncaminhamentoExtensions
{
    private const string Secao = "ForwardedHeaders:KnownProxies";

    private static string[] ProxiesConfigurados(IConfiguration configuracao) =>
        configuracao.GetSection(Secao).Get<string[]>()?.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray() ?? [];

    public static IServiceCollection AddEncaminhamentoSeguro(this IServiceCollection services)
    {
        // Lazy: lê a configuração só quando as opções são usadas (inclui o que o host de teste acrescenta)
        services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((opcoes, configuracao) =>
        {
            opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            opcoes.ForwardLimit = 1;
            opcoes.KnownProxies.Clear();
            opcoes.KnownIPNetworks.Clear();
            foreach (string proxy in ProxiesConfigurados(configuracao))
            {
                opcoes.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        });

        return services;
    }

    public static IApplicationBuilder UseEncaminhamentoSeguro(this IApplicationBuilder app, IConfiguration configuracao)
    {
        return ProxiesConfigurados(configuracao).Length == 0 ? app : app.UseForwardedHeaders();
    }
}
