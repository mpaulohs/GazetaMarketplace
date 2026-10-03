using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Areas.Panel.Models;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GazetaMarketplace.Web.Areas.Panel.Controllers;

/// <summary>
/// Gestão das contas da equipe (US-014). Só o Administrador entra aqui; quem é Redator cai em "acesso negado" (US-006-S10).
/// As regras (último Administrador, desativar a si mesmo, auditoria) ficam em <see cref="IUserManagement"/>: a tela só mostra o resultado.
/// Tudo funciona sem JavaScript: confirmar a desativação e redefinir a senha são páginas, não janelas.
/// </summary>
[Authorize(Policy = AccessPolicies.Administrator)]
[Route("painel/usuarios")]
public sealed class UsersController(IUserManagement management, ICurrentUser currentUser) : PanelControllerBase
{
    private const string MessageKey = "UsersMessage";

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new UsersIndexViewModel
        {
            Members = await management.ListAsync(cancellationToken),
            CurrentUserId = currentUser.UserId ?? 0,
            Message = TempData[MessageKey] as string
        });

    [HttpGet("novo")]
    public IActionResult New() => View(new NewUserViewModel());

    [HttpPost("novo")]
    public async Task<IActionResult> New(NewUserViewModel model, CancellationToken cancellationToken)
    {
        UserManagementResult result = await management.CreateAsync(
            new NewTeamMember(model.FullName, model.Email, model.Role, model.ProvisionalPassword), cancellationToken);
        if (!result.Succeeded)
        {
            model.ProvisionalPassword = "";
            return Failure(result, model);
        }

        return Saved($"Usuário {model.FullName.Trim()} criado.");
    }

    [HttpGet("{id:int}/editar")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        TeamMember member = await management.FindAsync(id, cancellationToken);
        return member is null ? NotFound() : View(ToEdit(member));
    }

    [HttpPost("{id:int}/editar")]
    public async Task<IActionResult> Edit(int id, EditUserViewModel model, CancellationToken cancellationToken)
    {
        TeamMember member = await management.FindAsync(id, cancellationToken);
        if (member is null)
        {
            return NotFound();
        }

        UserManagementResult result = await management.ChangeRoleAsync(id, model.Role, cancellationToken);
        if (!result.Succeeded)
        {
            // Nome e e-mail não vêm do formulário: voltam do cadastro, para a tela não perdê-los
            return Failure(result, ToEdit(member, model.Role));
        }

        return Saved($"Papel de {member.FullName} alterado para {model.Role}.");
    }

    [HttpGet("{id:int}/desativar")]
    public async Task<IActionResult> ConfirmDeactivation(int id, CancellationToken cancellationToken)
    {
        TeamMember member = await management.FindAsync(id, cancellationToken);
        return member is null ? NotFound() : View(new ConfirmDeactivationViewModel { Id = member.Id, FullName = member.FullName });
    }

    [HttpPost("{id:int}/desativar")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        TeamMember member = await management.FindAsync(id, cancellationToken);
        if (member is null)
        {
            return NotFound();
        }

        UserManagementResult result = await management.DeactivateAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return Failure(result, new ConfirmDeactivationViewModel { Id = member.Id, FullName = member.FullName }, nameof(ConfirmDeactivation));
        }

        return Saved($"A conta de {member.FullName} foi desativada.");
    }

    [HttpPost("{id:int}/reativar")]
    public async Task<IActionResult> Reactivate(int id, CancellationToken cancellationToken)
    {
        TeamMember member = await management.FindAsync(id, cancellationToken);
        if (member is null)
        {
            return NotFound();
        }

        UserManagementResult result = await management.ReactivateAsync(id, cancellationToken);
        return result.Succeeded ? Saved($"A conta de {member.FullName} foi reativada.") : Saved(result.Errors[0].Message);
    }

    [HttpGet("{id:int}/redefinir-senha")]
    public async Task<IActionResult> ResetPassword(int id, CancellationToken cancellationToken)
    {
        TeamMember member = await management.FindAsync(id, cancellationToken);
        return member is null ? NotFound() : View(new ResetPasswordViewModel { Id = member.Id, FullName = member.FullName });
    }

    [HttpPost("{id:int}/redefinir-senha")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        TeamMember member = await management.FindAsync(id, cancellationToken);
        if (member is null)
        {
            return NotFound();
        }

        UserManagementResult result = await management.ResetPasswordAsync(id, model.ProvisionalPassword, cancellationToken);
        if (!result.Succeeded)
        {
            return Failure(result, new ResetPasswordViewModel { Id = member.Id, FullName = member.FullName });
        }

        return Saved($"Senha de {member.FullName} redefinida. Informe a senha provisória a ela fora do sistema.");
    }

    private static EditUserViewModel ToEdit(TeamMember member, string role = null) =>
        new() { Id = member.Id, FullName = member.FullName, Email = member.Email, Role = role ?? member.Role };

    private IActionResult Saved(string message)
    {
        TempData[MessageKey] = message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Mostra de novo o formulário, com cada mensagem no campo dela; as sem campo vão para o aviso do topo.</summary>
    private IActionResult Failure(UserManagementResult result, object model, string view = null)
    {
        foreach (UserManagementError error in result.Errors)
        {
            ModelState.AddModelError(error.Field, error.Message);
        }

        return view is null ? View(model) : View(view, model);
    }
}
