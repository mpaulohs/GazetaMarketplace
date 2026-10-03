using System.Security.Claims;
using System.Threading.Tasks;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Security;

/// <summary>Acrescenta ao cookie o nome completo e se a senha ainda é provisória, para o painel não ir ao banco a cada página.</summary>
public sealed class TeamClaimsPrincipalFactory(
    UserManager<AppUser> users, RoleManager<AppRole> roles, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<AppUser, AppRole>(users, roles, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        ClaimsIdentity identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(TeamClaims.FullName, user.FullName ?? string.Empty));
        identity.AddClaim(new Claim(TeamClaims.MustChangePassword, user.MustChangePassword ? "1" : "0"));
        return identity;
    }
}
