using GazetaMarketplace.Core.Ads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GazetaMarketplace.Infrastructure.Data.Configurations;

internal sealed class AdPhotoConfiguration : IEntityTypeConfiguration<AdPhoto>
{
    public void Configure(EntityTypeBuilder<AdPhoto> builder)
    {
        builder.ToTable("AdPhotos", table =>
        {
            table.HasCheckConstraint("CK_AdPhotos_SortOrder", "[SortOrder] >= 0");
            table.HasCheckConstraint("CK_AdPhotos_Size", "[Width] > 0 AND [Height] > 0 AND [SizeBytes] > 0");
        });
        builder.HasKey(p => p.Id);

        // Nomes gerados, nunca o enviado pelo usuário (RC-4): ASCII, sem caminho
        builder.Property(p => p.StorageKey).IsRequired().HasMaxLength(100).IsUnicode(false);
        builder.Property(p => p.OriginalKey).HasMaxLength(260).IsUnicode(false);

        // Sem exclusão em cascata: apagar um anúncio (que a v1 não faz) deixaria arquivos órfãos no disco
        builder.HasOne<Ad>().WithMany().HasForeignKey(p => p.AdId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.AdId, p.SortOrder }).HasDatabaseName("IX_AdPhotos_AdId_SortOrder");
        builder.HasIndex(p => p.StorageKey).IsUnique().HasDatabaseName("UQ_AdPhotos_StorageKey");
    }
}
