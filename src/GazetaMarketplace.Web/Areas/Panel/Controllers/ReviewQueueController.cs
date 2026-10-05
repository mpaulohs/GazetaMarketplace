using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Web.Areas.Panel.Models;
using GazetaMarketplace.Web.Models;
using GazetaMarketplace.Web.Navigation;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Areas.Panel.Controllers;

/// <summary>
/// A fila de revisão e a pré-visualização do anúncio (US-010-S01, S02, S06 e S09). Só o Administrador entra; o Redator cai em "acesso negado" e nunca vê botão de decisão.
/// Esta tela só <b>lê</b>: publicar e rejeitar chegam na tarefa 4.2 e arquivar, na 4.3, então hoje a única ação é "Editar". Quem pode ver cada anúncio continua sendo
/// decidido no servidor pelo <see cref="IAdService"/>.
/// </summary>
[Authorize(Policy = AccessPolicies.Administrator)]
[Route("painel/anuncios")]
public sealed class ReviewQueueController(
    IReviewQueue queue,
    IAdService ads,
    IAdPhotoService photos,
    IAdSpecsReader specs,
    ICategoryTree tree,
    ISiteSettings settings,
    ILogger<ReviewQueueController> logger) : PanelControllerBase
{
    [HttpGet("fila")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        IReadOnlyList<ReviewQueueItem> items;
        try
        {
            items = await queue.ListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Falha ao ler a fila: a tela diz o que houve e oferece "Tentar novamente" (mesmo estado da lista de anúncios, US-012-S08)
            logger.LogError(ex, "Falha ao carregar a fila de revisão (traceId {TraceId})", HttpContext.TraceIdentifier);
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View("LoadError", new PageStateViewModel
            {
                Title = "Não foi possível carregar os anúncios",
                Message = "Tente novamente em alguns instantes.",
                ActionUrl = PanelRoutes.ReviewQueue,
                ReferenceCode = HttpContext.TraceIdentifier
            });
        }

        return View(new ReviewQueueViewModel { Items = items });
    }

    [HttpGet("{id:int}/pre-visualizacao")]
    public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
    {
        Ad ad;
        try
        {
            ad = await ads.GetAsync(id, cancellationToken);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        CategoryTreeSnapshot snapshot = await tree.GetAsync(cancellationToken);
        int? categoryId = ad.CategoryId is { } c && snapshot.Find(c) is not null ? c : null;
        FieldGroup group = categoryId is { } known ? FieldGroupRegistry.Resolve(snapshot, known) : FieldGroupRegistry.Default;
        bool isJob = group.Key == FieldGroupKeys.Jobs;

        IReadOnlyList<AdSpec> characteristics = categoryId is { } specsCategory ? await specs.ReadAsync(ad, specsCategory, group, cancellationToken) : [];
        IReadOnlyList<AdPhotoItem> gallery = isJob ? [] : await photos.ListAsync(id, cancellationToken);
        string phoneDigits = await settings.GetPhoneAsync(cancellationToken);
        bool hasPhone = !string.IsNullOrWhiteSpace(phoneDigits);

        return View(new ReviewPreviewViewModel
        {
            Id = ad.Id,
            StatusLabel = AdStatus.Label(ad.Status),
            InReview = ad.Status == AdStatus.InReview,
            CategoryPath = categoryId is { } path ? string.Join(" › ", snapshot.PathTo(path).Select(n => n.Name)) : null,
            LocationManual = ad.LocationManual,
            Cep = ad.Cep,
            Photos = gallery,
            IsJob = isJob,
            JobAreas = isJob ? [.. characteristics.Where(s => s.Label == "Área").SelectMany(s => s.Value.Split(", "))] : [],
            Phone = hasPhone ? PhoneNumber.Format(phoneDigits) : null,
            PhoneDigits = hasPhone ? phoneDigits : null,
            Body = new AdBodyViewModel
            {
                Title = ad.Title,
                Value = AdPresentation.ValueOf(group, ad.PriceCents, group.HasPrice ? null : characteristics.FirstOrDefault(s => s.Label == "Tipo")?.Value),
                Location = AdPresentation.Location(ad.City, ad.Uf),
                DescriptionLabel = group.DescriptionLabel,
                Description = ad.Description,
                Specs = isJob ? [.. characteristics.Where(s => s.Label != "Área")] : characteristics,
                HeadingLevel = 2
            }
        });
    }
}
