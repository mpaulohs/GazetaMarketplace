using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Location;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Controllers.Api;

/// <summary>
/// Municípios de uma UF para o filtro de cidade da busca (US-002-S10): a mesma lista do IBGE da <see cref="CitiesController"/>, mas <b>pública</b> (o visitante não tem conta) e com
/// cache de 10 minutos que pode ser compartilhado. Lista vazia = a UF ainda não tem carga do IBGE e a busca só mostra "Todas as cidades". UF inválida dá 400.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/public/cities")]
[Produces("application/json")]
[ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
public sealed class PublicCitiesController(ICityDirectory directory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CityItem>>> List([FromQuery] string uf, CancellationToken cancellationToken) =>
        Ok(await directory.ByUfAsync(uf, cancellationToken));
}
