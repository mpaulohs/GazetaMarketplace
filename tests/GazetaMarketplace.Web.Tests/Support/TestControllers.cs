using System;
using System.Collections.Generic;
using GazetaMarketplace.Core.Exceptions;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Endpoints JSON só de teste: lançam cada exceção do contrato de erros.</summary>
[ApiController]
[Route("api/v1/teste")]
public sealed class TestApiController(ILogger<TestApiController> logger) : ControllerBase
{
    [HttpGet("log")]
    public IActionResult Record()
    {
        logger.LogInformation("Linha de teste");
        return Ok();
    }

    [HttpGet("token")]
    public IActionResult Token([FromServices] IAntiforgery antiforgery) =>
        Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpPost("escrita")]
    public IActionResult Write() => Ok();

    [HttpGet("auth")]
    [EnableRateLimiting("auth")]
    public IActionResult Authentication() => Ok();

    [HttpGet("cultura")]
    public IActionResult Culture() => Ok(new
    {
        culture = CultureInfo.CurrentCulture.Name,
        ui = CultureInfo.CurrentUICulture.Name,
        number = 1234.5m.ToString(CultureInfo.CurrentCulture)
    });

    [HttpGet("ip")]
    public IActionResult Ip() => Ok(new { ip = HttpContext.Connection.RemoteIpAddress?.ToString() });

    [HttpPost("corpo")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Body()
    {
        using MemoryStream copy = new();
        await Request.Body.CopyToAsync(copy);
        return Ok(new { bytes = copy.Length });
    }

    [HttpPost("foto")]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Photo()
    {
        using MemoryStream copy = new();
        await Request.Body.CopyToAsync(copy);
        return Ok(new { bytes = copy.Length });
    }

    [HttpGet("erro/{type}")]
    public IActionResult Error(string type)
    {
        throw type switch
        {
            "notfound" => new NotFoundException("Anúncio", 42),
            "conflict" => new ConflictException("Nome de categoria repetido"),
            "validation" => new ValidationException(new Dictionary<string, string[]> { ["email"] = ["E-mail inválido"] }),
            "forbidden" => new ForbiddenException(),
            "unauthorized" => new UnauthorizedException(),
            "cep" => new ServiceUnavailableException("ViaCEP sem resposta"),
            _ => new InvalidOperationException("segredo-interno-123")
        };
    }
}

/// <summary>Página só de teste: lança um erro inesperado numa rota de página.</summary>
[Route("teste")]
public sealed class TestPageController : Controller
{
    [HttpPost("formulario")]
    public IActionResult Form() => Content("ok");

    [HttpGet("pagina-erro")]
    public IActionResult Error() => throw new InvalidOperationException("segredo-interno-123");
}
