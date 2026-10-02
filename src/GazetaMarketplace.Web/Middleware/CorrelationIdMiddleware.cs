using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace GazetaMarketplace.Web.Middleware;

/// <summary>
/// Lê ou cria o X-Correlation-ID, devolve-o na resposta e o põe em cada linha de log.
/// O mesmo valor é o traceId do ProblemDetails e o código de referência das telas de erro (ADR-010).
/// </summary>
public sealed partial class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string Cabecalho = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        string id = context.Request.Headers[Cabecalho].FirstOrDefault();

        // Só aceita um formato seguro: um valor livre do cliente poderia forjar linhas no log
        if (id is null || !IdValido().IsMatch(id))
        {
            id = Guid.NewGuid().ToString("N");
        }

        context.TraceIdentifier = id;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[Cabecalho] = id;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", id))
        {
            await next(context);
        }
    }

    [GeneratedRegex(@"^[A-Za-z0-9_\-]{1,64}$")]
    private static partial Regex IdValido();
}
