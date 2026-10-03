using System.Globalization;
using Microsoft.AspNetCore.Identity;

namespace GazetaMarketplace.Web.Seguranca;

/// <summary>Mensagens do Identity em português (política de senha do NFR-07 e e-mail repetido). Os códigos não mudam.</summary>
public sealed class DescritorDeErrosDaEquipe : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Erro(nameof(DefaultError), "Não foi possível concluir a operação. Tente de novo.");

    public override IdentityError PasswordMismatch() => Erro(nameof(PasswordMismatch), "A senha atual não confere.");

    public override IdentityError PasswordTooShort(int length) =>
        Erro(nameof(PasswordTooShort), string.Format(CultureInfo.InvariantCulture, "A senha precisa ter {0} caracteres ou mais.", length));

    public override IdentityError PasswordRequiresUpper() => Erro(nameof(PasswordRequiresUpper), "A senha precisa ter uma letra maiúscula.");

    public override IdentityError PasswordRequiresLower() => Erro(nameof(PasswordRequiresLower), "A senha precisa ter uma letra minúscula.");

    public override IdentityError PasswordRequiresDigit() => Erro(nameof(PasswordRequiresDigit), "A senha precisa ter um número.");

    public override IdentityError PasswordRequiresNonAlphanumeric() => Erro(nameof(PasswordRequiresNonAlphanumeric), "A senha precisa ter um símbolo, como ! ou @.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Erro(nameof(PasswordRequiresUniqueChars), string.Format(CultureInfo.InvariantCulture, "A senha precisa ter ao menos {0} caracteres diferentes.", uniqueChars));

    public override IdentityError InvalidEmail(string email) => Erro(nameof(InvalidEmail), "E-mail em formato inválido.");

    public override IdentityError InvalidUserName(string userName) => Erro(nameof(InvalidUserName), "E-mail em formato inválido.");

    public override IdentityError DuplicateUserName(string userName) => Erro(nameof(DuplicateUserName), "Já existe uma conta com este e-mail.");

    public override IdentityError DuplicateEmail(string email) => Erro(nameof(DuplicateEmail), "Já existe uma conta com este e-mail.");

    public override IdentityError InvalidToken() => Erro(nameof(InvalidToken), "O link não é mais válido.");

    private static IdentityError Erro(string codigo, string descricao) => new() { Code = codigo, Description = descricao };
}
