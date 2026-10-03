using System.Security.Claims;
using System.Threading.Tasks;
using GazetaMarketplace.Infrastructure.Identidade;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Seguranca;

/// <summary>Acrescenta o nome completo ao cookie, para o painel mostrá-lo sem ir ao banco a cada página.</summary>
public sealed class FabricaDeClaimsDaEquipe(
    UserManager<UsuarioIdentity> usuarios, RoleManager<PapelIdentity> papeis, IOptions<IdentityOptions> opcoes)
    : UserClaimsPrincipalFactory<UsuarioIdentity, PapelIdentity>(usuarios, papeis, opcoes)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(UsuarioIdentity usuario)
    {
        ClaimsIdentity identidade = await base.GenerateClaimsAsync(usuario);
        identidade.AddClaim(new Claim(ClaimsDaEquipe.NomeCompleto, usuario.FullName ?? string.Empty));
        return identidade;
    }
}
