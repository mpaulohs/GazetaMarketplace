using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;

namespace GazetaMarketplace.Infrastructure.Ads;

/// <inheritdoc cref="IAdTakedown"/>
/// <remarks>
/// Como a decisão da revisão (<see cref="AdReview"/>): cada tentativa lê o anúncio de novo, confere a situação e passa pelo <see cref="IAdService"/>. Se o
/// <c>RowVersion</c> mudou no meio (outro Administrador agiu, ou o anúncio foi editado), a conferência é refeita uma vez; se o anúncio já saiu da situação
/// esperada, o resultado é <see cref="TakedownOutcome.AlreadyDecided"/> com a frase da situação, não um erro.
/// </remarks>
public sealed class AdTakedown(AppDbContext context, IAdService ads, ICurrentUser currentUser) : IAdTakedown
{
    private const int Attempts = 2;

    public async Task<TakedownResult> CheckUnpublishAsync(int adId, CancellationToken cancellationToken)
    {
        EnsureAdministrator();
        return Check(await ads.GetAsync(adId, cancellationToken), AdStatus.Draft);
    }

    public async Task<TakedownResult> CheckArchiveAsync(int adId, CancellationToken cancellationToken)
    {
        EnsureAdministrator();
        return Check(await ads.GetAsync(adId, cancellationToken), AdStatus.Archived);
    }

    public Task<TakedownResult> UnpublishAsync(int adId, CancellationToken cancellationToken) => DecideAsync(adId, AdStatus.Draft, cancellationToken);

    public Task<TakedownResult> ArchiveAsync(int adId, CancellationToken cancellationToken) => DecideAsync(adId, AdStatus.Archived, cancellationToken);

    private async Task<TakedownResult> DecideAsync(int adId, byte target, CancellationToken cancellationToken)
    {
        EnsureAdministrator();
        for (int attempt = 1; ; attempt++)
        {
            Ad ad = await ads.GetAsync(adId, cancellationToken);
            TakedownResult check = Check(ad, target);
            if (check.Outcome != TakedownOutcome.Done)
            {
                return check;
            }

            try
            {
                await ads.TransitionAsync(adId, target, null, cancellationToken);
                return TakedownResult.Done;
            }
            catch (ConflictException) when (attempt < Attempts)
            {
                context.ChangeTracker.Clear();
            }
            catch (ConflictException)
            {
                context.ChangeTracker.Clear();
                TakedownResult current = Check(await ads.GetAsync(adId, cancellationToken), target);
                if (current.Outcome != TakedownOutcome.Done)
                {
                    return current;
                }

                throw;
            }
        }
    }

    // Despublicar só vale para Publicado; arquivar vale para tudo que ainda não está Arquivado
    private static TakedownResult Check(Ad ad, byte target) => target switch
    {
        AdStatus.Draft when ad.Status != AdStatus.Published => new(TakedownOutcome.AlreadyDecided, AdMessages.NoLongerPublished(ad.Status)),
        AdStatus.Archived when ad.Status == AdStatus.Archived => new(TakedownOutcome.AlreadyDecided, AdMessages.AlreadyArchived),
        _ => TakedownResult.Done
    };

    private void EnsureAdministrator()
    {
        if (!currentUser.IsAdministrator)
        {
            throw new ForbiddenException(AdMessages.NoPermission);
        }
    }
}
