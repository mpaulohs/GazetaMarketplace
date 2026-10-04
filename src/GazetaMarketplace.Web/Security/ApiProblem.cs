using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace GazetaMarketplace.Web.Security;

/// <summary>Resposta de erro dos endpoints JSON escrita fora do pipeline MVC (o cookie e o limitador não passam pelo filtro de exceção): ProblemDetails com <c>code</c> e <c>traceId</c>.</summary>
internal static class ApiProblem
{
    public static async Task WriteAsync(HttpContext http, int status, string code, string detail)
    {
        ProblemDetails problem = new()
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = detail,
            Instance = http.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = http.TraceIdentifier;

        http.Response.StatusCode = status;
        await http.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}
