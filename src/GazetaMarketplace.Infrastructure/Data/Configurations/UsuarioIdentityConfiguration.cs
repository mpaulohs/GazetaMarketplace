using GazetaMarketplace.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GazetaMarketplace.Infrastructure.Data.Configurations;

internal sealed class UsuarioIdentityConfiguration : IEntityTypeConfiguration<UsuarioIdentity>
{
    public void Configure(EntityTypeBuilder<UsuarioIdentity> builder)
    {
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.MustChangePassword).HasDefaultValue(false);
    }
}
