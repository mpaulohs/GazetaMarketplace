using System.Collections.Generic;
using GazetaMarketplace.Web.Navigation;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.ViewComponents;

/// <summary>Menu do painel conforme o papel de quem está logado.</summary>
public sealed class PanelMenuViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        IReadOnlyList<MenuItem> items = PanelMenu.Items(UserClaimsPrincipal, Request.Path);
        return View(items);
    }
}
