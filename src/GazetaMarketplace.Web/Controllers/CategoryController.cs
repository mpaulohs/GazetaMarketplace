using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Seo;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Web.Models;
using GazetaMarketplace.Web.Navigation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Controllers;

/// <summary>
/// A página de uma categoria (US-001-S02 a S04 e S07): subcategorias, caminho de navegação e os anúncios publicados dela e de todas as descendentes, 24 por página.
/// O endereço é <c>/categoria/{slug}</c>; um slug que não existe (inclusive de categoria excluída) dá 404 com "Categoria não encontrada" e os caminhos de volta.
/// </summary>
[Route("categoria")]
public sealed class CategoryController(IShowcase showcase, IOptions<SiteOptions> site, ILogger<CategoryController> logger) : Controller
{
    [HttpGet("{slug}")]
    public async Task<IActionResult> Index(string slug, [FromQuery(Name = "pagina")] int page, CancellationToken cancellationToken)
    {
        ShowcaseCategoryPage result;
        try
        {
            result = await showcase.CategoryAsync(slug, page, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao carregar a página da categoria (traceId {TraceId})", HttpContext.TraceIdentifier);
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View("~/Views/Home/LoadError.cshtml", new PageStateViewModel
            {
                Title = "Não foi possível carregar a página. Tente novamente.",
                ActionUrl = Request.Path + Request.QueryString,
                ReferenceCode = HttpContext.TraceIdentifier
            });
        }

        if (!result.Found)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound", result);
        }

        // Canônico: sem parâmetros, só a página quando passa da primeira (cada página da lista é uma página própria)
        string path = PublicRoutes.Category(result.Category.Slug) + (result.Page > 1 ? "?pagina=" + result.Page : string.Empty);
        ViewData[SeoModel.ViewDataKey] = SeoModel.Indexable(SeoTexts.CategoryDescription(result.Category.Name, result.Path.Count > 1 ? result.Path[^2].Name : null), PublicUrl.Absolute(Request, site.Value, path));
        return View(result);
    }
}
