using System;
using System.Collections.Generic;
using GazetaMarketplace.Core.Excecoes;
using Microsoft.AspNetCore.Mvc;
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
    [HttpGet("pagina-erro")]
    public IActionResult Erro() => throw new InvalidOperationException("segredo-interno-123");
}
