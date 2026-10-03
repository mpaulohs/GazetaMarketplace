using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Security;

/// <summary>Limites de requisição por IP (NFR-06, rules/security.md): login e "esqueci minha senha" 5 por 15 min; global 100 por minuto.</summary>
public static class RateLimitingExtensions
{
    /// <summary>Política para as ações de login e de recuperação de senha: <c>[EnableRateLimiting("auth")]</c>.</summary>
    public const string AuthPolicy = "auth";

    private static readonly string[] StaticPrefixes = ["/lib/", "/css/", "/js/", "/images/", "/favicon.ico"];

    public static IServiceCollection AddRateLimiters(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Uma página com dezenas de cards já passaria de 100 arquivos: os estáticos não contam
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                IsStaticFile(context.Request.Path)
                    ? RateLimitPartition.GetNoLimiter("estaticos")
                    : RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => Window(100, TimeSpan.FromMinutes(1))));

            options.AddPolicy(AuthPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => Window(5, TimeSpan.FromMinutes(15))));

            options.OnRejected = RespondTooManyRequestsAsync;
        });

        return services;
    }

    private static FixedWindowRateLimiterOptions Window(int limit, TimeSpan duration) => new()
    {
        PermitLimit = limit,
        Window = duration,
        QueueLimit = 0
    };

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
