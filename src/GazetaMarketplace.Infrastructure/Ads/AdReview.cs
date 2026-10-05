using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.Infrastructure.Ads;

/// <inheritdoc cref="IAdReview"/>
/// <remarks>
/// A passagem Em revisão → Publicado ou Rejeitado, o <c>RowVersion</c>, a trilha (<c>PublishedBy/At</c>, <c>RejectedBy/At/Reason</c>) e a auditoria são do
/// <see cref="IAdService"/>. Dois Administradores ao mesmo tempo: um passa; o outro perde a corrida (<see cref="ConflictException"/> do <c>RowVersion</c>) ou chega
/// depois e vê o anúncio já decidido. Se o anúncio só foi <b>editado</b> no intervalo (continua Em revisão, o <c>RowVersion</c> mudou), a decisão é refeita uma vez sobre a
/// versão corrigida, como a SPEC pede ("a decisão vale sobre a versão corrigida").
/// </remarks>
public sealed class AdReview(AppDbContext context, IAdService ads, ICategoryTree categories, ISiteSettings settings, ICurrentUser currentUser) : IAdReview
{
    private const int Attempts = 2;

    public async Task<ReviewResult> CheckPublishAsync(int adId, CancellationToken cancellationToken)
    {
        EnsureAdministrator();
        Ad ad = await ads.GetAsync(adId, cancellationToken);
        return await PublishChecksAsync(ad, cancellationToken);
    }

    public async Task<ReviewResult> CheckRejectAsync(int adId, CancellationToken cancellationToken)
    {
        EnsureAdministrator();
        Ad ad = await ads.GetAsync(adId, cancellationToken);
        return ad.Status == AdStatus.InReview ? ReviewResult.Done : Decided(ad);
    }

    public Task<ReviewResult> PublishAsync(int adId, CancellationToken cancellationToken) =>
        DecideAsync(adId, AdStatus.Published, null, cancellationToken);

    public Task<ReviewResult> RejectAsync(int adId, string reason, CancellationToken cancellationToken) =>
        DecideAsync(adId, AdStatus.Rejected, reason, cancellationToken);

    private async Task<ReviewResult> DecideAsync(int adId, byte target, string reason, CancellationToken cancellationToken)
    {
        EnsureAdministrator();
        for (int attempt = 1; ; attempt++)
        {
            // Cada tentativa lê o anúncio de novo: o que vale é o que está gravado agora, não o que a tela mostrava
            Ad ad = await ads.GetAsync(adId, cancellationToken);
            ReviewResult check = target == AdStatus.Published
                ? await PublishChecksAsync(ad, cancellationToken)
                : ad.Status == AdStatus.InReview ? ReviewResult.Done : Decided(ad);
            if (check.Outcome != ReviewOutcome.Done)
            {
                return check;
            }

            try
            {
                await ads.TransitionAsync(adId, target, reason, cancellationToken);
                return ReviewResult.Done;
            }
            catch (ConflictException) when (attempt < Attempts)
            {
                // Perdeu a corrida ou o anúncio foi editado no meio: solta o que o contexto guardava e confere tudo de novo
                context.ChangeTracker.Clear();
            }
            catch (ConflictException)
            {
                context.ChangeTracker.Clear();
                Ad current = await ads.GetAsync(adId, cancellationToken);
                if (current.Status != AdStatus.InReview)
                {
                    return Decided(current);
                }

                throw;
            }
        }
    }

    private async Task<ReviewResult> PublishChecksAsync(Ad ad, CancellationToken cancellationToken)
    {
        if (ad.Status != AdStatus.InReview)
        {
            return Decided(ad);
        }

        if (!await settings.IsPhoneConfiguredAsync(cancellationToken))
        {
            return new ReviewResult(ReviewOutcome.PhoneNotConfigured, Message: AdMessages.PhoneNotConfigured);
        }

        // O Administrador pode ter editado o anúncio em revisão: o site não publica um anúncio que o envio à revisão recusaria
        FieldGroup group = FieldGroupRegistry.Default;
        if (ad.CategoryId is { } categoryId)
        {
            CategoryTreeSnapshot snapshot = await categories.GetAsync(cancellationToken);
            group = FieldGroupRegistry.Resolve(snapshot, categoryId) ?? group;
        }

        int photos = await context.AdPhotos.CountAsync(p => p.AdId == ad.Id, cancellationToken);
        IReadOnlyList<AdPending> pending = AdSubmissionRules.Pending(ad, group, photos);
        return pending.Count > 0 ? new ReviewResult(ReviewOutcome.HasPending, pending) : ReviewResult.Done;
    }

    private ReviewResult Decided(Ad ad)
    {
        int? decider = ad.Status switch
        {
            AdStatus.Published => ad.PublishedById,
            AdStatus.Rejected => ad.RejectedById,
            _ => null
        };
        bool same = decider is not null && decider == currentUser.UserId;
        return new ReviewResult(ReviewOutcome.AlreadyDecided, Message: AdMessages.AlreadyDecided(ad.Status, same));
    }

    private void EnsureAdministrator()
    {
        if (!currentUser.IsAdministrator)
        {
            throw new ForbiddenException(AdMessages.NoPermission);
        }
    }
}
