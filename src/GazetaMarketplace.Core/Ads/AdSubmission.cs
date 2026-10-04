using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;

namespace GazetaMarketplace.Core.Ads;

/// <summary>Uma pendência que impede o envio à revisão: o texto da lista e o <c>id</c> do campo da tela para onde o link leva.</summary>
/// <param name="Message">A frase da lista ("Adicione ao menos 1 foto", "Informe a quilometragem").</param>
/// <param name="Target">O <c>id</c> HTML do campo ou da seção (<c>cep</c>, <c>fotos</c>, <c>campo-km</c>…).</param>
public sealed record AdPending(string Message, string Target);

/// <summary>Como terminou um pedido de envio para revisão.</summary>
public enum SubmitOutcome
{
    /// <summary>A situação passou a Em revisão agora.</summary>
    Sent = 1,

    /// <summary>O anúncio já estava Em revisão (clique duplo, segunda aba): nada foi mudado e não é erro.</summary>
    AlreadySent = 2,

    /// <summary>Faltam itens; a situação não mudou.</summary>
    HasPending = 3
}

/// <param name="Outcome">O que aconteceu.</param>
/// <param name="Pending">As pendências, quando <see cref="SubmitOutcome.HasPending"/>.</param>
public sealed record SubmitResult(SubmitOutcome Outcome, IReadOnlyList<AdPending> Pending);

/// <summary>
/// Enviar o anúncio para a revisão do Administrador (US-009). O servidor confere as pendências sobre o que está <b>gravado</b> (a tela salva o formulário antes) e só
/// então muda a situação, com a trilha e a auditoria <c>ad.submit</c> do <see cref="IAdService"/>.
/// </summary>
public interface IAdSubmission
{
    /// <summary>As pendências do anúncio gravado, na ordem da tela; vazia = pronto para enviar.</summary>
    /// <exception cref="NotFoundException">O anúncio não existe.</exception>
    /// <exception cref="ForbiddenException">Sem acesso, ou situação que não aceita envio (Em revisão, Publicado, Arquivado).</exception>
    Task<IReadOnlyList<AdPending>> CheckAsync(int adId, CancellationToken cancellationToken);

    /// <summary>
    /// Confere e envia. Anúncio já Em revisão devolve <see cref="SubmitOutcome.AlreadySent"/> (o clique duplo gera uma única passagem, uma única auditoria e uma única
    /// entrada na fila); com pendências devolve <see cref="SubmitOutcome.HasPending"/> sem mudar nada.
    /// </summary>
    /// <exception cref="NotFoundException">O anúncio não existe.</exception>
    /// <exception cref="ForbiddenException">Sem acesso, ou situação que não aceita envio (Publicado, Arquivado, ou rejeitado por outro autor).</exception>
    Task<SubmitResult> SubmitAsync(int adId, CancellationToken cancellationToken);
}
