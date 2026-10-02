using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Excecoes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Middleware;

/// <summary>
/// Contrato de erros dos endpoints JSON (ARCHITECTURE.md §8): ProblemDetails com código estável e traceId.
/// Erros inesperados devolvem mensagem genérica; a pilha fica só no log.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // O cliente desistiu; não é erro do sistema
            context.Response.StatusCode = 499;
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await ResponderAsync(context, ex);
        }
    }

    private async Task ResponderAsync(HttpContext context, Exception ex)
    {
        (int status, string codigo, string detalhe, IReadOnlyDictionary<string, string[]> erros) = ex switch
        {
            ValidationException v => (v.StatusCode, v.Code, v.Message, v.Errors),
            AppException a => (a.StatusCode, a.Code, a.Message, null),
            _ => (500, "INTERNAL_ERROR", "Ocorreu um erro inesperado.", null)
        };

        if (status >= 500)
        {
            logger.LogError(ex, "Erro não tratado em {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning("Erro do cliente {Code} em {Method} {Path}: {Detalhe}",
                codigo, context.Request.Method, context.Request.Path, detalhe);
        }

        ProblemDetails problema = new()
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = detalhe,
            Instance = context.Request.Path
        };
        problema.Extensions["code"] = codigo;
        problema.Extensions["traceId"] = context.TraceIdentifier;
        if (erros is not null)
        {
            problema.Extensions["errors"] = erros;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problema, options: null, contentType: "application/problem+json");
    }
}
