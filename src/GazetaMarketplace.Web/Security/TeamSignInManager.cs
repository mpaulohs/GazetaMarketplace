using System.Security.Claims;
using System.Threading.Tasks;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Security;

/// <summary>
/// Conta desativada não entra (S05) e perde a sessão na próxima revalidação do cookie (NFR-08),
/// mesmo que quem desativou esqueça de trocar o carimbo de segurança.
/// </summary>
public sealed class TeamSignInManager(
    UserManager<AppUser> users,
    IHttpContextAccessor context,
    IUserClaimsPrincipalFactory<AppUser> claimsFactory,
    IOptions<IdentityOptions> options,
    ILogger<SignInManager<AppUser>> log,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<AppUser> confirmation)
    : SignInManager<AppUser>(users, context, claimsFactory, options, log, schemes, confirmation)
{
    public override async Task<bool> CanSignInAsync(AppUser user) =>
        user.IsActive && await base.CanSignInAsync(user);

    public override async Task<AppUser> ValidateSecurityStampAsync(ClaimsPrincipal principal)
    {
        AppUser user = await base.ValidateSecurityStampAsync(principal);
        return user is { IsActive: true } ? user : null;
    }
}
