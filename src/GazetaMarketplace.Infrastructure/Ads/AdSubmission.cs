using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.Infrastructure.Ads;

/// <inheritdoc cref="IAdSubmission"/>
/// <remarks>
/// A mudança de situação é do <see cref="IAdService"/> (passagem do Apêndice A, <c>RowVersion</c>, trilha e auditoria <c>ad.submit</c>); aqui ficam só a conferência das
/// pendências e o clique duplo. Dois pedidos ao mesmo tempo: um passa, o outro perde a corrida pelo <c>RowVersion</c> (ou chega depois e vê o anúncio já Em revisão) e
/// recebe <see cref="SubmitOutcome.AlreadySent"/>, então a fila e a auditoria ganham uma única entrada.
/// </remarks>
public sealed class AdSubmission(AppDbContext context, IAdService ads, ICategoryTree categories, ICurrentUser currentUser) : IAdSubmission
{
    public async Task<IReadOnlyList<AdPending>> CheckAsync(int adId, CancellationToken cancellationToken)
    {
        Ad ad = await ads.GetAsync(adId, cancellationToken);
        EnsureCanSubmit(ad);
        return await PendingAsync(ad, cancellationToken);
    }

    public async Task<SubmitResult> SubmitAsync(int adId, CancellationToken cancellationToken)
    {
        Ad ad = await ads.GetAsync(adId, cancellationToken);
        if (ad.Status == AdStatus.InReview)
        {
            return new SubmitResult(SubmitOutcome.AlreadySent, []);
        }

        EnsureCanSubmit(ad);
        IReadOnlyList<AdPending> pending = await PendingAsync(ad, cancellationToken);
        if (pending.Count > 0)
        {
            return new SubmitResult(SubmitOutcome.HasPending, pending);
        }

        try
        {
            await ads.TransitionAsync(adId, AdStatus.InReview, null, cancellationToken);
            return new SubmitResult(SubmitOutcome.Sent, []);
        }
        catch (ConflictException)
        {
            // Perdeu a corrida: se o outro pedido já enviou, o resultado é o mesmo; qualquer outra mudança é um conflito de verdade
            context.ChangeTracker.Clear();
            Ad current = await ads.GetAsync(adId, cancellationToken);
            if (current.Status == AdStatus.InReview)
            {
                return new SubmitResult(SubmitOutcome.AlreadySent, []);
            }

            throw;
        }
    }

    // Só o autor (Rascunho ou Rejeitado) ou o Administrador enviam; a mensagem diz o motivo
    private void EnsureCanSubmit(Ad ad)
    {
        AdActor actor = new(currentUser.UserId, currentUser.IsAdministrator);
        if (!AdAccess.CanEdit(actor, ad))
        {
            throw new ForbiddenException(ad.Status == AdStatus.InReview ? AdMessages.InReviewReadOnly : AdMessages.NotEditable);
        }

        // O Administrador também edita Em revisão e Publicado, mas só Rascunho e Rejeitado passam a Em revisão (Apêndice A)
        if (AdStatusRules.Find(ad.Status, AdStatus.InReview) is null)
        {
            throw new ConflictException(AdMessages.InvalidTransition(ad.Status, AdStatus.InReview));
        }
    }

    private async Task<IReadOnlyList<AdPending>> PendingAsync(Ad ad, CancellationToken cancellationToken)
    {
        FieldGroup group = FieldGroupRegistry.Default;
        if (ad.CategoryId is { } categoryId)
        {
            CategoryTreeSnapshot snapshot = await categories.GetAsync(cancellationToken);
            group = FieldGroupRegistry.Resolve(snapshot, categoryId) ?? group;
        }

        int photos = await context.AdPhotos.CountAsync(p => p.AdId == ad.Id, cancellationToken);
        return AdSubmissionRules.Pending(ad, group, photos);
    }
}
