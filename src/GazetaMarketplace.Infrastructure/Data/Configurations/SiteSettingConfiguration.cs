using GazetaMarketplace.Core.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GazetaMarketplace.Infrastructure.Data.Configurations;

internal sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.ToTable("SiteSettings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key).IsRequired().HasMaxLength(100).IsUnicode(false);
        builder.Property(s => s.Value).IsRequired().HasMaxLength(500);

        // Uma linha por configuração; o índice único também impede duas gravações iniciais ao mesmo tempo
        builder.HasIndex(s => s.Key).IsUnique().HasDatabaseName("UQ_SiteSettings_Key");
    }
}
