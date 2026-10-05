using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;

namespace GazetaMarketplace.Core.Ads;

/// <summary>Como terminou um pedido de decisão (publicar ou rejeitar) do Administrador.</summary>
public enum ReviewOutcome
{
    /// <summary>A decisão foi aplicada agora (ou, em <c>Check…</c>, pode ser aplicada).</summary>
    Done = 1,

    /// <summary>O telefone/WhatsApp do site não foi configurado: nenhum anúncio pode ser publicado (US-010-S08).</summary>
    PhoneNotConfigured = 2,

    /// <summary>O anúncio já não está Em revisão: outra pessoa (ou este mesmo Administrador, num clique duplo) decidiu antes (US-010-S07).</summary>
    AlreadyDecided = 3,

    /// <summary>Faltam itens que o envio à revisão exige (por exemplo, o preço ou a foto foram apagados na edição); a situação não mudou.</summary>
    HasPending = 4
}

/// <param name="Outcome">O que aconteceu.</param>
/// <param name="Pending">As pendências, quando <see cref="ReviewOutcome.HasPending"/>.</param>
/// <param name="Message">A frase para mostrar, quando <see cref="ReviewOutcome.AlreadyDecided"/> ou <see cref="ReviewOutcome.PhoneNotConfigured"/>.</param>
public sealed record ReviewResult(ReviewOutcome Outcome, IReadOnlyList<AdPending> Pending = null, string Message = null)
{
    public static ReviewResult Done { get; } = new(ReviewOutcome.Done);
}

/// <summary>
/// A decisão do Administrador sobre um anúncio Em revisão (US-010): publicar ou rejeitar com motivo. A mudança de situação, a trilha de decisão e a auditoria são do
/// <see cref="IAdService"/>; aqui ficam o telefone do site, a reconferência das pendências, o motivo e a corrida entre dois Administradores. Quem pode decidir é conferido
/// no servidor (só o Administrador, NFR-13), não pela tela.
/// </summary>
public interface IAdReview
{
    /// <summary>Se o anúncio pode ser publicado agora: ainda Em revisão, com o telefone do site configurado e sem pendências. Não muda nada.</summary>
    /// <exception cref="NotFoundException">O anúncio não existe.</exception>
    /// <exception cref="ForbiddenException">Quem pede não é Administrador.</exception>
    Task<ReviewResult> CheckPublishAsync(int adId, CancellationToken cancellationToken);

    /// <summary>Se o anúncio ainda pode ser rejeitado (continua Em revisão). Não muda nada.</summary>
    /// <exception cref="NotFoundException">O anúncio não existe.</exception>
    /// <exception cref="ForbiddenException">Quem pede não é Administrador.</exception>
    Task<ReviewResult> CheckRejectAsync(int adId, CancellationToken cancellationToken);

    /// <summary>
    /// Publica. Telefone ausente, pendências ou decisão de outra pessoa não mudam nada e voltam no resultado. Dois pedidos ao mesmo tempo: um passa, o outro perde a corrida
    /// pelo <c>RowVersion</c> e recebe <see cref="ReviewOutcome.AlreadyDecided"/>, então há uma única passagem e uma única auditoria.
    /// </summary>
    Task<ReviewResult> PublishAsync(int adId, CancellationToken cancellationToken);

    /// <summary>Rejeita com o motivo (obrigatório, até <see cref="Ad.RejectionReasonMaxLength"/> caracteres; o motivo vai à auditoria).</summary>
    /// <exception cref="ValidationException">Motivo vazio ou longo demais.</exception>
    Task<ReviewResult> RejectAsync(int adId, string reason, CancellationToken cancellationToken);
}
