using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Areas.Panel.Controllers;

/// <summary>
/// PROVISÓRIO: páginas mínimas para as telas de entrada terem aonde chegar (US-006-S01, S02 e S07).
/// "Meus anúncios" é trocada na tarefa 4.4 e a "Fila de revisão" na 4.1.
/// </summary>
[Route("painel/anuncios")]
public sealed class AdsController : PanelControllerBase
{
    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("fila")]
    [Authorize(Policy = AccessPolicies.Administrator)]
    public IActionResult ReviewQueue() => View();
}
