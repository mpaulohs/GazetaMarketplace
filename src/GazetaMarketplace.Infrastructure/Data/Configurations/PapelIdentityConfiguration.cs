using GazetaMarketplace.Core.Equipe;
using GazetaMarketplace.Infrastructure.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GazetaMarketplace.Infrastructure.Data.Configurations;

/// <summary>Os dois papéis são dados fixos: entram pela migration (e pelo script), sem inicializador na partida.</summary>
internal sealed class PapelIdentityConfiguration : IEntityTypeConfiguration<PapelIdentity>
{
    public const int IdAdministrador = 1;

    public const int IdRedator = 2;

    public void Configure(EntityTypeBuilder<PapelIdentity> builder)
    {
        builder.HasData(
            new PapelIdentity
            {
                Id = IdAdministrador,
                Name = Papeis.Administrador,
                NormalizedName = Papeis.Administrador.ToUpperInvariant(),
                ConcurrencyStamp = "5c0b1d2a-8f7e-4a39-9d41-0a1e6c3b7f01"
            },
            new PapelIdentity
            {
                Id = IdRedator,
                Name = Papeis.Redator,
                NormalizedName = Papeis.Redator.ToUpperInvariant(),
                ConcurrencyStamp = "9e4a6f3c-2b15-4d87-a6c0-7d2f8e1b5a02"
            });
    }
}
