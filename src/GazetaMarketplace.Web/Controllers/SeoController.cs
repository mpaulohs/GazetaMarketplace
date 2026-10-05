using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Seo;
using GazetaMarketplace.Web.Navigation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Controllers;

/// <summary>
/// <c>/robots.txt</c> e <c>/sitemap.xml</c> (NFR-21). O mapa lista o início, as categorias com anúncio publicado e todos os anúncios publicados (um só arquivo, até 50.000 endereços); o anúncio
/// arquivado ou despublicado sai dele na hora (sem cache). Os endereços usam <c>Site:BaseUrl</c> (em Production é obrigatório), nunca o cabeçalho Host.
/// </summary>
public sealed class SeoController(ISitemap sitemap, IOptions<SiteOptions> site, ILogger<SeoController> logger) : Controller
{
    [HttpGet("robots.txt")]
    public ContentResult Robots() => Content(RobotsText.Build(BaseUrl()), "text/plain; charset=utf-8");

    [HttpGet("sitemap.xml")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        IReadOnlyList<SitemapEntry> entries;
        try
        {
            entries = await sitemap.EntriesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao montar o mapa do site (traceId {TraceId})", HttpContext.TraceIdentifier);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return Content(SitemapDocument.Build(BaseUrl(), entries), "application/xml; charset=utf-8");
    }

    private string BaseUrl() => PublicUrl.Absolute(Request, site.Value, string.Empty);
}
