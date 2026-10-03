using Microsoft.AspNetCore.Identity;

namespace GazetaMarketplace.Infrastructure.Identidade;

/// <summary>Papel da equipe: Administrador ou Redator (<c>GazetaMarketplace.Core.Equipe.Papeis</c>).</summary>
public class PapelIdentity : IdentityRole<int>
{
    public PapelIdentity()
    {
    }

    public PapelIdentity(string name)
        : base(name)
    {
    }
}
