using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Seo;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Web.Models;
using GazetaMarketplace.Web.Navigation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Controllers
{
    public class HomeController(IShowcase showcase, IOptions<SiteOptions> site, ILogger<HomeController> logger) : Controller
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

            ViewData[SeoModel.ViewDataKey] = SeoModel.Indexable(SeoTexts.HomeDescription, PublicUrl.Absolute(Request, site.Value, PublicRoutes.Home));
            return View(home);
        }

        /// <summary>
        /// A página de uma resposta de erro que saiu sem corpo (endereço que não existe, ação que devolveu <c>NotFound()</c>): o <c>UseStatusCodePagesWithReExecute</c> a chama com o código no endereço
        /// e o status da resposta continua o mesmo (404 segue 404, para os buscadores e para os testes).
        /// </summary>
        // Alvo de reexecução: o pedido volta com o método original (um POST recusado por falta de token chega aqui como POST). A página só mostra uma mensagem e não muda nada;
        // exigir o token de novo deixaria a resposta em branco (R-06)
        [IgnoreAntiforgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Status(int id)
        {
            int code = id is >= 400 and <= 599 ? id : StatusCodes.Status404NotFound;
            Response.StatusCode = code;
            ViewData["Title"] = code == StatusCodes.Status404NotFound ? "Página não encontrada" : "Algo deu errado";
            return View(new PageStateViewModel
            {
                Title = code == StatusCodes.Status404NotFound ? "Página não encontrada" : "Algo deu errado",
                Message = code == StatusCodes.Status404NotFound
                    ? "O endereço não existe ou foi alterado. Veja os anúncios na página inicial ou faça uma busca."
                    : "Não foi possível concluir o que você pediu. Tente de novo em alguns instantes.",
                ActionText = "Ir para a página inicial",
                ActionUrl = PublicRoutes.Home,
                ReferenceCode = code == StatusCodes.Status404NotFound ? null : HttpContext.TraceIdentifier
            });
        }

        // Alvo do UseExceptionHandler: um POST que falha reexecuta esta ação como POST; sem isto, o filtro de antiforgery deixaria a resposta em branco (R-06)
        [IgnoreAntiforgeryToken]
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
