using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Web.Models;
using GazetaMarketplace.Web.Navigation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Controllers
{
    public class HomeController(IShowcase showcase, ILogger<HomeController> logger) : Controller
    {
        /// <summary>A página inicial (US-001): categorias principais e os 12 anúncios publicados mais recentes. Qualquer falha vira a página de erro com "Tentar novamente".</summary>
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            ShowcaseHome home;
            try
            {
                home = await showcase.HomeAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Falha ao montar a página (US-001-S06): a causa fica no log com o código de referência; a tela não mostra detalhe técnico
                logger.LogError(ex, "Falha ao carregar a página inicial (traceId {TraceId})", HttpContext.TraceIdentifier);
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return View("LoadError", new PageStateViewModel
                {
                    Title = "Não foi possível carregar a página. Tente novamente.",
                    ActionUrl = PublicRoutes.Home,
                    ReferenceCode = HttpContext.TraceIdentifier
                });
            }

            return View(home);
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
