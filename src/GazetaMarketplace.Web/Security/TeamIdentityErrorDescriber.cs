using System.Globalization;
using Microsoft.AspNetCore.Identity;

namespace GazetaMarketplace.Web.Security;

/// <summary>Mensagens do Identity em português (política de senha do NFR-07 e e-mail repetido, com o texto da SPEC). Os códigos não mudam.</summary>
public sealed class TeamIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Error(nameof(DefaultError), "Não foi possível concluir a operação. Tente de novo.");

    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "A senha atual não confere.");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), string.Format(CultureInfo.InvariantCulture, "A senha precisa ter {0} caracteres ou mais.", length));

    public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper), "A senha precisa ter uma letra maiúscula.");

    public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower), "A senha precisa ter uma letra minúscula.");

    public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit), "A senha precisa ter um número.");

    public override IdentityError PasswordRequiresNonAlphanumeric() => Error(nameof(PasswordRequiresNonAlphanumeric), "A senha precisa ter um símbolo, como ! ou @.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Error(nameof(PasswordRequiresUniqueChars), string.Format(CultureInfo.InvariantCulture, "A senha precisa ter ao menos {0} caracteres diferentes.", uniqueChars));

    public override IdentityError InvalidEmail(string email) => Error(nameof(InvalidEmail), "Informe um e-mail válido");

    public override IdentityError InvalidUserName(string userName) => Error(nameof(InvalidUserName), "Informe um e-mail válido");

    public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), "Já existe um usuário com este e-mail");

    public override IdentityError DuplicateEmail(string email) => Error(nameof(DuplicateEmail), "Já existe um usuário com este e-mail");

    public override IdentityError InvalidToken() => Error(nameof(InvalidToken), "O link não é mais válido.");

    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };
}
