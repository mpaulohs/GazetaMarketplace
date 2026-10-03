using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Areas.Painel.Models;
using GazetaMarketplace.Web.Navigation;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Areas.Painel.Controllers;

/// <summary>
/// "Defina sua nova senha" (US-006-S09). Não herda de <see cref="PainelControllerBase"/> de propósito: aquele filtro
/// leva para esta própria tela e a pessoa ficaria presa num redirecionamento sem fim.
/// </summary>
[Area("Painel")]
[Authorize(Policy = PoliticasDeAcesso.Writer)]
[Route("painel")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class SenhaController(
    UserManager<UsuarioIdentity> users,
    SignInManager<UsuarioIdentity> acesso,
    ILogger<SenhaController> log) : Controller
{
    public const string MensagemIgualAProvisoria = "A nova senha precisa ser diferente da provisória";

    [HttpGet("definir-senha")]
    public async Task<IActionResult> SetPassword()
    {
        UsuarioIdentity user = await users.GetUserAsync(User);
        return user is { MustChangePassword: true } ? View(new DefinirSenhaViewModel()) : LocalRedirect(PaginaInicial());
    }

    [HttpPost("definir-senha")]
    public async Task<IActionResult> SetPassword(DefinirSenhaViewModel modelo)
    {
        UsuarioIdentity user = await users.GetUserAsync(User);
        if (user is not { MustChangePassword: true })
        {
            return LocalRedirect(PaginaInicial());
        }

        string nova = modelo.NovaSenha;
        modelo.NovaSenha = null;
        modelo.ConfirmarSenha = null; // nenhuma senha volta para a tela

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        if (await users.CheckPasswordAsync(user, nova))
        {
            ModelState.AddModelError(nameof(DefinirSenhaViewModel.NovaSenha), MensagemIgualAProvisoria);
            return View(modelo);
        }

        // Atômico: o token valida a política antes de gravar, então uma senha fraca deixa a provisória intacta
        string token = await users.GeneratePasswordResetTokenAsync(user);
        IdentityResult troca = await users.ResetPasswordAsync(user, token, nova);
        if (!troca.Succeeded)
        {
            foreach (IdentityError error in troca.Errors)
            {
                ModelState.AddModelError(nameof(DefinirSenhaViewModel.NovaSenha), error.Description);
            }

            log.LogWarning("Troca de senha recusada para o usuário {UsuarioId}: {Erros}", user.Id, string.Join(", ", troca.Errors.Select(e => e.Code)));
            return View(modelo);
        }

        user.MustChangePassword = false;
        await users.UpdateAsync(user);

        // O carimbo de segurança mudou: emite um cookie novo, já sem o aviso de senha provisória
        await acesso.RefreshSignInAsync(user);
        log.LogInformation("Usuário {UsuarioId} definiu a nova senha no primeiro acesso", user.Id);
        return LocalRedirect(PaginaInicial());
    }

    private string PaginaInicial() => User.IsInRole(RoleNames.Administrator) ? PanelRoutes.ReviewQueue : PanelRoutes.Ads;
}
