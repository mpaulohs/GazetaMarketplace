using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Search;
using GazetaMarketplace.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Controllers;

/// <summary>
/// A busca pública (US-002), em <c>/busca</c>. Os filtros ficam no endereço (S09), então a página reabre igual em outra aba. Parâmetro inválido volta ao padrão sem erro; só a faixa invertida, o valor
/// ilegível e o termo longo demais mostram mensagem junto do campo. Falha na consulta dá 503 com os filtros preservados, "Tentar novamente" e o código de referência (S11). A página não é indexada
/// (<c>noindex, follow</c>): o SEO das páginas públicas é a tarefa 5.6.
/// </summary>
[Route("busca")]
public sealed class SearchController(ISearch search, ILogger<SearchController> logger) : Controller
{
    private const string FailureMessage = "Não foi possível buscar agora. Tente novamente.";

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] SearchQueryModel query, CancellationToken cancellationToken)
    {
        string selfUrl = Request.Path + Request.QueryString;
        SearchForm form;
        try
        {
            form = await search.PrepareAsync(query.ToInput(), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Nem o formulário saiu (as listas de categorias, cidades ou catálogo falharam): sem filtros para preservar, só a mensagem
            logger.LogError(ex, "Falha ao preparar a busca (traceId {TraceId})", HttpContext.TraceIdentifier);
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View("~/Views/Home/LoadError.cshtml", new PageStateViewModel { Title = FailureMessage, ActionUrl = selfUrl, ReferenceCode = HttpContext.TraceIdentifier });
        }

        try
        {
            SearchResult result = await search.RunAsync(form, cancellationToken);
            return View(new SearchPageViewModel { Form = form, Result = result, SelfUrl = selfUrl });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao buscar anúncios (traceId {TraceId})", HttpContext.TraceIdentifier);
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(new SearchPageViewModel { Form = form, FailureCode = HttpContext.TraceIdentifier, SelfUrl = selfUrl });
        }
    }
}
