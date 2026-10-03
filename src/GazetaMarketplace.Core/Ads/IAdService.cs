using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;

namespace GazetaMarketplace.Core.Ads;

/// <summary>
/// Leitura protegida e mudança de situação do anúncio. A autoria é conferida aqui, no servidor (NFR-13), com o ator vindo do
/// <c>ICurrentUser</c>; não depende de o controller ter lembrado de checar.
/// </summary>
public interface IAdService
{
    /// <summary>Carrega o anúncio para leitura.</summary>
    /// <exception cref="NotFoundException">O anúncio não existe.</exception>
    /// <exception cref="ForbiddenException">Redator tentando ler anúncio de outro (US-008-S10).</exception>
    Task<Ad> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Carrega o anúncio para edição, rastreado pelo contexto para a tarefa 3.3 alterar e gravar.</summary>
    /// <exception cref="NotFoundException">O anúncio não existe.</exception>
    /// <exception cref="ForbiddenException">Sem acesso (S10) ou situação que não aceita edição (S12); a mensagem diz o motivo.</exception>
    Task<Ad> GetForEditAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Muda a situação (Apêndice A), grava a trilha de decisão e a auditoria. O motivo só vale para Rejeitado (obrigatório, até
    /// <see cref="Ad.RejectionReasonMaxLength"/> caracteres).
    /// </summary>
    /// <exception cref="NotFoundException">O anúncio não existe.</exception>
    /// <exception cref="ForbiddenException">O ator não pode fazer esta passagem neste anúncio.</exception>
    /// <exception cref="ConflictException">A passagem não existe, ou outra pessoa decidiu o mesmo anúncio antes.</exception>
    /// <exception cref="ValidationException">Rejeição sem motivo ou com motivo longo.</exception>
    Task<Ad> TransitionAsync(int id, byte target, string reason, CancellationToken cancellationToken);
}
