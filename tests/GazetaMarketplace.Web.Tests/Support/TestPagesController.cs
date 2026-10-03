using System;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Páginas HTML só de teste: exercitam os layouts e os partials sem depender de telas de produto.</summary>
public sealed class TestPagesController : Controller
{
    [HttpGet("teste/publica")]
    public IActionResult PublicPage() => View();

    [HttpGet("teste/painel")]
    public IActionResult Painel() => View();

    [HttpGet("teste/estados")]
    public IActionResult States() => View();

    [HttpGet("teste/estados-hostis")]
    public IActionResult HostileStates() => View();

    [HttpGet("teste/erro")]
    public IActionResult Error() => throw new InvalidOperationException("falha de teste");
}
