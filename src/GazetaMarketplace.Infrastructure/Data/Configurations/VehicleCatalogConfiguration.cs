using GazetaMarketplace.Core.VehicleCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GazetaMarketplace.Infrastructure.Data.Configurations;

// Catálogo de veículos (ADR-008). Dado de referência carregado por script: sem auditoria nem rowversion, e os ids vêm da origem (nunca gerados).
// Carros e motos vêm de tabelas separadas que numeram a partir de 1, então toda chave leva o tipo (Kind) e toda chave estrangeira também.
// O EF já cria o índice de cada chave estrangeira (pai + tipo), que serve às consultas filho-por-pai.
internal static class VehicleCatalogColumns
{
    public static void Kind<T>(EntityTypeBuilder<T> builder, string table)
        where T : class
    {
        builder.Property("Kind").IsRequired().HasMaxLength(4).IsUnicode(false);
        builder.Property("Source").IsRequired().HasMaxLength(60).IsUnicode(false);
        builder.ToTable(table, t => t.HasCheckConstraint($"CK_{table}_Kind", "[Kind] IN ('car', 'moto')"));
    }
}

internal sealed class VehicleBrandConfiguration : IEntityTypeConfiguration<VehicleBrand>
{
    public void Configure(EntityTypeBuilder<VehicleBrand> builder)
    {
        VehicleCatalogColumns.Kind(builder, "VehicleBrands");
        builder.HasKey(b => new { b.Id, b.Kind });
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Name).IsRequired().HasMaxLength(150);
    }
}

internal sealed class VehicleModelConfiguration : IEntityTypeConfiguration<VehicleModel>
{
    public void Configure(EntityTypeBuilder<VehicleModel> builder)
    {
        VehicleCatalogColumns.Kind(builder, "VehicleModels");
        builder.HasKey(m => new { m.Id, m.Kind });
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Name).IsRequired().HasMaxLength(150);

        builder.HasOne<VehicleBrand>().WithMany()
            .HasForeignKey(m => new { m.BrandId, m.Kind })
            .HasPrincipalKey(b => new { b.Id, b.Kind })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_VehicleModels_VehicleBrands");
    }
}

internal sealed class VehicleModelYearConfiguration : IEntityTypeConfiguration<VehicleModelYear>
{
    public void Configure(EntityTypeBuilder<VehicleModelYear> builder)
    {
        VehicleCatalogColumns.Kind(builder, "VehicleModelYears");
        builder.HasKey(y => new { y.ModelId, y.Year, y.Kind });

        builder.HasOne<VehicleModel>().WithMany()
            .HasForeignKey(y => new { y.ModelId, y.Kind })
            .HasPrincipalKey(m => new { m.Id, m.Kind })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_VehicleModelYears_VehicleModels");
    }
}

internal sealed class VehicleVersionConfiguration : IEntityTypeConfiguration<VehicleVersion>
{
    public void Configure(EntityTypeBuilder<VehicleVersion> builder)
    {
        VehicleCatalogColumns.Kind(builder, "VehicleVersions");
        builder.HasKey(v => new { v.Id, v.Kind });
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.Name).IsRequired().HasMaxLength(250);

        builder.HasOne<VehicleModelYear>().WithMany()
            .HasForeignKey(v => new { v.ModelId, v.Year, v.Kind })
            .HasPrincipalKey(y => new { y.ModelId, y.Year, y.Kind })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_VehicleVersions_VehicleModelYears");
    }
}
