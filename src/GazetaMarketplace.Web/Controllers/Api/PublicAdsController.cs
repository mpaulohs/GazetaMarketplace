using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Controllers.Api;

/// <summary>
/// <c>GET /api/v1/ads?ids=12,57,104</c> (<c>listAdsByIds</c>, US-005): os cards dos anúncios <b>publicados</b> entre os ids pedidos, <b>na ordem pedida</b>, no envelope <c>PagedResult</c>.
/// Público. Id de anúncio despublicado, arquivado ou inexistente simplesmente não volta (a página dos favoritos tira esses ids da lista do visitante). De 1 a 100 ids numéricos; fora disso, 400.
/// Nunca devolve dado do autor. A resposta não fica em cache: a lista muda quando a equipe despublica um anúncio.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/ads")]
[Produces("application/json")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PublicAdsController(IShowcase showcase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdCardDto>>> List([FromQuery] string ids, CancellationToken cancellationToken)
    {
        if (!FavoriteIds.TryParse(ids, out int[] parsed))
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["ids"] = [FavoriteIds.InvalidMessage] });
        }

        IReadOnlyList<ShowcaseAdCard> cards = await showcase.CardsByIdsAsync(parsed, cancellationToken);
        return Ok(new PagedResult<AdCardDto>
        {
            Items = [.. cards.Select(AdCardDto.From)],
            Page = 1,
            PageSize = parsed.Length,
            TotalCount = cards.Count
        });
    }
}
