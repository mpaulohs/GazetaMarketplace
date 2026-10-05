using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Web.Areas.Panel.Models;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Areas.Panel.Controllers;

/// <summary>
/// Criar e editar o rascunho do anúncio (US-008). A página funciona sem JavaScript: salvar é um POST com redirecionamento, e trocar a categoria sem
/// JavaScript é o botão "Atualizar campos", que refaz o formulário sem salvar. Quem pode ver ou editar cada anúncio é decidido no servidor
/// (<see cref="IAdService"/>), não por esta tela. "Meus anúncios" continua provisório até a tarefa 4.4; a fila de revisão é do `ReviewQueueController`.
/// Despublicar e arquivar (US-011) são páginas de confirmação só do Administrador, decididas pelo <see cref="IAdTakedown"/>.
/// </summary>
[Route("painel/anuncios")]
public sealed class AdsController(IAdService ads, IAdDraftService drafts, IAdSubmission submissions, IAdTakedown takedown, AdFormFactory forms, ICurrentUser currentUser) : PanelControllerBase
{
    /// <summary>Chave do aviso de sucesso no TempData; a página "Meus anúncios" (provisória) também a lê.</summary>
    public const string MessageKey = "AdsMessage";

    private const string WarningKey = "AdsWarning";

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("novo")]
    public async Task<IActionResult> New(CancellationToken cancellationToken) =>
        View("Edit", await forms.BuildAsync(null, new AdFormSubmission(), false, null, null, null, false, cancellationToken));

    [HttpPost("novo")]
    public async Task<IActionResult> Create(AdFormSubmission submission, CancellationToken cancellationToken)
    {
        if (!BindingIsValid())
        {
            return View("Edit", await Rebuild(null, submission, null, cancellationToken));
        }

        try
        {
            AdDraftResult result = await drafts.CreateAsync(ToInput(submission), cancellationToken);
            return Saved(result);
        }
        catch (ValidationException ex)
        {
            return await Invalid(null, submission, ex, cancellationToken);
        }
    }

    [HttpGet("{id:int}/editar")]
    public async Task<IActionResult> Edit(int id, [FromQuery] bool manual, [FromQuery] bool pendencias, CancellationToken cancellationToken)
    {
        try
        {
            Ad ad = await ads.GetAsync(id, cancellationToken);
            AdFormSubmission values = await forms.FromAdAsync(ad, cancellationToken);
            AdActor actor = new(currentUser.UserId, currentUser.IsAdministrator);
            string message = TempData[MessageKey] as string;
            string warning = TempData[WarningKey] as string;
            if (AdAccess.CanEdit(actor, ad))
            {
                IReadOnlyList<AdPending> pending = pendencias && AdStatusRules.Find(ad.Status, AdStatus.InReview) is not null ? await submissions.CheckAsync(id, cancellationToken) : null;
                return View(await forms.BuildAsync(ad, values, false, null, message, warning, manual, cancellationToken, pending));
            }

            string reason = ad.Status == AdStatus.InReview ? AdMessages.InReviewReadOnly : AdMessages.NotEditable;
            return View("Read", await forms.BuildAsync(ad, values, true, reason, message, warning, false, cancellationToken));
        }
        catch (ForbiddenException ex)
        {
            return NoPermission(ex.Message);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:int}/editar")]
    public Task<IActionResult> Save(int id, AdFormSubmission submission, CancellationToken cancellationToken) =>
        SaveThenAsync(id, submission, result => Task.FromResult(Saved(result)), cancellationToken);

    /// <summary>
    /// "Enviar para revisão" (US-009): salva o formulário como "Salvar rascunho" (o que a pessoa vê é o que vale) e confere as pendências do que ficou gravado.
    /// Com pendências, volta à edição com a lista; sem pendências, vai à página de confirmação. A situação só muda no POST de confirmação.
    /// </summary>
    [HttpPost("{id:int}/enviar")]
    public Task<IActionResult> SubmitForReview(int id, AdFormSubmission submission, CancellationToken cancellationToken) =>
        SaveThenAsync(id, submission, async result =>
        {
            SetLocationWarning(result);
            IReadOnlyList<AdPending> pending = await submissions.CheckAsync(id, cancellationToken);
            return pending.Count > 0
                ? RedirectToAction(nameof(Edit), new { id, pendencias = "true" })
                : RedirectToAction(nameof(ConfirmSubmit), new { id });
        }, cancellationToken);

    /// <summary>A confirmação do envio. Confere de novo no servidor: se entre a tela anterior e esta algo mudou, volta à lista de pendências.</summary>
    [HttpGet("{id:int}/enviar/confirmar")]
    public async Task<IActionResult> ConfirmSubmit(int id, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<AdPending> pending = await submissions.CheckAsync(id, cancellationToken);
            if (pending.Count > 0)
            {
                return RedirectToAction(nameof(Edit), new { id, pendencias = "true" });
            }

            Ad ad = await ads.GetAsync(id, cancellationToken);
            return View(new SubmitConfirmationViewModel(id, ad.Title));
        }
        catch (ForbiddenException ex)
        {
            return NoPermission(ex.Message);
        }
        catch (ConflictException ex)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            return View("NoPermission", ex.Message);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:int}/enviar/confirmar")]
    public async Task<IActionResult> Submit(int id, CancellationToken cancellationToken)
    {
        try
        {
            SubmitResult result = await submissions.SubmitAsync(id, cancellationToken);
            if (result.Outcome == SubmitOutcome.HasPending)
            {
                return RedirectToAction(nameof(Edit), new { id, pendencias = "true" });
            }

            // O clique duplo cai aqui na segunda vez: o resultado é "já foi enviado", não um erro
            TempData[MessageKey] = result.Outcome == SubmitOutcome.Sent ? AdMessages.Submitted : AdMessages.AlreadySubmitted;
            return RedirectToAction(nameof(Index));
        }
        catch (ForbiddenException ex)
        {
            return NoPermission(ex.Message);
        }
        catch (ConflictException ex)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            return View("NoPermission", ex.Message);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    [Authorize(Policy = AccessPolicies.Administrator)]
    [HttpGet("{id:int}/despublicar")]
    public Task<IActionResult> ConfirmUnpublish(int id, CancellationToken cancellationToken) =>
        ConfirmTakedownAsync(id, "ConfirmUnpublish", takedown.CheckUnpublishAsync, cancellationToken);

    [Authorize(Policy = AccessPolicies.Administrator)]
    [HttpPost("{id:int}/despublicar")]
    public Task<IActionResult> Unpublish(int id, CancellationToken cancellationToken) =>
        TakedownAsync(id, takedown.UnpublishAsync, AdMessages.Unpublished, nameof(Edit), cancellationToken);

    [Authorize(Policy = AccessPolicies.Administrator)]
    [HttpGet("{id:int}/arquivar")]
    public Task<IActionResult> ConfirmArchive(int id, CancellationToken cancellationToken) =>
        ConfirmTakedownAsync(id, "ConfirmArchive", takedown.CheckArchiveAsync, cancellationToken);

    [Authorize(Policy = AccessPolicies.Administrator)]
    [HttpPost("{id:int}/arquivar")]
    public Task<IActionResult> Archive(int id, CancellationToken cancellationToken) =>
        TakedownAsync(id, takedown.ArchiveAsync, AdMessages.Archived, nameof(Index), cancellationToken);

    // O GET só confere e mostra a pergunta: nada é gravado até o POST (cancelar = não fazer nada, US-011-S03)
    private async Task<IActionResult> ConfirmTakedownAsync(int id, string view, Func<int, CancellationToken, Task<TakedownResult>> check, CancellationToken cancellationToken)
    {
        try
        {
            TakedownResult result = await check(id, cancellationToken);
            if (result.Outcome != TakedownOutcome.Done)
            {
                TempData[WarningKey] = result.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }

            Ad ad = await ads.GetAsync(id, cancellationToken);
            string cancelUrl = ad.Status == AdStatus.InReview
                ? Url.Action("Preview", "ReviewQueue", new { area = "Panel", id })
                : Url.Action(nameof(Edit), new { id });
            return View(view, new TakedownConfirmationViewModel(id, ad.Title, cancelUrl));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    private async Task<IActionResult> TakedownAsync(
        int id, Func<int, CancellationToken, Task<TakedownResult>> act, string success, string successAction, CancellationToken cancellationToken)
    {
        try
        {
            TakedownResult result = await act(id, cancellationToken);
            if (result.Outcome == TakedownOutcome.Done)
            {
                TempData[MessageKey] = success;
                return successAction == nameof(Edit) ? RedirectToAction(nameof(Edit), new { id }) : RedirectToAction(successAction);
            }

            // Clique duplo ou outra pessoa agiu antes: a frase da situação, na tela do anúncio, sem erro
            TempData[WarningKey] = result.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    // Salva o formulário (as mesmas recusas de "Salvar rascunho") e, se salvou, deixa a continuação decidir para onde ir
    private async Task<IActionResult> SaveThenAsync(int id, AdFormSubmission submission, Func<AdDraftResult, Task<IActionResult>> onSaved, CancellationToken cancellationToken)
    {
        if (!BindingIsValid())
        {
            return View("Edit", await Rebuild(id, submission, null, cancellationToken));
        }

        try
        {
            AdDraftResult result = await drafts.UpdateAsync(id, DecodeRowVersion(submission.RowVersion), ToInput(submission), cancellationToken);
            return await onSaved(result);
        }
        catch (ValidationException ex)
        {
            return await Invalid(id, submission, ex, cancellationToken);
        }
        catch (ConflictException ex)
        {
            return await Conflicted(id, submission, ex.Message, cancellationToken);
        }
        catch (ForbiddenException ex)
        {
            return NoPermission(ex.Message);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Sem JavaScript: refaz o formulário para a categoria escolhida, sem salvar nada.</summary>
    [HttpPost("novo/atualizar")]
    public async Task<IActionResult> RefreshNew(AdFormSubmission submission, CancellationToken cancellationToken) =>
        View("Edit", await forms.BuildAsync(null, submission, false, null, null, null, false, cancellationToken));

    [HttpPost("{id:int}/atualizar")]
    public async Task<IActionResult> Refresh(int id, AdFormSubmission submission, CancellationToken cancellationToken)
    {
        try
        {
            Ad ad = await ads.GetAsync(id, cancellationToken);
            AdActor actor = new(currentUser.UserId, currentUser.IsAdministrator);
            if (!AdAccess.CanEdit(actor, ad))
            {
                return NoPermission(ad.Status == AdStatus.InReview ? AdMessages.InReviewReadOnly : AdMessages.NotEditable);
            }

            return View("Edit", await forms.BuildAsync(ad, submission, false, null, null, null, false, cancellationToken));
        }
        catch (ForbiddenException ex)
        {
            return NoPermission(ex.Message);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Só o trecho do formulário que depende da categoria (título, descrição, preço, campos e fotos), para o JavaScript trocar sem recarregar a
    /// página. Não lê nem devolve dado de anúncio nenhum: é o desenho do grupo, vazio.
    /// </summary>
    [HttpGet("campos")]
    public async Task<IActionResult> Fields([FromQuery] int? categoryId, CancellationToken cancellationToken) =>
        PartialView("_AdGroupRegion", await forms.BuildAsync(null, new AdFormSubmission { CategoryId = categoryId }, false, null, null, null, false, cancellationToken));

    private static AdDraftInput ToInput(AdFormSubmission s) =>
        new(s.Title, s.Description, s.CategoryId, s.Price, s.Cep, s.City, s.Uf, s.LocationCep, s.LocationManual, s.Fields ?? []);

    private static byte[] DecodeRowVersion(string text)
    {
        try
        {
            return string.IsNullOrEmpty(text) ? null : Convert.FromBase64String(text);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    // Um valor de tipo errado (CategoryId="abc") não chega ao serviço como "vazio": volta ao formulário com a recusa em português, sem erro 500
    private bool BindingIsValid()
    {
        if (ModelState.IsValid)
        {
            return true;
        }

        bool onlyHarmless = true;
        foreach (string key in ModelState.Keys.ToList())
        {
            if (ModelState[key].Errors.Count == 0)
            {
                continue;
            }

            ModelState.Remove(key);
            if (key == nameof(AdFormSubmission.CategoryId))
            {
                ModelState.AddModelError(key, AdMessages.CategoryInvalid);
                onlyHarmless = false;
            }
            else if (key is not (nameof(AdFormSubmission.LocationManual) or nameof(AdFormSubmission.RowVersion)))
            {
                ModelState.AddModelError(string.Empty, "Os dados enviados são inválidos. Confira o formulário e tente de novo.");
                onlyHarmless = false;
            }
        }

        return onlyHarmless;
    }

    private IActionResult Saved(AdDraftResult result)
    {
        TempData[MessageKey] = AdMessages.DraftSaved;
        SetLocationWarning(result);
        return RedirectToAction(nameof(Edit), new { id = result.Id, manual = result.Location == LocationOutcome.CepUnavailable ? true : (bool?)null });
    }

    private void SetLocationWarning(AdDraftResult result)
    {
        switch (result.Location)
        {
            case LocationOutcome.CepNotFound:
                TempData[WarningKey] = CepRules.NotFoundMessage;
                break;
            case LocationOutcome.CepUnavailable:
                TempData[WarningKey] = "Não foi possível buscar o CEP. Preencha Cidade e UF manualmente.";
                break;
        }
    }

    private async Task<IActionResult> Invalid(int? id, AdFormSubmission submission, ValidationException exception, CancellationToken cancellationToken)
    {
        foreach (KeyValuePair<string, string[]> error in exception.Errors)
        {
            foreach (string message in error.Value)
            {
                ModelState.AddModelError(FormKey(error.Key), message);
            }
        }

        return View("Edit", await Rebuild(id, submission, null, cancellationToken));
    }

    private async Task<IActionResult> Conflicted(int id, AdFormSubmission submission, string message, CancellationToken cancellationToken)
    {
        ModelState.AddModelError(string.Empty, message);
        return View("Edit", await Rebuild(id, submission, null, cancellationToken));
    }

    private async Task<AdFormViewModel> Rebuild(int? id, AdFormSubmission submission, string message, CancellationToken cancellationToken)
    {
        Ad ad = id is { } existing ? await ads.GetAsync(existing, cancellationToken) : null;
        return await forms.BuildAsync(ad, submission, false, null, message, null, false, cancellationToken);
    }

    private ViewResult NoPermission(string message)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("NoPermission", message);
    }

    // Os erros do domínio chegam com o nome em minúsculas; o formulário usa os nomes das propriedades
    private static string FormKey(string key) => key switch
    {
        "title" => nameof(AdFormSubmission.Title),
        "description" => nameof(AdFormSubmission.Description),
        "price" => nameof(AdFormSubmission.Price),
        _ => key
    };
}
