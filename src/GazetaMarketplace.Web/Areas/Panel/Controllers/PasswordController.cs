using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Web.Areas.Panel.Models;
using GazetaMarketplace.Web.Navigation;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Areas.Panel.Controllers;

/// <summary>
/// "Defina sua nova senha" (US-006-S09). Não herda de <see cref="PanelControllerBase"/> de propósito: aquele filtro
/// leva para esta própria tela e a pessoa ficaria presa num redirecionamento sem fim.
/// </summary>
[Area("Panel")]
[Authorize(Policy = AccessPolicies.Writer)]
[Route("painel")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class PasswordController(
    UserManager<AppUser> users,
    SignInManager<AppUser> access,
    ILogger<PasswordController> log) : Controller
{
    public const string SameAsProvisionalMessage = "A nova senha precisa ser diferente da provisória";

    [HttpGet("definir-senha")]
    public async Task<IActionResult> SetPassword()
    {
        AppUser user = await users.GetUserAsync(User);
        return user is { MustChangePassword: true } ? View(new SetPasswordViewModel()) : LocalRedirect(HomePage());
    }

    [HttpPost("definir-senha")]
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
        AppUser user = await users.GetUserAsync(User);
        if (user is not { MustChangePassword: true })
        {
            return LocalRedirect(HomePage());
        }

        string newPassword = model.NewPassword;
        model.NewPassword = null;
        model.ConfirmPassword = null; // nenhuma senha volta para a tela

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await users.CheckPasswordAsync(user, newPassword))
        {
            ModelState.AddModelError(nameof(SetPasswordViewModel.NewPassword), SameAsProvisionalMessage);
            return View(model);
        }

        // Atômico: o token valida a política antes de gravar, então uma senha fraca deixa a provisória intacta
        string token = await users.GeneratePasswordResetTokenAsync(user);
        IdentityResult change = await users.ResetPasswordAsync(user, token, newPassword);
        if (!change.Succeeded)
        {
            foreach (IdentityError error in change.Errors)
            {
                ModelState.AddModelError(nameof(SetPasswordViewModel.NewPassword), error.Description);
            }

            log.LogWarning("Troca de senha recusada para o usuário {UserId}: {Errors}", user.Id, string.Join(", ", change.Errors.Select(e => e.Code)));
            return View(model);
        }

        user.MustChangePassword = false;
        await users.UpdateAsync(user);

        // O carimbo de segurança mudou: emite um cookie novo, já sem o aviso de senha provisória
        await access.RefreshSignInAsync(user);
        log.LogInformation("Usuário {UserId} definiu a nova senha no primeiro acesso", user.Id);
        return LocalRedirect(HomePage());
    }

    private string HomePage() => User.IsInRole(RoleNames.Administrator) ? PanelRoutes.ReviewQueue : PanelRoutes.Ads;
}
