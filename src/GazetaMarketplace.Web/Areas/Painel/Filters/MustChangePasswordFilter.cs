using GazetaMarketplace.Web.Navegacao;
using GazetaMarketplace.Web.Seguranca;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GazetaMarketplace.Web.Areas.Painel.Filters;

/// <summary>
/// Enquanto a senha for provisória (S6), toda página do painel leva a "Defina sua nova senha". Lê o claim do cookie
/// em vez de ir ao banco a cada página; o claim é refeito quando a senha é trocada.
/// </summary>
public sealed class MustChangePasswordFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.User.HasClaim(ClaimsDaEquipe.DeveTrocarSenha, "1"))
        {
            context.Result = new RedirectResult(RotasDoPainel.DefinirSenha);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
