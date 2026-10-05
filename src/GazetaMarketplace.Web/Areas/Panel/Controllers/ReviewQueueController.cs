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
/// Publicar e rejeitar (US-010-S03 a S05, S07 e S08) são páginas de confirmação e do motivo, que funcionam com e sem JavaScript; a decisão é do <see cref="IAdReview"/>.
/// "Arquivar" (US-011-S05) é uma página do <c>AdsController</c>, ligada a esta tela pela barra de decisão. Quem pode ver cada anúncio é decidido no servidor pelo <see cref="IAdService"/>.
/// </summary>
[Authorize(Policy = AccessPolicies.Administrator)]
[Route("painel/anuncios")]
public sealed class ReviewQueueController(
    IReviewQueue queue,
    IAdReview review,
    IAdService ads,
    IAdPhotoService photos,
    IAdSpecsReader specs,
    ICategoryTree tree,
    ISiteSettings settings,
    ILogger<ReviewQueueController> logger) : PanelControllerBase
{
    /// <summary>Chave do aviso de sucesso ("Anúncio publicado") que a fila mostra depois da decisão.</summary>
    public const string MessageKey = "ReviewMessage";

    private const string AlertKindKey = "ReviewAlertKind";
    private const string AlertMessageKey = "ReviewAlertMessage";
    private const string CheckPendingKey = "ReviewCheckPending";

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

        return View(new ReviewQueueViewModel { Items = items, Message = TempData[MessageKey] as string });
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
        ReviewAlert alert = TempData[AlertMessageKey] is string alertMessage
            ? new ReviewAlert(TempData[AlertKindKey] is "phone" ? ReviewAlertKind.PhoneMissing : ReviewAlertKind.Conflict, alertMessage)
            : null;
        IReadOnlyList<AdPending> pending = [];
        if (TempData[CheckPendingKey] is not null && ad.Status == AdStatus.InReview)
        {
            ReviewResult check = await review.CheckPublishAsync(id, cancellationToken);
            pending = check.Pending ?? [];
        }

        string phoneDigits = await settings.GetPhoneAsync(cancellationToken);
        bool hasPhone = !string.IsNullOrWhiteSpace(phoneDigits);

        return View(new ReviewPreviewViewModel
        {
            Id = ad.Id,
            StatusLabel = AdStatus.Label(ad.Status),
            InReview = ad.Status == AdStatus.InReview,
            Takedown = TakedownActions.For(new AdActor(null, true), ad), // o controller é só do Administrador,
            Alert = alert,
            Pending = pending,
            CategoryPath = categoryId is { } path ? string.Join(" › ", snapshot.PathTo(path).Select(n => n.Name)) : null,
            LocationManual = ad.LocationManual,
            Cep = ad.Cep,
            Photos = gallery,
            IsJob = isJob,
            JobAreas = isJob ? [.. characteristics.Where(s => s.Key == FieldKeys.JobAreas).SelectMany(s => s.Items ?? [])] : [],
            Phone = hasPhone ? PhoneNumber.Format(phoneDigits) : null,
            PhoneDigits = hasPhone ? phoneDigits : null,
            Body = new AdBodyViewModel
            {
                Title = ad.Title,
                Value = AdPresentation.ValueOf(group, ad.PriceCents, group.HasPrice ? null : characteristics.FirstOrDefault(s => s.Key == FieldKeys.ServiceType)?.Value),
                Location = AdPresentation.Location(ad.City, ad.Uf),
                DescriptionLabel = group.DescriptionLabel,
                Description = ad.Description,
                Specs = isJob ? [.. characteristics.Where(s => s.Key != FieldKeys.JobAreas)] : characteristics,
                HeadingLevel = 2
            }
        });
    }

    [HttpGet("{id:int}/publicar")]
    public async Task<IActionResult> ConfirmPublish(int id, CancellationToken cancellationToken)
    {
        try
        {
            ReviewResult check = await review.CheckPublishAsync(id, cancellationToken);
            if (check.Outcome != ReviewOutcome.Done)
            {
                return BackToPreview(id, check);
            }

            Ad ad = await ads.GetAsync(id, cancellationToken);
            return View(new PublishConfirmationViewModel(id, ad.Title));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:int}/publicar")]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        try
        {
            ReviewResult result = await review.PublishAsync(id, cancellationToken);
            if (result.Outcome != ReviewOutcome.Done)
            {
                return BackToPreview(id, result);
            }

            TempData[MessageKey] = AdMessages.Published;
            return Redirect(PanelRoutes.ReviewQueue);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id:int}/rejeitar")]
    public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken)
    {
        try
        {
            ReviewResult check = await review.CheckRejectAsync(id, cancellationToken);
            if (check.Outcome != ReviewOutcome.Done)
            {
                return BackToPreview(id, check);
            }

            Ad ad = await ads.GetAsync(id, cancellationToken);
            return View(new RejectViewModel { Id = id, Title = ad.Title });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:int}/rejeitar")]
    public async Task<IActionResult> Reject(int id, string reason, CancellationToken cancellationToken)
    {
        try
        {
            ReviewResult result = await review.RejectAsync(id, reason, cancellationToken);
            if (result.Outcome != ReviewOutcome.Done)
            {
                return BackToPreview(id, result);
            }

            TempData[MessageKey] = AdMessages.Rejected;
            return Redirect(PanelRoutes.ReviewQueue);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ValidationException ex)
        {
            // Motivo vazio ou longo demais (S05): a página volta com o que foi digitado e o erro ao lado do campo; a situação não mudou
            Ad ad = await ads.GetAsync(id, cancellationToken);
            ModelState.AddModelError(nameof(RejectViewModel.Reason), ex.Errors.Values.SelectMany(v => v).FirstOrDefault() ?? AdMessages.RejectionReasonRequired);
            return View(new RejectViewModel { Id = id, Title = ad.Title, Reason = reason });
        }
    }

    // Volta à pré-visualização com o aviso do que impediu a decisão (a situação mostrada é sempre a verdadeira)
    private RedirectToActionResult BackToPreview(int id, ReviewResult result)
    {
        switch (result.Outcome)
        {
            case ReviewOutcome.PhoneNotConfigured:
                TempData[AlertKindKey] = "phone";
                TempData[AlertMessageKey] = result.Message;
                break;
            case ReviewOutcome.AlreadyDecided:
                TempData[AlertKindKey] = "conflict";
                TempData[AlertMessageKey] = result.Message;
                break;
            case ReviewOutcome.HasPending:
                TempData[CheckPendingKey] = "1";
                break;
        }

        return RedirectToAction(nameof(Preview), new { id });
    }
}
