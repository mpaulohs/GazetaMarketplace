using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace GazetaMarketplace.Web.Controllers;

/// <summary>
/// Entrega as fotos (ADR-005, <c>getPhotoFile</c>): <c>/fotos/{adId}/{photoId}-{480|1600}.webp</c>. Anúncio publicado é público e o navegador pode guardar para
/// sempre (o nome do arquivo não muda sem a foto mudar); em qualquer outra situação só a equipe com acesso vê, e a resposta de "não existe" e de "não pode
/// ver" é a mesma 404. Os originais (<c>_originals/</c>) não têm rota: o caminho do disco nunca sai do servidor. Tem limite próprio de 300 pedidos por minuto por IP
/// (e fica fora do limite global de 100).
/// </summary>
[Route("fotos")]
[EnableRateLimiting(RateLimitingExtensions.PhotoPolicy)]
public sealed class PhotosController(IPhotoDelivery delivery) : Controller
{
    public const string PublicCache = "public, max-age=31536000, immutable";

    public const string PrivateCache = "private, no-store";

    // :int recusa ids que não são números antes de chegar aqui (404); {size} é conferido contra os dois tamanhos
    [HttpGet("{adId:int}/{photoId:int}-{size}.webp")]
    public async Task<IActionResult> Get(int adId, int photoId, string size, CancellationToken cancellationToken)
    {
        if (PhotoSizes.Parse(size) is not { } photoSize)
        {
            return NotFound();
        }

        PhotoFile file = await delivery.OpenAsync(adId, photoId, photoSize, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        Response.Headers[HeaderNames.CacheControl] = file.IsPublic ? PublicCache : PrivateCache;
        return File(file.Content, "image/webp");
    }
}
