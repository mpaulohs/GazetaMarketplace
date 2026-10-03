using Microsoft.AspNetCore.Identity;

namespace GazetaMarketplace.Infrastructure.Identity;

/// <summary>Papel da equipe: Administrador ou Redator (<c>GazetaMarketplace.Core.Equipe.Papeis</c>).</summary>
public class AppRole : IdentityRole<int>
{
    public AppRole()
    {
    }

    public AppRole(string name)
        : base(name)
    {
    }
}
