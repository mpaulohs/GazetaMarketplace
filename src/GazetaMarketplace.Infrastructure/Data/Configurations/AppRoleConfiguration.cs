using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GazetaMarketplace.Infrastructure.Data.Configurations;

/// <summary>Os dois papéis são dados fixos: entram pela migration (e pelo script), sem inicializador na partida.</summary>
internal sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public const int AdministratorId = 1;

    public const int WriterId = 2;

    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.HasData(
            new AppRole
            {
                Id = AdministratorId,
                Name = RoleNames.Administrator,
                NormalizedName = RoleNames.Administrator.ToUpperInvariant(),
                ConcurrencyStamp = "5c0b1d2a-8f7e-4a39-9d41-0a1e6c3b7f01"
            },
            new AppRole
            {
                Id = WriterId,
                Name = RoleNames.Writer,
                NormalizedName = RoleNames.Writer.ToUpperInvariant(),
                ConcurrencyStamp = "9e4a6f3c-2b15-4d87-a6c0-7d2f8e1b5a02"
            });
    }
}
