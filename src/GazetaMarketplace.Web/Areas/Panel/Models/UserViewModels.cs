using System.Collections.Generic;
using GazetaMarketplace.Core.Team;

namespace GazetaMarketplace.Web.Areas.Panel.Models;

/// <summary>Lista de usuários da equipe (US-014). <c>Message</c> é o aviso do resultado da última ação.</summary>
public sealed class UsersIndexViewModel
{
    public IReadOnlyList<TeamMember> Members { get; init; } = [];

    /// <summary>Id de quem está logado: a linha dele não oferece "Redefinir senha".</summary>
    public int CurrentUserId { get; init; }

    public string Message { get; init; }
}

/// <summary>Formulário de novo usuário. A senha nunca volta para a tela.</summary>
public sealed class NewUserViewModel
{
    public string FullName { get; set; }

    public string Email { get; set; }

    public string Role { get; set; } = RoleNames.Writer;

    public string ProvisionalPassword { get; set; }
}

/// <summary>Mudança de papel: nome e e-mail só aparecem, não se editam nesta versão.</summary>
public sealed class EditUserViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; }

    public string Email { get; set; }

    public string Role { get; set; }
}

/// <summary>Pergunta "Desativar fulano?" antes de desativar.</summary>
public sealed class ConfirmDeactivationViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; }
}

/// <summary>Redefinição da senha de outra pessoa: o Administrador digita a senha provisória.</summary>
public sealed class ResetPasswordViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; }

    public string ProvisionalPassword { get; set; }
}
