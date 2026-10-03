using System.Security.Claims;
using System.Threading.Tasks;
using GazetaMarketplace.Infrastructure.Identidade;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Security;

/// <summary>Acrescenta ao cookie o nome completo e se a senha ainda é provisória, para o painel não ir ao banco a cada página.</summary>
public sealed class FabricaDeClaimsDaEquipe(
    UserManager<UsuarioIdentity> users, RoleManager<PapelIdentity> papeis, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<UsuarioIdentity, PapelIdentity>(users, papeis, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(UsuarioIdentity user)
    {
        ClaimsIdentity identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(ClaimsDaEquipe.NomeCompleto, user.FullName ?? string.Empty));
        identity.AddClaim(new Claim(ClaimsDaEquipe.DeveTrocarSenha, user.MustChangePassword ? "1" : "0"));
        return identity;
    }
}
