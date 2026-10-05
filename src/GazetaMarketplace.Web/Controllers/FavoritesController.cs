using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Showcase;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Controllers;

/// <summary>
/// "Meus favoritos" (US-005). Os favoritos moram <b>só no navegador</b> do visitante (<c>localStorage</c>, suposição S1): o servidor não os guarda e a página <c>/favoritos</c> é só a moldura (título,
/// o aviso permanente e as regiões que o <c>favorites.js</c> preenche). Quem lê o <c>localStorage</c> pede os cards a <c>/favoritos/lista?ids=…</c>, que devolve um <b>fragmento de HTML</b> desenhado pelo mesmo
/// <c>AdCard</c> das demais listas (um só desenho de card), com só os anúncios publicados e na ordem pedida; a ausência de um id é o sinal de que o anúncio saiu do ar.
/// A página não é indexada (<c>noindex, follow</c>); o SEO é a tarefa 5.6.
/// </summary>
public sealed class FavoritesController(IShowcase showcase) : Controller
{
    [HttpGet("favoritos")]
    public IActionResult Index() => View();

    [HttpGet("favoritos/lista")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> List([FromQuery] string ids, CancellationToken cancellationToken)
    {
        if (!FavoriteIds.TryParse(ids, out int[] parsed))
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["ids"] = [FavoriteIds.InvalidMessage] });
        }

        return View(await showcase.CardsByIdsAsync(parsed, cancellationToken));
    }
}
