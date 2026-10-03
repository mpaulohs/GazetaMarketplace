using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
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
            await RespondAsync(context, ex);
        }
    }

    private async Task RespondAsync(HttpContext context, Exception ex)
    {
        (int status, string code, string detail, IReadOnlyDictionary<string, string[]> errors) = ex switch
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
            logger.LogWarning("Erro do cliente {Code} em {Method} {Path}: {Detail}",
                code, context.Request.Method, context.Request.Path, detail);
        }

        ProblemDetails problem = new()
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}
