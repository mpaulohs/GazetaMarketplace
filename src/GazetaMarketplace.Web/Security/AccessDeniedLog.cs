using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Navigation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Security;

/// <summary>
/// RC-16: toda negação de acesso no painel deixa uma linha de <c>Warning</c> com o pedido, o usuário e o papel. Nunca leva e-mail, nome nem o conteúdo do que foi negado.
/// Vale para a política por papel (o cookie redireciona para a tela "Acesso negado") e para a recusa dentro de uma tela (resposta 403 do anúncio).
/// </summary>
internal static class AccessDeniedLog
{
    public const string Category = "GazetaMarketplace.Web.Security.AccessDenied";

    public static void Write(ILogger logger, HttpContext context)
    {
        string role = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).FirstOrDefault() ?? "sem papel";
        logger.LogWarning(
            "Permissão negada em {Method} {Path} ao usuário {UserId} ({Role})",
            context.Request.Method, context.Request.Path, context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anônimo", role);
    }
}

/// <summary>
/// Registra a resposta 403 das telas do painel (anúncio de outro autor, situação que não aceita a ação). A tela "Acesso negado" fica de fora: o registro dela já saiu quando o cookie
/// redirecionou, com o endereço que a pessoa de fato pediu.
/// </summary>
internal sealed class AccessDeniedLoggingMiddleware(RequestDelegate next, ILoggerFactory loggers)
{
    private readonly ILogger _logger = loggers.CreateLogger(AccessDeniedLog.Category);

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);
        if (context.Response.StatusCode == StatusCodes.Status403Forbidden
            && context.Request.Path.StartsWithSegments("/painel")
            && !context.Request.Path.StartsWithSegments(PanelRoutes.AccessDenied))
        {
            AccessDeniedLog.Write(_logger, context);
        }
    }
}
