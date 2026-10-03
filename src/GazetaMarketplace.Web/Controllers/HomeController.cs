using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Web.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Erro esperado lançado numa página mantém o seu status (404, 403...); o resto fica 500
            IExceptionHandlerPathFeature error = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (error?.Error is AppException expected)
            {
                Response.StatusCode = expected.StatusCode;
            }

            // "Tentar novamente" volta à página que falhou; fora de uma falha, vai para a página inicial
            string source = error is null ? "/" : error.Path + HttpContext.Request.QueryString;

            // O código de referência é o CorrelationId (HttpContext.TraceIdentifier), o mesmo traceId do log
            return View(new PageStateViewModel
            {
                Title = "Algo deu errado",
                Message = "Não foi possível concluir o que você pediu. Tente de novo em alguns instantes.",
                ActionUrl = source,
                ReferenceCode = HttpContext.TraceIdentifier
            });
        }
    }
}
