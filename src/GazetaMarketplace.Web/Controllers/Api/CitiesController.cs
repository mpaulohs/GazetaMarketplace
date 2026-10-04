using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Controllers.Api;

/// <summary>
/// Municípios de uma UF para o preenchimento manual de cidade (US-008-S14, <c>listCities</c>). Só para a equipe; a lista não muda de um dia para o
/// outro, então o navegador a guarda por 10 minutos (cache privado: o conteúdo é de área logada). Lista vazia = a UF ainda não tem carga do IBGE e a tela
/// mostra um campo de texto (a regra de padronização da SPEC vale).
/// </summary>
[ApiController]
[Route("api/v1/cities")]
[Produces("application/json")]
[Authorize(Policy = AccessPolicies.Writer)]
[ResponseCache(Duration = 600, Location = ResponseCacheLocation.Client)]
public sealed class CitiesController(ICityDirectory directory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CityItem>>> List([FromQuery] string uf, CancellationToken cancellationToken) =>
        Ok(await directory.ByUfAsync(uf, cancellationToken));
}
