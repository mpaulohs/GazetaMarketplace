using System;
using System.Linq;
using System.Net;
using Cloudflare.ForwardedHeaders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Security;

/// <summary>
/// IP do cliente atrás do proxy do provedor (RC-10, SEC-01). Fail-closed: sem proxies configurados em
/// <c>ForwardedHeaders:KnownProxies</c>, o middleware nem entra no pipeline. Com as listas de origem vazias,
/// o <c>UseForwardedHeaders</c> confiaria em qualquer remetente de X-Forwarded-For.
/// Atrás do Cloudflare (<c>ForwardedHeaders:Cloudflare</c> = true) o IP do visitante vem de <c>CF-Connecting-IP</c> e só vale quando o pedido
/// chega de um endereço do Cloudflare; a lista de faixas é a do cloudflare.com, com a cópia embutida no pacote como reserva.
/// </summary>
public static class ForwardingExtensions
{
    private const string Section = "ForwardedHeaders:KnownProxies";

    /// <summary>Liga o modo Cloudflare. Desligado por padrão: sem ele o site nem baixa a lista de faixas.</summary>
    public const string CloudflareKey = "ForwardedHeaders:Cloudflare";

    /// <summary>Cabeçalho em que o Cloudflare entrega o IP do visitante (um só valor; o Cloudflare sobrescreve o que o cliente mandar).</summary>
    public const string CloudflareIpHeader = "CF-Connecting-IP";

    public static bool CloudflareEnabled(IConfiguration configuration) => configuration.GetValue<bool>(CloudflareKey);

    /// <summary>Há uma origem de IP confiável configurada (proxies conhecidos ou Cloudflare): o IP do visitante é o real e os limites são os da rules/security.md.</summary>
    public static bool HasTrustedForwarding(IConfiguration configuration) =>
        CloudflareEnabled(configuration) || ConfiguredProxies(configuration).Length > 0;

    public static string[] ConfiguredProxies(IConfiguration configuration) =>
        configuration.GetSection(Section).Get<string[]>()?.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray() ?? [];

    public static IServiceCollection AddSecureForwarding(this IServiceCollection services, IConfiguration configuration)
    {
        // A lista de faixas do Cloudflare é baixada quando as opções são montadas (partida); registrar só no modo Cloudflare evita rede em desenvolvimento e nos testes.
        // O pacote só preenche a lista de redes confiáveis; o cabeçalho do IP (CF-Connecting-IP) é escolhido abaixo.
        if (CloudflareEnabled(configuration))
        {
            services.AddCloudflareForwardedHeaders(options => options.FetchTimeout = TimeSpan.FromSeconds(5));
        }

        // Lazy: lê a configuração só quando as opções são usadas (inclui o que o host de teste acrescenta)
        services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            if (CloudflareEnabled(configuration))
            {
                options.ForwardedForHeaderName = CloudflareIpHeader;
            }

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

    /// <summary>
    /// Registra um <c>Warning</c> na partida se não há origem de IP confiável. O comportamento não muda: sem a lista o site ignora <c>X-Forwarded-For</c> (fail-closed).
    /// No modo Cloudflare registra, em vez disso, quantas faixas do Cloudflare o site confia (zero seria o sinal de que a lista não carregou).
    /// </summary>
    public static void WarnIfProxiesAreUnknown(this Microsoft.AspNetCore.Builder.WebApplication app)
    {
        if (CloudflareEnabled(app.Configuration))
        {
            ForwardedHeadersOptions options = app.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
            int ranges = options.KnownIPNetworks.Count;
            if (ranges == 0)
            {
                app.Logger.LogError("Modo Cloudflare ligado, mas nenhuma faixa de IP do Cloudflare foi carregada: o site ignora CF-Connecting-IP e todo visitante parece ter o IP do Cloudflare");
            }
            else
            {
                app.Logger.LogInformation("Modo Cloudflare ligado: {Ranges} faixas de IP do Cloudflare confiáveis; o IP do visitante vem de {Header}", ranges, CloudflareIpHeader);
            }
        }
        else if (ConfiguredProxies(app.Configuration).Length == 0)
        {
            app.Logger.LogWarning(UnknownProxiesWarning);
        }
    }

    public static IApplicationBuilder UseSecureForwarding(this IApplicationBuilder app, IConfiguration configuration)
    {
        return HasTrustedForwarding(configuration) ? app.UseForwardedHeaders() : app;
    }
}
