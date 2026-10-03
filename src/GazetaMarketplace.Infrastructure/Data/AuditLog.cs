using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Interfaces;

namespace GazetaMarketplace.Infrastructure.Data;

/// <inheritdoc cref="IAuditLog"/>
public sealed class AuditLog(AppDbContext context, ICurrentUser currentUser, TimeProvider time) : IAuditLog
{
    public async Task RecordAsync(AuditRecord entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.Action);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.TargetType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.TargetId);

        context.AuditEntries.Add(new AuditEntry
        {
            OccurredAt = time.GetUtcNow().UtcDateTime,
            ActorId = currentUser.UserId,
            Action = entry.Action,
            TargetType = entry.TargetType,
            TargetId = entry.TargetId,
            PreviousValue = entry.PreviousValue,
            NewValue = entry.NewValue,
            Result = entry.Result,
            CorrelationId = currentUser.CorrelationId
        });

        await context.SaveChangesAsync(cancellationToken);
    }
}
