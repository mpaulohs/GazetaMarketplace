using GazetaMarketplace.Core.Excecoes;
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Erro esperado lançado numa página mantém o seu status (404, 403...); o resto fica 500
            IExceptionHandlerFeature erro = HttpContext.Features.Get<IExceptionHandlerFeature>();
            if (erro?.Error is AppException esperado)
            {
                Response.StatusCode = esperado.StatusCode;
            }

            // O código de referência é o CorrelationId (HttpContext.TraceIdentifier), o mesmo traceId do log
            return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }
}
