using System.Security.Claims;
using System.Threading.Tasks;
using GazetaMarketplace.Infrastructure.Identidade;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Seguranca;

/// <summary>
/// Conta desativada não entra (S05) e perde a sessão na próxima revalidação do cookie (NFR-08),
/// mesmo que quem desativou esqueça de trocar o carimbo de segurança.
/// </summary>
public sealed class SignInManagerDaEquipe(
    UserManager<UsuarioIdentity> usuarios,
    IHttpContextAccessor contexto,
    IUserClaimsPrincipalFactory<UsuarioIdentity> fabricaDeClaims,
    IOptions<IdentityOptions> opcoes,
    ILogger<SignInManager<UsuarioIdentity>> log,
    IAuthenticationSchemeProvider esquemas,
    IUserConfirmation<UsuarioIdentity> confirmacao)
    : SignInManager<UsuarioIdentity>(usuarios, contexto, fabricaDeClaims, opcoes, log, esquemas, confirmacao)
{
    public override async Task<bool> CanSignInAsync(UsuarioIdentity usuario) =>
        usuario.IsActive && await base.CanSignInAsync(usuario);

    public override async Task<UsuarioIdentity> ValidateSecurityStampAsync(ClaimsPrincipal principal)
    {
        UsuarioIdentity usuario = await base.ValidateSecurityStampAsync(principal);
        return usuario is { IsActive: true } ? usuario : null;
    }
}
