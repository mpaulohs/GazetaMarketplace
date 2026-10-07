using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace GazetaMarketplace.Web.Security;

/// <summary>
/// Quantas tentativas por origem a entrada e a recuperação de senha aceitam em 15 minutos (SC-01, SC-02).
/// Com proxies conhecidos em <c>ForwardedHeaders:KnownProxies</c> o IP é o de cada visitante e o limite é o da rules/security.md: 5.
/// Sem eles, atrás do proxy do provedor, todo mundo chega com o IP do proxy e o limite por IP vira o limite do site inteiro:
/// enquanto a lista real não vem do provedor (SEC-01) o limite é 20, para uma pessoa errando a senha não travar a redação.
/// </summary>
public static class AuthLimits
{
    public const int Default = 5;

    public const int WhileProxyUnknown = 20;

    /// <summary>Falhas de entrada por origem. Existe na configuração para a suíte de testes; em produção quem decide é a lista de proxies.</summary>
    public const string LoginFailuresKey = "RateLimiting:LoginFailuresPerOrigin";

    /// <summary>Valor que vale quando a configuração não escolhe: 5 com proxies conhecidos, 20 sem eles.</summary>
    public static int ForOrigin(IConfiguration configuration) =>
        ForwardingExtensions.ConfiguredProxies(configuration).Length > 0 ? Default : WhileProxyUnknown;

    public static int LoginFailures(IConfiguration configuration) => Configured(configuration, LoginFailuresKey) ?? ForOrigin(configuration);

    /// <summary>Número inteiro positivo da configuração; ausente, zero, negativo ou ilegível não conta.</summary>
    public static int? Configured(IConfiguration configuration, string key) =>
        int.TryParse(configuration[key], NumberStyles.None, CultureInfo.InvariantCulture, out int value) && value > 0 ? value : null;
}
