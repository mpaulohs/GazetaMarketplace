using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace GazetaMarketplace.Web.Middleware;

/// <summary>
/// Limite global de 1 MB para o corpo de requisição (RC-21). Rotas com <c>[RequestSizeLimit]</c>
/// (o envio de foto, 11 MB) têm limite próprio e ficam fora deste. Roda depois do roteamento.
/// </summary>
public sealed class LimiteCorpoMiddleware(RequestDelegate next)
{
    public const long LimiteEmBytes = 1024 * 1024;

    public Task InvokeAsync(HttpContext context)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<IRequestSizeLimitMetadata>() is not null)
        {
            return next(context);
        }

        // Cobre também corpo em partes (sem Content-Length) onde o servidor suporta o recurso
        IHttpMaxRequestBodySizeFeature recurso = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (recurso is { IsReadOnly: false })
        {
            recurso.MaxRequestBodySize = LimiteEmBytes;
        }

        if (context.Request.ContentLength > LimiteEmBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return Task.CompletedTask;
        }

        return next(context);
    }
}
