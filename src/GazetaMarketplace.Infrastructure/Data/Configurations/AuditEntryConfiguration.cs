using GazetaMarketplace.Core.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GazetaMarketplace.Infrastructure.Data.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Action).IsRequired().HasMaxLength(60).IsUnicode(false);
        builder.Property(e => e.TargetType).IsRequired().HasMaxLength(60).IsUnicode(false);
        builder.Property(e => e.TargetId).IsRequired().HasMaxLength(64).IsUnicode(false);
        builder.Property(e => e.PreviousValue).HasMaxLength(2000);
        builder.Property(e => e.NewValue).HasMaxLength(2000);
        builder.Property(e => e.Result).HasConversion<byte>();
        builder.Property(e => e.CorrelationId).HasMaxLength(64).IsUnicode(false);

        builder.HasIndex(e => new { e.TargetType, e.TargetId }).HasDatabaseName("IX_AuditEntries_TargetType_TargetId");
        builder.HasIndex(e => new { e.ActorId, e.OccurredAt }).HasDatabaseName("IX_AuditEntries_ActorId_OccurredAt");
        builder.HasIndex(e => e.OccurredAt).HasDatabaseName("IX_AuditEntries_OccurredAt");
    }
}
