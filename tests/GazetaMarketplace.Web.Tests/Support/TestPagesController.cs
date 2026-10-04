using System;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Páginas HTML só de teste: exercitam os layouts e os partials sem depender de telas de produto.</summary>
public sealed class TestPagesController : Controller
{
    [HttpGet("teste/publica")]
    public IActionResult PublicPage() => View();

    [HttpGet("teste/painel")]
    public IActionResult Panel() => View();

    [HttpGet("teste/estados")]
    public IActionResult States() => View();

    [HttpGet("teste/estados-hostis")]
    public IActionResult HostileStates() => View();

    /// <summary>Um card isolado (3.8), montado a partir da query string, sem layout.</summary>
    [HttpGet("teste/card")]
    public IActionResult Card(
        string title = "Anúncio", string group = FieldGroupKeys.GeneralProducts, long? price = null, string service = null, string area = null, bool cover = false,
        string city = null, string uf = null, bool eager = false) =>
        ViewComponent("AdCard", new
        {
            ad = new AdCardModel(7, title, group, price, service, area, cover ? AdCardCover.FromStored(9, 1600, 1200) : null, city, uf, "/anuncio/7"),
            eagerImage = eager
        });

    /// <summary>O corpo do anúncio (3.8) com o título e a descrição da query string, para provar a codificação do texto.</summary>
    [HttpGet("teste/corpo")]
    public IActionResult Body(string title = "Título", string description = "Descrição", int level = 1) =>
        PartialView("_AdBody", new AdBodyViewModel { Title = title, Description = description, HeadingLevel = level, Location = "Campinas/SP", Specs = [new AdSpec("Ano", "2018")] });

    [HttpGet("teste/erro")]
    public IActionResult Error() => throw new InvalidOperationException("falha de teste");
}
