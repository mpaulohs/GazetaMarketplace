using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GazetaMarketplace.Infrastructure.Identity;

/// <inheritdoc cref="IUserManagement"/>
/// <remarks>
/// Cada operação que muda dados roda numa transação serializável: a contagem de Administradores ativos e a escrita que
/// depende dela não podem ser separadas por outra requisição (dois Administradores se rebaixando ao mesmo tempo).
/// O ator vem de <see cref="ICurrentUser"/>; nenhuma senha vai para o log nem para a auditoria.
/// </remarks>
public sealed class UserManagement(
    UserManager<AppUser> users,
    AppDbContext context,
    IAuditLog audit,
    ICurrentUser currentUser) : IUserManagement
{
    private const int MaxNameLength = 100;

    private const string TargetType = "User";

    /// <summary>Resultado da tarefa mais a decisão de gravar: uma recusa também deixa rastro na auditoria.</summary>
    private readonly record struct Outcome(UserManagementResult Result, bool Commit);

    public async Task<IReadOnlyList<TeamMember>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await context.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Email)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.IsActive,
                Role = context.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new TeamMember(r.Id, r.FullName, r.Email, r.Role, r.IsActive)).ToList();
    }

    public async Task<TeamMember> FindAsync(int id, CancellationToken cancellationToken)
    {
        AppUser user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        string role = await context.UserRoles
            .Where(ur => ur.UserId == id)
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return new TeamMember(user.Id, user.FullName, user.Email, role, user.IsActive);
    }

    public async Task<UserManagementResult> CreateAsync(NewTeamMember request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string name = request.FullName?.Trim();
        string email = request.Email?.Trim();
        List<UserManagementError> errors = [];

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(new UserManagementError(nameof(NewTeamMember.FullName), UserManagementMessages.NameRequired));
        }
        else if (name.Length > MaxNameLength)
        {
            errors.Add(new UserManagementError(nameof(NewTeamMember.FullName), UserManagementMessages.NameTooLong));
        }

        if (!IsValidEmail(email))
        {
            errors.Add(new UserManagementError(nameof(NewTeamMember.Email), UserManagementMessages.InvalidEmail));
        }

        if (!IsKnownRole(request.Role))
        {
            errors.Add(new UserManagementError(nameof(NewTeamMember.Role), UserManagementMessages.RoleRequired));
        }

        if (string.IsNullOrEmpty(request.ProvisionalPassword))
        {
            errors.Add(new UserManagementError(nameof(NewTeamMember.ProvisionalPassword), UserManagementMessages.PasswordRequired));
        }

        if (errors.Count > 0)
        {
            return UserManagementResult.Failure([.. errors]);
        }

        return await RunAsync(async () =>
        {
            if (await users.FindByEmailAsync(email) is not null)
            {
                return new Outcome(UserManagementResult.Failure(nameof(NewTeamMember.Email), UserManagementMessages.DuplicateEmail), false);
            }

            AppUser user = new() { UserName = email, Email = email, FullName = name, IsActive = true, MustChangePassword = true };
            IdentityResult created = await users.CreateAsync(user, request.ProvisionalPassword);
            if (!created.Succeeded)
            {
                return new Outcome(FromIdentity(created, nameof(NewTeamMember.Email)), false);
            }

            IdentityResult added = await users.AddToRoleAsync(user, request.Role);
            if (!added.Succeeded)
            {
                return new Outcome(FromIdentity(added, nameof(NewTeamMember.Role)), false);
            }

            await audit.RecordAsync(
                new AuditRecord("user.create", TargetType, Id(user), AuditResult.Success, null, "role=" + request.Role), cancellationToken);
            return new Outcome(UserManagementResult.Success, true);
        }, cancellationToken);
    }

    public Task<UserManagementResult> ChangeRoleAsync(int id, string role, CancellationToken cancellationToken) =>
        RunAsync(async () =>
        {
            AppUser target = await users.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture));
            if (target is null)
            {
                return NotFound();
            }

            if (!IsKnownRole(role))
            {
                return new Outcome(UserManagementResult.Failure(nameof(NewTeamMember.Role), UserManagementMessages.RoleRequired), false);
            }

            string current = await RoleOfAsync(target);
            if (current == role)
            {
                return new Outcome(UserManagementResult.Success, false);
            }

            // S09: tirar o papel do último Administrador ativo deixaria o painel sem quem o gerencie
            if (current == RoleNames.Administrator && target.IsActive && await ActiveAdministratorsAsync() <= 1)
            {
                return await DenyAsync("user.change_role", target, UserManagementMessages.LastAdministrator, cancellationToken);
            }

            IdentityResult removed = current is null ? IdentityResult.Success : await users.RemoveFromRoleAsync(target, current);
            IdentityResult added = removed.Succeeded ? await users.AddToRoleAsync(target, role) : removed;
            if (!added.Succeeded)
            {
                return new Outcome(FromIdentity(added, nameof(NewTeamMember.Role)), false);
            }

            // O papel vive no cookie: o carimbo novo faz as sessões abertas se renovarem na próxima revalidação
            await users.UpdateSecurityStampAsync(target);
            await audit.RecordAsync(
                new AuditRecord("user.change_role", TargetType, Id(target), AuditResult.Success, "role=" + current, "role=" + role), cancellationToken);
            return new Outcome(UserManagementResult.Success, true);
        }, cancellationToken);

    public Task<UserManagementResult> DeactivateAsync(int id, CancellationToken cancellationToken) =>
        RunAsync(async () =>
        {
            AppUser target = await users.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture));
            if (target is null)
            {
                return NotFound();
            }

            if (currentUser.UserId == target.Id)
            {
                return await DenyAsync("user.deactivate", target, UserManagementMessages.CannotDeactivateSelf, cancellationToken);
            }

            if (!target.IsActive)
            {
                return new Outcome(UserManagementResult.Success, false);
            }

            if (await RoleOfAsync(target) == RoleNames.Administrator && await ActiveAdministratorsAsync() <= 1)
            {
                return await DenyAsync("user.deactivate", target, UserManagementMessages.LastAdministrator, cancellationToken);
            }

            // O carimbo novo, somado à checagem de IsActive na revalidação, encerra as sessões dela em até 5 minutos (NFR-08)
            target.IsActive = false;
            IdentityResult saved = await users.UpdateSecurityStampAsync(target);
            if (!saved.Succeeded)
            {
                return new Outcome(FromIdentity(saved, string.Empty), false);
            }

            await audit.RecordAsync(
                new AuditRecord("user.deactivate", TargetType, Id(target), AuditResult.Success, "active", "inactive"), cancellationToken);
            return new Outcome(UserManagementResult.Success, true);
        }, cancellationToken);

    public Task<UserManagementResult> ReactivateAsync(int id, CancellationToken cancellationToken) =>
        RunAsync(async () =>
        {
            AppUser target = await users.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture));
            if (target is null)
            {
                return NotFound();
            }

            if (target.IsActive)
            {
                return new Outcome(UserManagementResult.Success, false);
            }

            target.IsActive = true;
            await users.SetLockoutEndDateAsync(target, null);
            await users.ResetAccessFailedCountAsync(target);
            IdentityResult saved = await users.UpdateAsync(target);
            if (!saved.Succeeded)
            {
                return new Outcome(FromIdentity(saved, string.Empty), false);
            }

            await audit.RecordAsync(
                new AuditRecord("user.reactivate", TargetType, Id(target), AuditResult.Success, "inactive", "active"), cancellationToken);
            return new Outcome(UserManagementResult.Success, true);
        }, cancellationToken);

    public Task<UserManagementResult> ResetPasswordAsync(int id, string provisionalPassword, CancellationToken cancellationToken) =>
        RunAsync(async () =>
        {
            AppUser target = await users.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture));
            if (target is null)
            {
                return NotFound();
            }

            // Para a própria senha existe a recuperação normal; conta desativada não entra, então não precisa de senha
            if (currentUser.UserId == target.Id)
            {
                return await DenyAsync("user.reset_password", target, UserManagementMessages.CannotResetOwnPassword, cancellationToken);
            }

            if (!target.IsActive)
            {
                return await DenyAsync("user.reset_password", target, UserManagementMessages.AccountDeactivated, cancellationToken);
            }

            if (string.IsNullOrEmpty(provisionalPassword))
            {
                return new Outcome(
                    UserManagementResult.Failure(nameof(NewTeamMember.ProvisionalPassword), UserManagementMessages.PasswordRequired), false);
            }

            // O token valida a política antes de gravar: senha fraca deixa a senha atual intacta (mesmo caminho do primeiro acesso)
            string token = await users.GeneratePasswordResetTokenAsync(target);
            IdentityResult reset = await users.ResetPasswordAsync(target, token, provisionalPassword);
            if (!reset.Succeeded)
            {
                return new Outcome(FromIdentity(reset, nameof(NewTeamMember.ProvisionalPassword)), false);
            }

            // A senha nova também destrava a conta: o Administrador resolveu o acesso da pessoa
            target.MustChangePassword = true;
            await users.SetLockoutEndDateAsync(target, null);
            await users.ResetAccessFailedCountAsync(target);
            IdentityResult saved = await users.UpdateAsync(target);
            if (!saved.Succeeded)
            {
                return new Outcome(FromIdentity(saved, string.Empty), false);
            }

            // Sem valores: a senha nunca vai para a auditoria
            await audit.RecordAsync(new AuditRecord("user.reset_password", TargetType, Id(target)), cancellationToken);
            return new Outcome(UserManagementResult.Success, true);
        }, cancellationToken);

    private static Outcome NotFound() =>
        new(UserManagementResult.Failure(string.Empty, UserManagementMessages.NotFound), false);

    private static string Id(AppUser user) => user.Id.ToString(CultureInfo.InvariantCulture);

    private static bool IsKnownRole(string role) => role is RoleNames.Administrator or RoleNames.Writer;

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Length <= 256 && new EmailAddressAttribute().IsValid(email);

    /// <summary>Recusa de regra: grava o rastro (Negado) e confirma só a auditoria, sem tocar na conta.</summary>
    private async Task<Outcome> DenyAsync(string action, AppUser target, string message, CancellationToken cancellationToken)
    {
        await audit.RecordAsync(new AuditRecord(action, TargetType, Id(target), AuditResult.Denied), cancellationToken);
        return new Outcome(UserManagementResult.Failure(string.Empty, message), true);
    }

    private async Task<string> RoleOfAsync(AppUser user) => (await users.GetRolesAsync(user)).FirstOrDefault();

    private async Task<int> ActiveAdministratorsAsync() =>
        (await users.GetUsersInRoleAsync(RoleNames.Administrator)).Count(u => u.IsActive);

    /// <summary>
    /// Traduz os erros do Identity (mensagens em português do <c>TeamIdentityErrorDescriber</c>), sem repetir a mesma frase:
    /// e-mail repetido chega como nome de usuário e como e-mail.
    /// </summary>
    private static UserManagementResult FromIdentity(IdentityResult result, string defaultField)
    {
        List<UserManagementError> errors = [];
        foreach (IdentityError error in result.Errors)
        {
            string field = error.Code.StartsWith("Password", StringComparison.Ordinal)
                ? nameof(NewTeamMember.ProvisionalPassword)
                : error.Code.Contains("Email", StringComparison.Ordinal) || error.Code.Contains("UserName", StringComparison.Ordinal)
                    ? nameof(NewTeamMember.Email)
                    : defaultField;
            if (!errors.Any(e => e.Field == field && e.Message == error.Description))
            {
                errors.Add(new UserManagementError(field, error.Description));
            }
        }

        return UserManagementResult.Failure([.. errors]);
    }

    private async Task<UserManagementResult> RunAsync(Func<Task<Outcome>> work, CancellationToken cancellationToken)
    {
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Uma nova tentativa da estratégia de retentativa não pode herdar o que a anterior deixou rastreado
            context.ChangeTracker.Clear();
            await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            Outcome outcome = await work();
            if (outcome.Commit)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return outcome.Result;
        });
    }
}
