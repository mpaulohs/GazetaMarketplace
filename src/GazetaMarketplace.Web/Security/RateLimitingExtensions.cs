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

/// <summary>Limites de requisição por IP (NFR-06, rules/security.md): "esqueci" e "redefinir" 5 por 15 min cada (20 enquanto o IP do proxy não é conhecido); entrar conta só falhas; global 100 por minuto.</summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// "Esqueci minha senha": <c>[EnableRateLimiting("auth-esqueci")]</c>. Balde próprio por IP (SC-02): antes entrar, "esqueci" e "redefinir" dividiam um só,
    /// e quem errava a senha 5 vezes não conseguia sequer pedir a redefinição. Entrar não tem política: só as <b>falhas</b> contam, no <see cref="LoginFailureCounter"/>.
    /// </summary>
    public const string ForgotPolicy = "auth-esqueci";

    /// <summary>"Redefinir senha" (o formulário aberto pelo link do e-mail): <c>[EnableRateLimiting("auth-redefinir")]</c>, com balde próprio por IP.</summary>
    public const string ResetPolicy = "auth-redefinir";

    /// <summary>
    /// Chave de configuração do limite de "esqueci" e "redefinir" (cada um com o seu balde de 15 minutos por IP). Existe para a suíte E2E, que entra dezenas de
    /// vezes do mesmo IP; ausente, vale <see cref="AuthLimits.ForOrigin"/>: 5 com proxies conhecidos e 20 enquanto a lista não vem do provedor (SC-01).
    /// </summary>
    public const string AuthPermitsKey = "RateLimiting:AuthPermits";

    /// <summary>Política da consulta de CEP: <c>[EnableRateLimiting("cep")]</c>. 30 por minuto <b>por usuário</b> (a primeira política por usuário do site), para a equipe não esgotar a cota do ViaCEP.</summary>
    public const string CepPolicy = "cep";

    public const int CepPerMinute = 30;

    /// <summary>Política da entrega de fotos: <c>[EnableRateLimiting("fotos")]</c>. 300 por minuto <b>por IP</b>: cabe uma página de 24 cards com 2 fotos (48 pedidos) várias vezes e ainda trava quem tenta enumerar os ids.</summary>
    public const string PhotoPolicy = "fotos";

    public const int PhotosPerMinute = 300;

    /// <summary>Chave de configuração do limite de entrega de fotos por IP. Existe para a suíte E2E (a página de um anúncio com 20 fotos já pede mais de 20 imagens, e a suíte abre dezenas de páginas de um IP só); em produção vale 300.</summary>
    public const string PhotosPerMinuteKey = "RateLimiting:PhotosPerMinute";

    /// <summary>Política do envio de fotos: <c>[EnableRateLimiting("fotos-envio")]</c>. 30 por minuto <b>por usuário</b>, contando cada arquivo (uma foto por pedido, RC-6): cada envio gasta CPU e disco.</summary>
    public const string PhotoUploadPolicy = "fotos-envio";

    public const int PhotoUploadsPerMinute = 30;

    /// <summary>Chave de configuração do limite de envios por usuário. Como a do limite global, existe para a suíte E2E (que sobe dezenas de fotos de uma conta só em poucos minutos); em produção vale 30.</summary>
    public const string PhotoUploadsPerMinuteKey = "RateLimiting:PhotoUploadsPerMinute";

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

            options.AddPolicy(ForgotPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => Window(AuthPermits(context), TimeSpan.FromMinutes(15))));

            options.AddPolicy(ResetPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => Window(AuthPermits(context), TimeSpan.FromMinutes(15))));

            // Por usuário logado; sem identidade (não deveria chegar aqui, a ação exige login) cai no IP
            options.AddPolicy(CepPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } user ? "u:" + user : "ip:" + ClientIp(context),
                    _ => Window(CepPerMinute, TimeSpan.FromMinutes(1))));

            options.AddPolicy(PhotoUploadPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } user ? "u:" + user : "ip:" + ClientIp(context),
                    _ => Window(Configured(context, PhotoUploadsPerMinuteKey, PhotoUploadsPerMinute), TimeSpan.FromMinutes(1))));

            options.AddPolicy(PhotoPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => Window(Configured(context, PhotosPerMinuteKey, PhotosPerMinute), TimeSpan.FromMinutes(1))));

            options.OnRejected = RespondTooManyRequestsAsync;
        });

        return services;
    }

    // Lido quando a janela de um IP é criada (não na partida): a configuração do host de teste só existe depois que o Program.cs rodou.
    // Ausente, zero, negativa ou ilegível vale o padrão.
    private static int GlobalPerMinute(HttpContext context) => Configured(context, GlobalPerMinuteKey, DefaultGlobalPerMinute);

    private static int AuthPermits(HttpContext context)
    {
        IConfiguration configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        return AuthLimits.Configured(configuration, AuthPermitsKey) ?? AuthLimits.ForOrigin(configuration);
    }

    private static int Configured(HttpContext context, string key, int fallback) =>
        int.TryParse(context.RequestServices.GetRequiredService<IConfiguration>()[key], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int configured) && configured > 0
            ? configured
            : fallback;

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

    // Um aviso por IP por minuto e com o caminho cortado (SC-04): quem passa do limite pode mandar milhares de pedidos por minuto
    // com caminhos de 4 KB, e cada aviso a mais encheria o arquivo de log até o dia perder os eventos de segurança.
    private const int MaxLoggedPathLength = 200;

    private const int MaxSampledOrigins = 10_000;

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> LastRejectionLog = new();

    private static void LogRejection(HttpContext http)
    {
        long now = Environment.TickCount64;
        string origin = ClientIp(http);
        if (LastRejectionLog.Count > MaxSampledOrigins)
        {
            LastRejectionLog.Clear();
        }

        bool recent = LastRejectionLog.TryGetValue(origin, out long last) && now - last < 60_000;
        if (recent)
        {
            return;
        }

        LastRejectionLog[origin] = now;
        string path = http.Request.Path.Value ?? string.Empty;
        if (path.Length > MaxLoggedPathLength)
        {
            path = string.Concat(path.AsSpan(0, MaxLoggedPathLength), "…");
        }

        http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("LimiteDeRequisicoes")
            .LogWarning("Limite de requisições excedido em {Method} {Path} (origem {Origin}; no máximo um aviso por origem por minuto)", http.Request.Method, path, origin);
    }

    private static async ValueTask RespondTooManyRequestsAsync(OnRejectedContext context, System.Threading.CancellationToken cancellation)
    {
        HttpContext http = context.HttpContext;
        LogRejection(http);

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
