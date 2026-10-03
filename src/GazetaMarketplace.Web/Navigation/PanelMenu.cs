using System.Collections.Generic;
using System.Security.Claims;
using GazetaMarketplace.Core.Team;
using Microsoft.AspNetCore.Http;

namespace GazetaMarketplace.Web.Navigation;

/// <summary>Um item do menu do painel.</summary>
public sealed record MenuItem(string Text, string Path, bool Active);

/// <summary>Itens do menu do painel por papel (wireframe US-006): o Redator tem 1, o Administrador tem 4.</summary>
public static class PanelMenu
{
    public static IReadOnlyList<MenuItem> Items(ClaimsPrincipal user, PathString currentPath)
    {
        if (user.IsInRole(RoleNames.Administrator))
        {
            return
            [
                Item("Anúncios", PanelRoutes.Ads, currentPath),
                Item("Categorias", PanelRoutes.Categories, currentPath),
                Item("Usuários", PanelRoutes.Users, currentPath),
                Item("Configurações", PanelRoutes.Settings, currentPath)
            ];
        }

        if (user.IsInRole(RoleNames.Writer))
        {
            return [Item("Meus anúncios", PanelRoutes.Ads, currentPath)];
        }

        return [];
    }

    private static MenuItem Item(string text, string path, PathString currentPath) =>
        new(text, path, currentPath.StartsWithSegments(path, System.StringComparison.OrdinalIgnoreCase));
}
