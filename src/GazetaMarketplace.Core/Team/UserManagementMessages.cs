namespace GazetaMarketplace.Core.Team;

/// <summary>Mensagens da gestão de usuários, com o texto exato da SPEC (US-014).</summary>
public static class UserManagementMessages
{
    public const string DuplicateEmail = "Já existe um usuário com este e-mail";

    public const string InvalidEmail = "Informe um e-mail válido";

    public const string NameRequired = "Informe o nome";

    public const string NameTooLong = "O nome pode ter até 100 caracteres";

    public const string RoleRequired = "Escolha o papel";

    public const string PasswordRequired = "Informe a senha provisória";

    public const string CannotDeactivateSelf = "Você não pode desativar a sua própria conta";

    public const string LastAdministrator = "Deve existir ao menos um administrador ativo";

    public const string CannotResetOwnPassword = "Para trocar a sua própria senha, use a recuperação de senha";

    public const string AccountDeactivated = "Reative a conta antes de redefinir a senha";

    public const string NotFound = "Usuário não encontrado";
}
