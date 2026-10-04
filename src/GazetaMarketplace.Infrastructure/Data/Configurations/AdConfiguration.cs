using System.Linq;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GazetaMarketplace.Infrastructure.Data.Configurations;

/// <summary>
/// A tabela <c>Ads</c> (ARCHITECTURE §6.2 e §6.4). As colunas calculadas e os <c>CHECK</c>s usam funções do SQL Server (<c>JSON_VALUE</c>,
/// <c>ISJSON</c>, <c>LEN</c>); em outro provedor (os testes unitários em SQLite) <see cref="RemoveSqlServerOnlyFeatures"/> os tira do modelo, e o
/// esquema real é provado nos testes de integração.
/// </summary>
internal sealed class AdConfiguration : IEntityTypeConfiguration<Ad>
{
    /// <summary>Colunas calculadas persistidas: nome da coluna → caminho no JSON (ADR-002). Os caminhos são as chaves dos campos filtráveis dos grupos.</summary>
    public static readonly (string Column, string Path, string SqlType)[] ComputedColumns =
    [
        (nameof(Ad.VehicleBrandId), "$.brandId", "int"),
        (nameof(Ad.VehicleModelId), "$.modelId", "int"),
        (nameof(Ad.ModelYear), "$.modelYear", "int"),
        (nameof(Ad.Km), "$.km", "int"),
        (nameof(Ad.AreaM2), "$.areaM2", "decimal(12,2)")
    ];

    public void Configure(EntityTypeBuilder<Ad> builder)
    {
        builder.ToTable("Ads", table =>
        {
            table.HasCheckConstraint("CK_Ads_Status", "[Status] BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_Ads_PriceCents", $"[PriceCents] IS NULL OR ([PriceCents] > 0 AND [PriceCents] <= {FieldLimits.MaxMoneyCents})");
            table.HasCheckConstraint("CK_Ads_Cep", "[Cep] IS NULL OR ([Cep] NOT LIKE '%[^0-9]%' AND LEN([Cep]) = 8)");
            table.HasCheckConstraint("CK_Ads_Uf", "[Uf] IS NULL OR ([Uf] NOT LIKE '%[^A-Za-z]%' AND LEN([Uf]) = 2)");
            table.HasCheckConstraint("CK_Ads_Title", "LEN(LTRIM(RTRIM([Title]))) > 0");
            // ISJSON aceita arrays e escalares; as colunas calculadas e o código esperam um objeto
            table.HasCheckConstraint("CK_Ads_Attributes", "ISJSON([Attributes]) = 1 AND LEFT(LTRIM([Attributes]), 1) = '{'");
            table.HasCheckConstraint("CK_Ads_Description", $"[Description] IS NULL OR LEN([Description]) <= {Ad.DescriptionMaxLength}");
        });
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title).IsRequired().HasMaxLength(Ad.TitleMaxLength);
        builder.Property(a => a.Description);
        builder.Property(a => a.Cep).HasColumnType("char(8)");
        builder.Property(a => a.City).HasMaxLength(Ad.CityMaxLength);
        builder.Property(a => a.Uf).HasColumnType("char(2)");
        builder.Property(a => a.Attributes).IsRequired();
        builder.Property(a => a.TitleSearch).HasMaxLength(200);
        builder.Property(a => a.DescriptionSearch);
        builder.Property(a => a.RejectionReason).HasMaxLength(Ad.RejectionReasonMaxLength);

        // TRY_CAST (emenda do ADR-002, 2026-10-03): um valor malformado no JSON vira NULL em vez de derrubar o INSERT
        foreach ((string column, string path, string sqlType) in ComputedColumns)
        {
            builder.Property(column).HasComputedColumnSql($"TRY_CAST(JSON_VALUE([Attributes], '{path}') AS {sqlType})", stored: true);
        }

        builder.Property(a => a.AreaM2).HasColumnType("decimal(12,2)");

        // Nenhuma exclusão em cascata: não existe função de excluir anúncio na v1, e categoria ou usuário com anúncios não podem sumir
        builder.HasOne<Category>().WithMany().HasForeignKey(a => a.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(a => a.AuthorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(a => a.PublishedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(a => a.RejectedById).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.Status, a.CategoryId, a.PublishedAt }).IsDescending(false, false, true).HasDatabaseName("IX_Ads_Status_CategoryId_PublishedAt");
        builder.HasIndex(a => new { a.Status, a.Uf, a.City }).HasDatabaseName("IX_Ads_Status_Uf_City");
        builder.HasIndex(a => new { a.Status, a.PriceCents }).HasFilter("[PriceCents] IS NOT NULL").HasDatabaseName("IX_Ads_Status_PriceCents");
        builder.HasIndex(a => new { a.VehicleBrandId, a.ModelYear }).HasDatabaseName("IX_Ads_VehicleBrandId_ModelYear");
        builder.HasIndex(a => a.Km).HasDatabaseName("IX_Ads_Km");
        builder.HasIndex(a => a.AreaM2).HasDatabaseName("IX_Ads_AreaM2");
        builder.HasIndex(a => new { a.AuthorId, a.Status, a.UpdatedAt }).HasDatabaseName("IX_Ads_AuthorId_Status_UpdatedAt");
        builder.HasIndex(a => a.CategoryId).HasDatabaseName("IX_Ads_CategoryId");
        builder.HasIndex(a => a.PublishedById).HasDatabaseName("IX_Ads_PublishedById");
        builder.HasIndex(a => a.RejectedById).HasDatabaseName("IX_Ads_RejectedById");
    }

    /// <summary>Tira do modelo o que só o SQL Server entende (colunas calculadas por JSON e CHECKs); as colunas calculadas viram colunas comuns, sempre nulas.</summary>
    public static void RemoveSqlServerOnlyFeatures(ModelBuilder modelBuilder)
    {
        IMutableEntityType ad = modelBuilder.Model.FindEntityType(typeof(Ad))!;

        // Os CHECKs de Ads e do cache de CEP usam LEN, ISJSON e LIKE com classes de caracteres do T-SQL
        foreach (IMutableEntityType type in new[] { ad, modelBuilder.Model.FindEntityType(typeof(GazetaMarketplace.Core.Location.CepCacheEntry))! })
        {
            foreach (IMutableCheckConstraint check in type.GetCheckConstraints().ToList())
            {
                type.RemoveCheckConstraint(check.ModelName);
            }
        }

        foreach ((string column, _, _) in ComputedColumns)
        {
            IMutableProperty property = ad.FindProperty(column)!;
            property.SetComputedColumnSql(null);
            property.SetIsStored(null);
            property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
        }
    }
}
