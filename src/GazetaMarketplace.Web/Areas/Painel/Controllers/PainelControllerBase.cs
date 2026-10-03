using GazetaMarketplace.Web.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Areas.Painel.Controllers;

/// <summary>
/// Base de toda página do painel: exige login com papel de equipe (NFR-13) e nunca vai para o cache,
/// para que o botão Voltar depois de "Sair" não mostre o painel (S03).
/// </summary>
[Area("Painel")]
[Authorize(Policy = PoliticasDeAcesso.Redator)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public abstract class PainelControllerBase : Controller
{
}
