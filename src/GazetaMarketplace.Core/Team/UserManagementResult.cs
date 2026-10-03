using System.Collections.Generic;
using System.Linq;

namespace GazetaMarketplace.Core.Team;

/// <summary>
/// Uma recusa da gestão de usuários. <c>Field</c> é o nome da propriedade do formulário a que a mensagem se refere
/// (<c>FullName</c>, <c>Email</c>, <c>Role</c>, <c>ProvisionalPassword</c>); vazio = a mensagem vale para a página toda.
/// </summary>
public sealed record UserManagementError(string Field, string Message);

/// <summary>Resultado de uma operação da gestão de usuários: deu certo ou traz as mensagens para mostrar ao Administrador.</summary>
public sealed class UserManagementResult
{
    private UserManagementResult(IReadOnlyList<UserManagementError> errors) => Errors = errors;

    public static UserManagementResult Success { get; } = new([]);

    public bool Succeeded => Errors.Count == 0;

    public IReadOnlyList<UserManagementError> Errors { get; }

    public static UserManagementResult Failure(params UserManagementError[] errors) => new(errors.ToList());

    public static UserManagementResult Failure(string field, string message) => new([new UserManagementError(field, message)]);
}
