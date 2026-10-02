using System.Collections.Generic;
using GazetaMarketplace.Web.Navegacao;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.ViewComponents;

/// <summary>Menu do painel conforme o papel de quem está logado.</summary>
public sealed class MenuPainelViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        IReadOnlyList<ItemDeMenu> itens = MenuDoPainel.Itens(UserClaimsPrincipal, Request.Path);
        return View(itens);
    }
}
