using System;
using System.Collections.Generic;
using GazetaMarketplace.Core.Excecoes;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Tests.Suporte;

/// <summary>Endpoints JSON só de teste: lançam cada exceção do contrato de erros.</summary>
[ApiController]
[Route("api/v1/teste")]
public sealed class ApiTesteController(ILogger<ApiTesteController> logger) : ControllerBase
{
    [HttpGet("log")]
    public IActionResult Registrar()
    {
        logger.LogInformation("Linha de teste");
        return Ok();
    }

    [HttpGet("token")]
    public IActionResult Token([FromServices] IAntiforgery antiforgery) =>
        Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpPost("escrita")]
    public IActionResult Escrita() => Ok();

    [HttpGet("auth")]
    [EnableRateLimiting("auth")]
    public IActionResult Autenticacao() => Ok();

    [HttpGet("cultura")]
    public IActionResult Cultura() => Ok(new
    {
        cultura = CultureInfo.CurrentCulture.Name,
        ui = CultureInfo.CurrentUICulture.Name,
        numero = 1234.5m.ToString(CultureInfo.CurrentCulture)
    });

    [HttpGet("ip")]
    public IActionResult Ip() => Ok(new { ip = HttpContext.Connection.RemoteIpAddress?.ToString() });

    [HttpPost("corpo")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Corpo()
    {
        using MemoryStream copia = new();
        await Request.Body.CopyToAsync(copia);
        return Ok(new { bytes = copia.Length });
    }

    [HttpPost("foto")]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Foto()
    {
        using MemoryStream copia = new();
        await Request.Body.CopyToAsync(copia);
        return Ok(new { bytes = copia.Length });
    }

    [HttpGet("erro/{tipo}")]
    public IActionResult Erro(string tipo)
    {
        throw tipo switch
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
public sealed class PaginaTesteController : Controller
{
    [HttpPost("formulario")]
    public IActionResult Formulario() => Content("ok");

    [HttpGet("pagina-erro")]
    public IActionResult Erro() => throw new InvalidOperationException("segredo-interno-123");
}
