using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GazetaMarketplace.Web.Middleware;

/// <summary>
/// Cabeçalhos de segurança em toda resposta (NFR-10, ARCHITECTURE.md §7). HSTS só em produção e
/// só em HTTPS; a CSP não permite script nem estilo inline.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, bool producao)
{
    private const string Csp =
        "default-src 'self'; img-src 'self' data:; script-src 'self'; style-src 'self'; " +
        "frame-ancestors 'none'; form-action 'self'";

    private const string Hsts = "max-age=31536000; includeSubDomains";

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            IHeaderDictionary cabecalhos = context.Response.Headers;
            cabecalhos["X-Content-Type-Options"] = "nosniff";
            cabecalhos["X-Frame-Options"] = "DENY";
            cabecalhos["Referrer-Policy"] = "strict-origin-when-cross-origin";
            cabecalhos["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            cabecalhos["Content-Security-Policy"] = Csp;
            if (producao && context.Request.IsHttps)
            {
                cabecalhos["Strict-Transport-Security"] = Hsts;
            }

            return Task.CompletedTask;
        });

        return next(context);
    }
}
