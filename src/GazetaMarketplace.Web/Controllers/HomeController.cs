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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Erro esperado lançado numa página mantém o seu status (404, 403...); o resto fica 500
            IExceptionHandlerPathFeature erro = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (erro?.Error is AppException esperado)
            {
                Response.StatusCode = esperado.StatusCode;
            }

            // "Tentar novamente" volta à página que falhou; fora de uma falha, vai para a página inicial
            string origem = erro is null ? "/" : erro.Path + HttpContext.Request.QueryString;

            // O código de referência é o CorrelationId (HttpContext.TraceIdentifier), o mesmo traceId do log
            return View(new EstadoPaginaViewModel
            {
                Titulo = "Algo deu errado",
                Mensagem = "Não foi possível concluir o que você pediu. Tente de novo em alguns instantes.",
                AcaoUrl = origem,
                CodigoReferencia = HttpContext.TraceIdentifier
            });
        }
    }
}
