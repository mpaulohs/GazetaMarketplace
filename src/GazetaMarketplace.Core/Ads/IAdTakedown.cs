using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Ads;

/// <summary>Como terminou um pedido de retirada: feito, ou o anúncio já não estava na situação que o pedido supunha (outra pessoa agiu antes, ou clique duplo).</summary>
public enum TakedownOutcome
{
    Done = 1,
    AlreadyDecided = 2
}

/// <param name="Outcome">Como terminou.</param>
/// <param name="Message">A frase para a pessoa quando <see cref="TakedownOutcome.AlreadyDecided"/>.</param>
public sealed record TakedownResult(TakedownOutcome Outcome, string Message = null)
{
    public static TakedownResult Done { get; } = new(TakedownOutcome.Done);
}

/// <summary>
/// Tirar um anúncio do ar (US-011): <b>despublicar</b> (Publicado → Rascunho, pode voltar à revisão) e <b>arquivar</b> (qualquer situação menos Arquivado,
/// definitivo na v1). Só o Administrador; quem não é Administrador recebe <c>ForbiddenException</c>. A passagem, a trilha e a auditoria são do <see cref="IAdService"/>.
/// </summary>
public interface IAdTakedown
{
    /// <summary>Confere, sem gravar, se o anúncio ainda pode ser despublicado (a página de confirmação usa).</summary>
    Task<TakedownResult> CheckUnpublishAsync(int adId, CancellationToken cancellationToken);

    /// <summary>Confere, sem gravar, se o anúncio ainda pode ser arquivado.</summary>
    Task<TakedownResult> CheckArchiveAsync(int adId, CancellationToken cancellationToken);

    Task<TakedownResult> UnpublishAsync(int adId, CancellationToken cancellationToken);

    Task<TakedownResult> ArchiveAsync(int adId, CancellationToken cancellationToken);
}
