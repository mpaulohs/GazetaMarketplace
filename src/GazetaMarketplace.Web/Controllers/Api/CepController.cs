using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GazetaMarketplace.Web.Controllers.Api;

/// <summary>
/// Cidade e UF de um CEP (ADR-007, <c>getCep</c>). Só para a equipe logada, para o site não virar proxy público de CEP; 30 consultas por minuto por usuário.
/// Respostas: 200 · 400 <c>VALIDATION_ERROR</c> (menos de 8 dígitos, nada é consultado) · 404 <c>NOT_FOUND</c> (CEP que não existe: não abre o preenchimento manual)
/// · 503 <c>CEP_SERVICE_UNAVAILABLE</c> (a tela tenta de novo uma vez). A resposta nunca traz rua nem bairro.
/// </summary>
[ApiController]
[Route("api/v1/cep")]
[Produces("application/json")]
[Authorize(Policy = AccessPolicies.Writer)]
[EnableRateLimiting(RateLimitingExtensions.CepPolicy)]
public sealed class CepController(ICepService cepService) : ControllerBase
{
    [HttpGet("{cep}")]
    public async Task<ActionResult<CepResponse>> Get(string cep, CancellationToken cancellationToken)
    {
        CepResult result = await cepService.GetAsync(cep, cancellationToken);
        return Ok(new CepResponse(result.Cep, result.City, result.Uf, result.Source));
    }

    /// <summary>O corpo do 200: o contrato <c>CepResult</c> do <c>openapi.yaml</c>.</summary>
    public sealed record CepResponse(string Cep, string City, string Uf, string Source);
}
