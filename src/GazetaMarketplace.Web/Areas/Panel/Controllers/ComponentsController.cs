using System.Collections.Generic;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Web.Areas.Panel.Models;
using GazetaMarketplace.Web.Models;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Areas.Panel.Controllers;

/// <summary>
/// Página de componentes (tarefa 3.8): mostra lado a lado os três cards e o corpo do anúncio com dados de exemplo, para conferir o visual sem ter anúncios
/// publicados. <b>Só em Development</b> (em Production é 404, sem rota pública) e só para o Administrador.
/// </summary>
[DevelopmentOnly]
[Authorize(Policy = AccessPolicies.Administrator)]
[Route("painel/componentes")]
public sealed class ComponentsController : PanelControllerBase
{
    private const string SampleCover = "/images/componentes/capa-exemplo.svg";

    [HttpGet("")]
    public IActionResult Index() => View(new ComponentsViewModel
    {
        Cards =
        [
            new AdCardModel(1, "Honda Civic 2018 automático, único dono, revisões na concessionária", FieldGroupKeys.Cars, 6_200_000, null, null, new AdCardCover(1, 480, 360, SampleCover), "Campinas", "SP", "#padrao"),
            new AdCardModel(2, "Diarista com experiência, atendo toda a região", FieldGroupKeys.Services, null, "Serviços domésticos", null, new AdCardCover(2, 480, 360, SampleCover), "São Paulo", "SP", "#servicos"),
            new AdCardModel(3, "Pizzaiolo com experiência, período integral", FieldGroupKeys.Jobs, 280_000, null, "Alimentação e restaurantes", null, "Campinas", "SP", "#vagas"),
            new AdCardModel(4, "Violão Giannini, com capa, cordas novas", FieldGroupKeys.GeneralProducts, 249_990, null, null, null, "Goiânia", "GO", "#sem-foto")
        ],
        Body = new AdBodyViewModel
        {
            Title = "Honda Civic 2018 automático",
            Value = AdPresentation.ValueOf(FieldGroupRegistry.Get(FieldGroupKeys.Cars), 6_200_000, null),
            Location = AdPresentation.Location("Campinas", "SP"),
            Description = "Único dono, revisões em dia.\nAceito troca por veículo de menor valor.",
            Specs = new List<AdSpec> { new("Marca", "Honda"), new("Modelo", "Civic"), new("Ano", "2018"), new("Quilometragem", "45.000 km") },
            HeadingLevel = 3
        }
    });
}
