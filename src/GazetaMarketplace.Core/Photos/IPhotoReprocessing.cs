using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;

namespace GazetaMarketplace.Core.Photos;

/// <summary>
/// Regera as duas versões WebP de uma foto a partir do original guardado (manutenção, ADR-005; sem tela nem rota na v1). O original só existe por 30 dias:
/// depois disso, ou se o arquivo sumiu, a foto não pode ser reprocessada.
/// </summary>
/// <remarks>
/// As versões são regravadas <b>nos mesmos nomes</b>. A rota pública usa o <c>photoId</c> e responde com cache <c>immutable</c> de 1 ano, então quem já tem a foto
/// em cache não vê a mudança: antes de qualquer uso real, a URL precisa de um parâmetro de versão (<c>?v=</c>). Está no BACKLOG.
/// </remarks>
public interface IPhotoReprocessing
{
    /// <exception cref="NotFoundException">A foto não existe.</exception>
    /// <exception cref="ConflictException">Sem original: a chave foi anulada pela limpeza ou o arquivo não existe ("Original indisponível").</exception>
    /// <exception cref="ValidationException">O original não pode mais ser lido como foto.</exception>
    Task ReprocessAsync(int photoId, CancellationToken cancellationToken);
}
