using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Excecoes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Seguranca;

/// <summary>Limites de requisição por IP (NFR-06, rules/security.md): login e "esqueci minha senha" 5 por 15 min; global 100 por minuto.</summary>
public static class LimitadoresExtensions
{
    /// <summary>Política para as ações de login e de recuperação de senha: <c>[EnableRateLimiting("auth")]</c>.</summary>
    public const string PoliticaAuth = "auth";

    private static readonly string[] PrefixosEstaticos = ["/lib/", "/css/", "/js/", "/images/", "/favicon.ico"];

    public static IServiceCollection AddLimitadores(this IServiceCollection services)
    {
        services.AddRateLimiter(opcoes =>
        {
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Uma página com dezenas de cards já passaria de 100 arquivos: os estáticos não contam
            opcoes.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
                EhArquivoEstatico(contexto.Request.Path)
                    ? RateLimitPartition.GetNoLimiter("estaticos")
                    : RateLimitPartition.GetFixedWindowLimiter(IpDoCliente(contexto), _ => Janela(100, TimeSpan.FromMinutes(1))));

            opcoes.AddPolicy(PoliticaAuth, contexto =>
                RateLimitPartition.GetFixedWindowLimiter(IpDoCliente(contexto), _ => Janela(5, TimeSpan.FromMinutes(15))));

            opcoes.OnRejected = ResponderExcessoAsync;
        });

        return services;
    }

    private static FixedWindowRateLimiterOptions Janela(int limite, TimeSpan duracao) => new()
    {
        PermitLimit = limite,
        Window = duracao,
        QueueLimit = 0
    };

    private static bool EhArquivoEstatico(PathString caminho)
    {
        foreach (string prefixo in PrefixosEstaticos)
        {
            if (caminho.StartsWithSegments(prefixo.TrimEnd('/')) || caminho.Value.Equals(prefixo, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // O IP vem do servidor, já corrigido pelo UseForwardedHeaders quando há proxy conhecido (RC-10)
    private static string IpDoCliente(HttpContext contexto) => contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

    private static async ValueTask ResponderExcessoAsync(OnRejectedContext contexto, System.Threading.CancellationToken cancelamento)
    {
        HttpContext http = contexto.HttpContext;
        http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("LimiteDeRequisicoes")
            .LogWarning("Limite de requisições excedido em {Method} {Path}", http.Request.Method, http.Request.Path);

        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan espera))
        {
            http.Response.Headers.RetryAfter = ((int)Math.Ceiling(espera.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (http.Request.Path.StartsWithSegments("/api"))
        {
            ProblemDetails problema = new()
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status429TooManyRequests),
                Detail = "Muitas tentativas. Tente novamente em alguns instantes.",
                Instance = http.Request.Path
            };
            problema.Extensions["code"] = "RATE_LIMITED";
            problema.Extensions["traceId"] = http.TraceIdentifier;
            await http.Response.WriteAsJsonAsync(problema, options: null, contentType: "application/problem+json", cancellationToken: cancelamento);
            return;
        }

        http.Response.ContentType = "text/plain; charset=utf-8";
        await http.Response.WriteAsync("Muitas tentativas. Tente novamente em alguns instantes.", cancelamento);
    }
}
