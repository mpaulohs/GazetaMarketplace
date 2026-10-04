using GazetaMarketplace.Core.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GazetaMarketplace.Infrastructure.Data.Configurations;

internal sealed class CepCacheConfiguration : IEntityTypeConfiguration<CepCacheEntry>
{
    public void Configure(EntityTypeBuilder<CepCacheEntry> builder)
    {
        builder.ToTable("CepCache", table => table.HasCheckConstraint("CK_CepCache_Cep", "[Cep] NOT LIKE '%[^0-9]%' AND LEN([Cep]) = 8"));
        builder.HasKey(e => e.Cep);

        builder.Property(e => e.Cep).HasColumnType("char(8)").ValueGeneratedNever();
        builder.Property(e => e.City).IsRequired().HasMaxLength(80);
        builder.Property(e => e.Uf).IsRequired().HasColumnType("char(2)");
    }
}
