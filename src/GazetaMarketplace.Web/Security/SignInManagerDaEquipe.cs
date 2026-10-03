using System.Security.Claims;
using System.Threading.Tasks;
using GazetaMarketplace.Infrastructure.Identidade;
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
public sealed class SignInManagerDaEquipe(
    UserManager<UsuarioIdentity> users,
    IHttpContextAccessor context,
    IUserClaimsPrincipalFactory<UsuarioIdentity> fabricaDeClaims,
    IOptions<IdentityOptions> options,
    ILogger<SignInManager<UsuarioIdentity>> log,
    IAuthenticationSchemeProvider esquemas,
    IUserConfirmation<UsuarioIdentity> confirmacao)
    : SignInManager<UsuarioIdentity>(users, context, fabricaDeClaims, options, log, esquemas, confirmacao)
{
    public override async Task<bool> CanSignInAsync(UsuarioIdentity user) =>
        user.IsActive && await base.CanSignInAsync(user);

    public override async Task<UsuarioIdentity> ValidateSecurityStampAsync(ClaimsPrincipal principal)
    {
        UsuarioIdentity user = await base.ValidateSecurityStampAsync(principal);
        return user is { IsActive: true } ? user : null;
    }
}
