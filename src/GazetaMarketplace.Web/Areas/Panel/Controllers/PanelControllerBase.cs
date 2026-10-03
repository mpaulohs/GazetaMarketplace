using GazetaMarketplace.Web.Areas.Panel.Filters;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Areas.Panel.Controllers;

/// <summary>
/// Base de toda página do painel: exige login com papel de equipe (NFR-13), leva à troca de senha enquanto a senha
/// for provisória (S09) e nunca vai para o cache, para que o botão Voltar depois de "Sair" não mostre o painel (S03).
/// </summary>
[Area("Panel")]
[Authorize(Policy = AccessPolicies.Writer)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[TypeFilter(typeof(MustChangePasswordFilter))]
public abstract class PanelControllerBase : Controller
{
}
