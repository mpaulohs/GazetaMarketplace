using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GazetaMarketplace.Web.Controllers.Api;

/// <summary>
/// Fotos de um anúncio (ADR-005, US-008-S02 a S06): <c>uploadAdPhoto</c>, <c>setAdPhotoCover</c> e <c>deleteAdPhoto</c>. Uma foto por pedido. Só a equipe logada, e quem
/// pode mexer em cada anúncio é decidido pelo <see cref="IAdPhotoService"/> (a mesma regra de edição do rascunho). Escritas exigem o token antiforgery no cabeçalho.
/// Respostas: 201/204 · 400 <c>VALIDATION_ERROR</c> (arquivo vazio, acima de 10 MB, formato não aceito, ilegível) · 401 · 403 · 404 · 409 <c>CONFLICT</c> (limite de fotos) ·
/// 413 (corpo acima de 11 MB, recusado antes de ler) · 429 <c>RATE_LIMITED</c> (30 envios por minuto por usuário).
/// </summary>
[ApiController]
[Route("api/v1/ads/{adId:int}/photos")]
[Produces("application/json")]
[Authorize(Policy = AccessPolicies.Writer)]
public sealed class AdPhotosController(IAdPhotoService photos) : ControllerBase
{
    /// <summary>
    /// O limite do corpo é 11 MB, um a mais que o da foto (10 MB): assim um arquivo de 10 a 11 MB chega ao serviço e recebe a mensagem de validação
    /// ("A foto excede o limite de 10 MB") em vez de um 413 sem texto.
    /// </summary>
    public const long MaxBodyBytes = 11 * 1024 * 1024;

    [HttpPost("")]
    [RequestSizeLimit(MaxBodyBytes)]
    [EnableRateLimiting(RateLimitingExtensions.PhotoUploadPolicy)]
    public async Task<ActionResult<PhotoResponse>> Upload(int adId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            throw new ValidationException(new Dictionary<string, string[]> { [PhotoMessages.Field] = [PhotoMessages.Empty] });
        }

        await using Stream content = file.OpenReadStream();
        AdPhotoItem item = await photos.AddAsync(adId, content, cancellationToken);
        return Created(item.LargeUrl, PhotoResponse.From(item));
    }

    [HttpPost("{photoId:int}/cover")]
    public async Task<IActionResult> SetCover(int adId, int photoId, CancellationToken cancellationToken)
    {
        await photos.SetCoverAsync(adId, photoId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{photoId:int}")]
    public async Task<IActionResult> Delete(int adId, int photoId, CancellationToken cancellationToken)
    {
        await photos.DeleteAsync(adId, photoId, cancellationToken);
        return NoContent();
    }

    /// <summary>O contrato <c>Photo</c> do <c>openapi.yaml</c>.</summary>
    public sealed record PhotoResponse(int Id, int SortOrder, string Url480, string Url1600, int Width, int Height)
    {
        public static PhotoResponse From(AdPhotoItem item) => new(item.Id, item.Position, item.ThumbUrl, item.LargeUrl, item.Width, item.Height);
    }
}
