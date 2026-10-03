using GazetaMarketplace.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GazetaMarketplace.Infrastructure.Data.Configurations;

internal sealed class PasswordRecoveryAttemptConfiguration : IEntityTypeConfiguration<PasswordRecoveryAttempt>
{
    public void Configure(EntityTypeBuilder<PasswordRecoveryAttempt> builder)
    {
        builder.ToTable("PasswordRecoveryAttempts");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Email).IsRequired().HasMaxLength(254);
        builder.Property(e => e.Ip).IsRequired().HasMaxLength(45).IsUnicode(false);

        // Limite por e-mail e por IP na última hora; a data sozinha serve ao total diário e à limpeza
        builder.HasIndex(e => new { e.Email, e.RequestedAt }).HasDatabaseName("IX_PasswordRecoveryAttempts_Email_RequestedAt");
        builder.HasIndex(e => new { e.Ip, e.RequestedAt }).HasDatabaseName("IX_PasswordRecoveryAttempts_Ip_RequestedAt");
        builder.HasIndex(e => e.RequestedAt).HasDatabaseName("IX_PasswordRecoveryAttempts_RequestedAt");
    }
}
