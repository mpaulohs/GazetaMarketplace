using System;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Tests.Suporte;

/// <summary>Páginas HTML só de teste: exercitam os layouts e os partials sem depender de telas de produto.</summary>
public sealed class PaginasDeTesteController : Controller
{
    [HttpGet("teste/publica")]
    public IActionResult Publica() => View();

    [HttpGet("teste/painel")]
    public IActionResult Painel() => View();

    [HttpGet("teste/estados")]
    public IActionResult Estados() => View();

    [HttpGet("teste/estados-hostis")]
    public IActionResult EstadosHostis() => View();

    [HttpGet("teste/erro")]
    public IActionResult Erro() => throw new InvalidOperationException("falha de teste");
}
