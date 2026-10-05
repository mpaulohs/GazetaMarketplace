using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Ads;

/// <summary>Uma linha da fila de revisão: o que o Administrador precisa para escolher qual anúncio abrir (US-010-S01).</summary>
/// <param name="Id">Id do anúncio.</param>
/// <param name="Title">Título.</param>
/// <param name="AuthorName">Nome de quem cadastrou.</param>
/// <param name="CategoryName">Nome da categoria.</param>
/// <param name="SentAt">Quando foi enviado para revisão (UTC); o reenvio de um rejeitado vale como novo envio.</param>
public sealed record ReviewQueueItem(int Id, string Title, string AuthorName, string CategoryName, DateTime SentAt);

/// <summary>
/// A fila de revisão: os anúncios "Em revisão", do mais antigo ao mais novo (quem espera há mais tempo vem primeiro). Só leitura; decidir é da 4.2. Quem pode abrir a fila
/// é decidido pela tela (só o Administrador, NFR-13).
/// </summary>
public interface IReviewQueue
{
    Task<IReadOnlyList<ReviewQueueItem>> ListAsync(CancellationToken cancellationToken);
}

/// <summary>As características do anúncio com os nomes do catálogo de veículos já resolvidos.</summary>
public interface IAdSpecsReader
{
    Task<IReadOnlyList<AdSpec>> ReadAsync(Ad ad, int categoryId, Fields.FieldGroup group, CancellationToken cancellationToken);
}
