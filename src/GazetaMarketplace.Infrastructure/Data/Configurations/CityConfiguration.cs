using GazetaMarketplace.Core.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GazetaMarketplace.Infrastructure.Data.Configurations;

internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");
        builder.HasKey(c => c.IbgeCode);

        // O código vem do IBGE, nunca é gerado
        builder.Property(c => c.IbgeCode).ValueGeneratedNever();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(80);
        builder.Property(c => c.Uf).IsRequired().HasColumnType("char(2)");
        builder.Property(c => c.NameSearch).IsRequired().HasMaxLength(120);

        // Dentro de uma UF não há dois municípios com o mesmo nome (comparado sem acento nem maiúscula)
        builder.HasIndex(c => new { c.Uf, c.NameSearch }).IsUnique().HasDatabaseName("UQ_Cities_Uf_NameSearch");
    }
}
