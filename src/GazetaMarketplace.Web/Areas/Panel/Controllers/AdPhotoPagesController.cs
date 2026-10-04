using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Web.Controllers.Api;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GazetaMarketplace.Web.Areas.Panel.Controllers;

/// <summary>
/// As fotos do rascunho <b>sem JavaScript</b> (US-008-S02 e S03): cada ação é um POST comum (uma foto por vez) que volta à página do anúncio com o resultado.
/// Com JavaScript, a mesma tela usa a API (<c>AdPhotosController</c>); as duas passam pelo mesmo <see cref="IAdPhotoService"/>, então a regra de acesso e os limites são idênticos.
/// Remover pede confirmação numa página própria, antes do POST.
/// </summary>
[Route("painel/anuncios/{id:int}/fotos")]
public sealed class AdPhotoPagesController(IAdPhotoService photos, IAdService ads, ICurrentUser currentUser) : PanelControllerBase
{
    /// <summary>Chaves do TempData lidas pela parcial <c>_Photos</c>.</summary>
    public const string MessageKey = "PhotoMessage";

    public const string ErrorKey = "PhotoError";

    [HttpPost("")]
    [RequestSizeLimit(AdPhotosController.MaxBodyBytes)]
    [EnableRateLimiting(RateLimitingExtensions.PhotoUploadPolicy)]
    public Task<IActionResult> Upload(int id, IFormFile file, CancellationToken cancellationToken) =>
        Run(id, async () =>
        {
            if (file is null)
            {
                throw new ValidationException(new System.Collections.Generic.Dictionary<string, string[]> { [PhotoMessages.Field] = ["Escolha uma foto para enviar"] });
            }

            await using Stream content = file.OpenReadStream();
            await photos.AddAsync(id, content, cancellationToken);
            return "Foto adicionada.";
        });

    [HttpPost("{photoId:int}/capa")]
    public Task<IActionResult> SetCover(int id, int photoId, CancellationToken cancellationToken) =>
        Run(id, async () =>
        {
            await photos.SetCoverAsync(id, photoId, cancellationToken);
            return "Capa alterada.";
        });

    /// <summary>A confirmação da remoção (sem JavaScript não há caixa de diálogo): mostra a foto e pede o POST.</summary>
    [HttpGet("{photoId:int}/remover")]
    public async Task<IActionResult> ConfirmRemoval(int id, int photoId, CancellationToken cancellationToken)
    {
        try
        {
            Ad ad = await ads.GetAsync(id, cancellationToken);
            if (!AdAccess.CanEdit(new AdActor(currentUser.UserId, currentUser.IsAdministrator), ad))
            {
                return NoPermission(ad.Status == AdStatus.InReview ? AdMessages.InReviewReadOnly : AdMessages.NotEditable);
            }

            AdPhotoItem photo = (await photos.ListAsync(id, cancellationToken)).FirstOrDefault(p => p.Id == photoId);
            return photo is null ? NotFound() : View(photo);
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

    [HttpPost("{photoId:int}/remover")]
    public Task<IActionResult> Remove(int id, int photoId, CancellationToken cancellationToken) =>
        Run(id, async () =>
        {
            await photos.DeleteAsync(id, photoId, cancellationToken);
            return "Foto removida.";
        });

    // Resultado no TempData e volta à página do anúncio (POST/redirect/GET). A recusa do arquivo vira o aviso da seção de fotos
    private async Task<IActionResult> Run(int id, System.Func<Task<string>> action)
    {
        try
        {
            TempData[MessageKey] = await action();
        }
        catch (ValidationException ex)
        {
            TempData[ErrorKey] = ex.Errors.Values.SelectMany(messages => messages).FirstOrDefault() ?? ex.Message;
        }
        catch (ConflictException ex)
        {
            TempData[ErrorKey] = ex.Message;
        }
        catch (ForbiddenException ex)
        {
            return NoPermission(ex.Message);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction("Edit", "Ads", new { area = "Panel", id });
    }

    private ViewResult NoPermission(string message)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("~/Areas/Panel/Views/Ads/NoPermission.cshtml", message);
    }
}
