using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Web.Filters;

/// <summary>
/// Em /api, escrita sem o cabeçalho RequestVerificationToken válido responde ProblemDetails
/// 400 VALIDATION_ERROR (o contrato não tem código próprio para antiforgery). Nas páginas vale o
/// AutoValidateAntiforgeryToken do MVC.
/// </summary>
public sealed class AntiforgeryJsonFilter : IAsyncAuthorizationFilter, IOrderedFilter
{
    // Antes do AutoValidateAntiforgeryToken, para a resposta ser a do contrato de erros
    public int Order => int.MinValue;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        HttpRequest request = context.HttpContext.Request;
        bool dismissed = context.Filters.OfType<IgnoreAntiforgeryTokenAttribute>().Any();
        if (dismissed || !request.Path.StartsWithSegments("/api") || IsSafeMethod(request.Method))
        {
            return;
        }

        IAntiforgery antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["requisicao"] = ["Token de segurança ausente ou inválido. Recarregue a página e tente de novo."]
            });
        }
    }

    private static bool IsSafeMethod(string method) =>
        HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method);
}
