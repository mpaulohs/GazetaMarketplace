using System;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Security;

/// <summary>Limites de requisição por IP (NFR-06, rules/security.md): login e "esqueci minha senha" 5 por 15 min; global 100 por minuto.</summary>
public static class RateLimitingExtensions
{
    /// <summary>Política para as ações de login e de recuperação de senha: <c>[EnableRateLimiting("auth")]</c>.</summary>
    public const string AuthPolicy = "auth";

    /// <summary>Política da consulta de CEP: <c>[EnableRateLimiting("cep")]</c>. 30 por minuto <b>por usuário</b> (a primeira política por usuário do site), para a equipe não esgotar a cota do ViaCEP.</summary>
    public const string CepPolicy = "cep";

    public const int CepPerMinute = 30;

    /// <summary>Política da entrega de fotos: <c>[EnableRateLimiting("fotos")]</c>. 300 por minuto <b>por IP</b>: cabe uma página de 24 cards com 2 fotos (48 pedidos) várias vezes e ainda trava quem tenta enumerar os ids.</summary>
    public const string PhotoPolicy = "fotos";

    public const int PhotosPerMinute = 300;

    private static readonly string[] OwnLimitPrefixes = ["/fotos/"];

    private static readonly string[] StaticPrefixes = ["/lib/", "/css/", "/js/", "/images/", "/favicon.ico"];

    /// <summary>Pedidos por minuto e por IP quando <c>RateLimiting:GlobalPerMinute</c> não está configurada: 100 (rules/security.md).</summary>
    public const int DefaultGlobalPerMinute = 100;

    /// <summary>Chave de configuração do limite global. Existe para a suíte E2E (que faz centenas de pedidos de um IP só) rodar contra o site publicado.</summary>
    public const string GlobalPerMinuteKey = "RateLimiting:GlobalPerMinute";

    public static IServiceCollection AddRateLimiters(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Uma página com dezenas de cards já passaria de 100 arquivos: os estáticos não contam
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                IsStaticFile(context.Request.Path) || HasOwnLimit(context.Request.Path)
                    ? RateLimitPartition.GetNoLimiter("estaticos")
                    : RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => Window(GlobalPerMinute(context), TimeSpan.FromMinutes(1))));

            options.AddPolicy(AuthPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => Window(5, TimeSpan.FromMinutes(15))));

            // Por usuário logado; sem identidade (não deveria chegar aqui, a ação exige login) cai no IP
            options.AddPolicy(CepPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } user ? "u:" + user : "ip:" + ClientIp(context),
                    _ => Window(CepPerMinute, TimeSpan.FromMinutes(1))));

            options.AddPolicy(PhotoPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => Window(PhotosPerMinute, TimeSpan.FromMinutes(1))));

            options.OnRejected = RespondTooManyRequestsAsync;
        });

        return services;
    }

    // Lido quando a janela de um IP é criada (não na partida): a configuração do host de teste só existe depois que o Program.cs rodou.
    // Ausente, zero, negativa ou ilegível vale 100. O limite de login e de recuperação de senha (5 por 15 min) não é configurável.
    private static int GlobalPerMinute(HttpContext context) =>
        int.TryParse(context.RequestServices.GetRequiredService<IConfiguration>()[GlobalPerMinuteKey], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int configured) && configured > 0
            ? configured
            : DefaultGlobalPerMinute;

    private static FixedWindowRateLimiterOptions Window(int limit, TimeSpan duration) => new()
    {
        PermitLimit = limit,
        Window = duration,
        QueueLimit = 0
    };

    // As fotos têm a própria política; contar também no limite global faria uma página de cards estourar os 100
    private static bool HasOwnLimit(PathString path)
    {
        foreach (string prefix in OwnLimitPrefixes)
        {
            if (path.StartsWithSegments(prefix.TrimEnd('/')))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsStaticFile(PathString path)
    {
        foreach (string prefix in StaticPrefixes)
        {
            if (path.StartsWithSegments(prefix.TrimEnd('/')) || path.Value.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // O IP vem do servidor, já corrigido pelo UseForwardedHeaders quando há proxy conhecido (RC-10)
    private static string ClientIp(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

    private static async ValueTask RespondTooManyRequestsAsync(OnRejectedContext context, System.Threading.CancellationToken cancellation)
    {
        HttpContext http = context.HttpContext;
        http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("LimiteDeRequisicoes")
            .LogWarning("Limite de requisições excedido em {Method} {Path}", http.Request.Method, http.Request.Path);

        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan wait))
        {
            http.Response.Headers.RetryAfter = ((int)Math.Ceiling(wait.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (http.Request.Path.StartsWithSegments("/api"))
        {
            ProblemDetails problem = new()
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status429TooManyRequests),
                Detail = "Muitas tentativas. Tente novamente em alguns instantes.",
                Instance = http.Request.Path
            };
            problem.Extensions["code"] = "RATE_LIMITED";
            problem.Extensions["traceId"] = http.TraceIdentifier;
            await http.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: cancellation);
            return;
        }

        http.Response.ContentType = "text/plain; charset=utf-8";
        await http.Response.WriteAsync("Muitas tentativas. Tente novamente em alguns instantes.", cancellation);
    }
}
