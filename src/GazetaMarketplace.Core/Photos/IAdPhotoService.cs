using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;

namespace GazetaMarketplace.Core.Photos;

/// <summary>Uma foto do anúncio como a tela a mostra: posição (0 é a capa), tamanho da versão grande e os dois endereços.</summary>
public sealed record AdPhotoItem(int Id, int AdId, int Position, int Width, int Height)
{
    public bool IsCover => Position == 0;

    public string ThumbUrl => PhotoUrls.For(AdId, Id, PhotoSize.Thumb);

    public string LargeUrl => PhotoUrls.For(AdId, Id, PhotoSize.Large);
}

/// <summary>
/// Enviar, trocar a capa e remover as fotos de um rascunho (US-008-S02 a S06). Quem pode mexer é decidido aqui, no servidor, pela mesma regra de edição do
/// anúncio (<c>AdAccess.CanEdit</c>): o autor em Rascunho ou Rejeitado, ou o Administrador. Cada operação trava a linha do anúncio dentro de uma transação, então
/// dois envios ao mesmo tempo nunca passam do limite nem repetem a posição.
/// </summary>
public interface IAdPhotoService
{
    /// <summary>As fotos do anúncio na ordem da galeria. Não confere acesso: quem chama já carregou o anúncio pelo <c>IAdService</c>.</summary>
    Task<IReadOnlyList<AdPhotoItem>> ListAsync(int adId, CancellationToken cancellationToken);

    /// <summary>Processa e grava a foto no fim da galeria (a primeira é a capa).</summary>
    /// <exception cref="NotFoundException">O anúncio não existe.</exception>
    /// <exception cref="ForbiddenException">Sem acesso ao anúncio ou situação que não aceita edição.</exception>
    /// <exception cref="ConflictException">O anúncio já tem o máximo de fotos da categoria (409, <see cref="PhotoMessages.TooManyPhotos"/>).</exception>
    /// <exception cref="ValidationException">Arquivo vazio, acima de 10 MB, de formato não aceito ou ilegível (campo <c>file</c>).</exception>
    Task<AdPhotoItem> AddAsync(int adId, Stream content, CancellationToken cancellationToken);

    /// <summary>Move a foto para a primeira posição; as outras descem uma, na mesma ordem.</summary>
    /// <exception cref="NotFoundException">O anúncio ou a foto (deste anúncio) não existe.</exception>
    /// <exception cref="ForbiddenException">Sem acesso ao anúncio ou situação que não aceita edição.</exception>
    Task SetCoverAsync(int adId, int photoId, CancellationToken cancellationToken);

    /// <summary>Remove a foto e as duas versões WebP; o original fica para a limpeza de 30 dias. Se era a capa, a próxima assume.</summary>
    /// <exception cref="NotFoundException">O anúncio ou a foto (deste anúncio) não existe.</exception>
    /// <exception cref="ForbiddenException">Sem acesso ao anúncio ou situação que não aceita edição.</exception>
    Task DeleteAsync(int adId, int photoId, CancellationToken cancellationToken);
}
