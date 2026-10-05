using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Web.Models;
using GazetaMarketplace.Web.Navigation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Controllers;

/// <summary>
/// A página pública do anúncio (US-003), em <c>/anuncio/{id}/{slug}</c>. O <b>id</b> manda: slug errado ou faltando leva (301) ao endereço atual, então um título editado não quebra
/// o link salvo. Só anúncio Publicado aparece; qualquer outra situação, ou um id que não existe, dá <b>404 com a mesma mensagem</b> (nunca 410, que diria que o anúncio existiu).
/// </summary>
[Route("anuncio")]
public sealed class AdController(IShowcase showcase, AdDetailFactory factory, ISiteSettings settings, IOptions<SiteOptions> site, ILogger<AdController> logger) : Controller
{
    [HttpGet("{id:int}/{slug?}")]
    public async Task<IActionResult> Index(int id, string slug, CancellationToken cancellationToken)
    {
        try
        {
            ShowcaseAdPage page = await showcase.AdAsync(id, cancellationToken);
            if (!page.Available)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return View("Unavailable", new AdUnavailableViewModel { Roots = page.Roots, ArchivedCategory = page.ArchivedCategory });
            }

            Ad ad = page.Ad;
            string canonicalPath = AdRoutes.Detail(ad.Id, ad.Title);
            if (!string.Equals(Request.Path.Value, canonicalPath, StringComparison.Ordinal))
            {
                return RedirectPermanent(canonicalPath + Request.QueryString);
            }

            AdDetail detail = await factory.CreateAsync(ad, headingLevel: 1, includePublicMeta: true, cancellationToken);
            string pageUrl = PublicUrl.Absolute(Request, site.Value, canonicalPath);
            return View(new AdPageViewModel
            {
                Detail = detail,
                CanonicalUrl = pageUrl,
                Contact = ContactViewModel.Create(await settings.GetPhoneAsync(cancellationToken), ad.Title, pageUrl),
                MetaDescription = AdMetaDescription.For(ad.Title, ad.Description, detail.Body.Location)
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Falha ao montar a página: a causa fica no log com o código de referência; a tela não mostra detalhe técnico
            logger.LogError(ex, "Falha ao carregar o anúncio {AdId} (traceId {TraceId})", id, HttpContext.TraceIdentifier);
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View("~/Views/Home/LoadError.cshtml", new PageStateViewModel
            {
                Title = "Não foi possível carregar o anúncio. Tente novamente.",
                ActionUrl = Request.Path + Request.QueryString,
                ReferenceCode = HttpContext.TraceIdentifier
            });
        }
    }
}
