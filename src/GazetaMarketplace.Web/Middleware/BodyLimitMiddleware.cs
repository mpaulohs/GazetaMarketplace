using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace GazetaMarketplace.Web.Middleware;

/// <summary>
/// Limite global de 1 MB para o corpo de requisição (RC-21). Rotas com <c>[RequestSizeLimit]</c>
/// (o envio de foto, 11 MB) têm limite próprio: ele vale no lugar deste, também aqui (um corpo declarado maior que o limite da rota recebe 413 antes de ser lido).
/// Roda depois do roteamento.
/// </summary>
public sealed class BodyLimitMiddleware(RequestDelegate next)
{
    public const long LimitInBytes = 1024 * 1024;

    public Task InvokeAsync(HttpContext context)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<IRequestSizeLimitMetadata>() is { } own)
        {
            if (own.MaxRequestBodySize is { } routeLimit && context.Request.ContentLength > routeLimit)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                return Task.CompletedTask;
            }

            return next(context);
        }

        // Cobre também corpo em partes (sem Content-Length) onde o servidor suporta o recurso
        IHttpMaxRequestBodySizeFeature feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false })
        {
            feature.MaxRequestBodySize = LimitInBytes;
        }

        if (context.Request.ContentLength > LimitInBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return Task.CompletedTask;
        }

        return next(context);
    }
}
