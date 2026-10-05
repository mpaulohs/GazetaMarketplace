using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.Infrastructure.Ads;

/// <inheritdoc cref="IAdService"/>
/// <remarks>
/// A mudança de situação e a auditoria vão no mesmo <c>SaveChanges</c> (o <see cref="IAuditLog"/> grava o que estiver pendente no contexto), então
/// ou as duas existem ou nenhuma. Duas pessoas decidindo o mesmo anúncio: o <c>RowVersion</c> deixa passar só uma; a outra recebe
/// <see cref="ConflictException"/> do <see cref="AppDbContext"/>. Recusas não deixam rastro na auditoria.
/// </remarks>
public sealed class AdService(AppDbContext context, ICurrentUser currentUser, IAuditLog audit, TimeProvider time) : IAdService
{
    private const string TargetType = "Ad";

    public async Task<Ad> GetAsync(int id, CancellationToken cancellationToken)
    {
        Ad ad = await FindAsync(id, tracked: false, cancellationToken);
        if (!AdAccess.CanView(Actor(), ad))
        {
            throw new ForbiddenException(AdMessages.NoPermission);
        }

        return ad;
    }

    public async Task<Ad> GetForEditAsync(int id, CancellationToken cancellationToken)
    {
        Ad ad = await FindAsync(id, tracked: true, cancellationToken);
        AdActor actor = Actor();
        if (!AdAccess.CanView(actor, ad))
        {
            throw new ForbiddenException(AdMessages.NoPermission);
        }

        if (!AdAccess.CanEdit(actor, ad))
        {
            throw new ForbiddenException(ad.Status == AdStatus.InReview ? AdMessages.InReviewReadOnly : AdMessages.NotEditable);
        }

        return ad;
    }

    public async Task<Ad> TransitionAsync(int id, byte target, string reason, CancellationToken cancellationToken)
    {
        Ad ad = await FindAsync(id, tracked: true, cancellationToken);
        AdActor actor = Actor();
        if (!AdAccess.CanView(actor, ad))
        {
            throw new ForbiddenException(AdMessages.NoPermission);
        }

        AdTransition transition = AdStatusRules.Find(ad.Status, target)
            ?? throw new ConflictException(AdMessages.InvalidTransition(ad.Status, target));

        if (!AdAccess.CanTransition(actor, ad, transition))
        {
            throw new ForbiddenException(AdMessages.NoPermission);
        }

        byte previous = ad.Status;
        ad.ApplyTransition(target, actor.UserId, time.GetUtcNow().UtcDateTime, reason);

        await audit.RecordAsync(
            new AuditRecord(transition.Action, TargetType, id.ToString(CultureInfo.InvariantCulture), AuditResult.Success, AdStatus.Label(previous), NewAuditValue(ad, target)),
            cancellationToken);
        return ad;
    }

    // A rejeição leva o motivo à auditoria: ao reenviar, o motivo sai do anúncio (ClearRejection) e o histórico fica aqui
    private static string NewAuditValue(Ad ad, byte target) =>
        target == AdStatus.Rejected ? $"{AdStatus.Label(target)} — motivo: {ad.RejectionReason}" : AdStatus.Label(target);

    private AdActor Actor() => new(currentUser.UserId, currentUser.IsAdministrator);

    private async Task<Ad> FindAsync(int id, bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<Ad> query = tracked ? context.Ads : context.Ads.AsNoTracking();
        return await query.SingleOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw new NotFoundException(AdMessages.NotFound);
    }
}
