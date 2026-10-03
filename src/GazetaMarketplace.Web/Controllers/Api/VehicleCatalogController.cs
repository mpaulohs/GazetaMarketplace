using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.VehicleCatalog;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Controllers.Api;

/// <summary>
/// Listas encadeadas do catálogo de veículos (ADR-008) para o formulário do anúncio e para os filtros da busca. Públicas e cacheáveis por 10 minutos.
/// Carros e motos têm ids próprios que podem coincidir, então todas as consultas pedem o tipo (<c>kind=car</c> ou <c>kind=moto</c>).
/// </summary>
[ApiController]
[Route("api/v1/vehicle-catalog")]
[Produces("application/json")]
[ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
public sealed class VehicleCatalogController(IVehicleCatalog catalog) : ControllerBase
{
    private const int MinYear = 1950;
    private const int MaxYear = 2100;

    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<CatalogItem>>> Brands([FromQuery] string kind, CancellationToken cancellationToken)
    {
        RequireKind(kind);
        return Ok(await catalog.BrandsAsync(kind, cancellationToken));
    }

    [HttpGet("brands/{brandId:int:min(1)}/models")]
    public async Task<ActionResult<IReadOnlyList<CatalogItem>>> Models(int brandId, [FromQuery] string kind, CancellationToken cancellationToken)
    {
        RequireKind(kind);
        return Ok(await catalog.ModelsAsync(kind, brandId, cancellationToken));
    }

    [HttpGet("models/{modelId:int:min(1)}/years")]
    public async Task<ActionResult<IReadOnlyList<int>>> Years(int modelId, [FromQuery] string kind, CancellationToken cancellationToken)
    {
        RequireKind(kind);
        return Ok(await catalog.YearsAsync(kind, modelId, cancellationToken));
    }

    [HttpGet("models/{modelId:int:min(1)}/years/{year:int}/versions")]
    public async Task<ActionResult<IReadOnlyList<CatalogItem>>> Versions(int modelId, int year, [FromQuery] string kind, CancellationToken cancellationToken)
    {
        RequireKind(kind);
        if (year is < MinYear or > MaxYear)
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["year"] = [$"O ano deve estar entre {MinYear} e {MaxYear}."] });
        }

        return Ok(await catalog.VersionsAsync(kind, modelId, year, cancellationToken));
    }

    private static void RequireKind(string kind)
    {
        if (!VehicleKinds.IsValid(kind))
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["kind"] = ["Informe o tipo: car ou moto."] });
        }
    }
}
