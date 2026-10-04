using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Photos;

/// <summary>Uma foto aberta para ser entregue.</summary>
/// <param name="Content">O arquivo WebP.</param>
/// <param name="IsPublic">Anúncio publicado: qualquer visitante vê e o navegador pode guardar para sempre. Senão, só a equipe com acesso.</param>
public sealed record PhotoFile(Stream Content, bool IsPublic);

/// <summary>Decide quem pode ver cada foto (ADR-005) e a abre. A resposta é a mesma para "não existe" e "não pode ver".</summary>
public interface IPhotoDelivery
{
    /// <returns>A foto, ou <c>null</c> se não existe, não pertence a este anúncio ou o chamador não pode vê-la.</returns>
    Task<PhotoFile> OpenAsync(int adId, int photoId, PhotoSize size, CancellationToken cancellationToken);
}
