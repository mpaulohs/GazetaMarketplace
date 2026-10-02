using System.Collections.Generic;
using System.Security.Claims;
using GazetaMarketplace.Core.Equipe;
using Microsoft.AspNetCore.Http;

namespace GazetaMarketplace.Web.Navegacao;

/// <summary>Um item do menu do painel.</summary>
public sealed record ItemDeMenu(string Texto, string Caminho, bool Ativo);

/// <summary>Itens do menu do painel por papel (wireframe US-006): o Redator tem 1, o Administrador tem 4.</summary>
public static class MenuDoPainel
{
    public static IReadOnlyList<ItemDeMenu> Itens(ClaimsPrincipal usuario, PathString caminhoAtual)
    {
        if (usuario.IsInRole(Papeis.Administrador))
        {
            return
            [
                Item("Anúncios", RotasDoPainel.Anuncios, caminhoAtual),
                Item("Categorias", RotasDoPainel.Categorias, caminhoAtual),
                Item("Usuários", RotasDoPainel.Usuarios, caminhoAtual),
                Item("Configurações", RotasDoPainel.Configuracoes, caminhoAtual)
            ];
        }

        if (usuario.IsInRole(Papeis.Redator))
        {
            return [Item("Meus anúncios", RotasDoPainel.Anuncios, caminhoAtual)];
        }

        return [];
    }

    private static ItemDeMenu Item(string texto, string caminho, PathString caminhoAtual) =>
        new(texto, caminho, caminhoAtual.StartsWithSegments(caminho, System.StringComparison.OrdinalIgnoreCase));
}
