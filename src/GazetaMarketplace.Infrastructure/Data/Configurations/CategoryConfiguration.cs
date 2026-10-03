using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Infrastructure.Data.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GazetaMarketplace.Infrastructure.Data.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", table => table.HasCheckConstraint("CK_Categories_NotOwnParent", "[ParentId] IS NULL OR [ParentId] <> [Id]"));
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(CategoryRules.MaxNameLength);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(SlugGenerator.MaxLength).IsUnicode(false);
        builder.Property(c => c.FieldGroup).HasMaxLength(40).IsUnicode(false);

        // Apagar um pai com filhas é recusado pelo banco (a regra de negócio está no serviço, na tarefa 2.6)
        builder.HasOne<Category>().WithMany().HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("UQ_Categories_Slug");
        // Nome único entre irmãs; o NULL do primeiro nível conta como valor igual só no índice filtrado
        builder.HasIndex(c => new { c.ParentId, c.Name }).IsUnique().HasFilter("[ParentId] IS NOT NULL").HasDatabaseName("UQ_Categories_ParentId_Name");
        builder.HasIndex(c => c.Name).IsUnique().HasFilter("[ParentId] IS NULL").HasDatabaseName("UQ_Categories_Name_Root");
        builder.HasIndex(c => new { c.ParentId, c.DisplayOrder }).HasDatabaseName("IX_Categories_ParentId_DisplayOrder");

        builder.HasData(InitialCategories.All);
    }
}
