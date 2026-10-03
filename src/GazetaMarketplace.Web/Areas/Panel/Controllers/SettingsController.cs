using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Web.Areas.Panel.Models;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Areas.Panel.Controllers;

/// <summary>
/// Configurações do site (US-015): hoje, o telefone/WhatsApp de contato. Só o Administrador entra; o Redator cai em "acesso negado".
/// A validação e a auditoria ficam em <see cref="ISiteSettingsManagement"/>; funciona sem JavaScript (formulário comum, redireciona depois de salvar).
/// </summary>
[Authorize(Policy = AccessPolicies.Administrator)]
[Route("painel/configuracoes")]
public sealed class SettingsController(ISiteSettings settings, ISiteSettingsManagement management) : PanelControllerBase
{
    private const string MessageKey = "SettingsMessage";

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await BuildAsync(null, cancellationToken));

    [HttpPost("")]
    public async Task<IActionResult> Save(SettingsViewModel model, CancellationToken cancellationToken)
    {
        SettingsSaveResult result = await management.SavePhoneAsync(model.Phone, cancellationToken);
        if (result.Succeeded)
        {
            TempData[MessageKey] = SiteSettingsMessages.Saved;
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(nameof(SettingsViewModel.Phone), result.Error);
        // O que a pessoa digitou volta como está, para ela corrigir sem redigitar
        return View(nameof(Index), await BuildAsync(model.Phone, cancellationToken));
    }

    private async Task<SettingsViewModel> BuildAsync(string typedPhone, CancellationToken cancellationToken)
    {
        string stored = await settings.GetPhoneAsync(cancellationToken);
        return new SettingsViewModel
        {
            Phone = typedPhone ?? (string.IsNullOrEmpty(stored) ? "" : PhoneNumber.Format(stored)),
            IsPhoneConfigured = !string.IsNullOrEmpty(stored),
            Message = TempData[MessageKey] as string
        };
    }
}
