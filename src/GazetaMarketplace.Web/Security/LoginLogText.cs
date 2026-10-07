using System;
using System.Text.RegularExpressions;

namespace GazetaMarketplace.Web.Security;

/// <summary>
/// O que do campo "e-mail" da tela de entrada pode ir para o log (SC-06). Quem digita a senha no campo de e-mail não pode
/// deixar a senha no arquivo: só um endereço completo e bem formado entra, e sempre mascarado ("a***@dominio"); o resto vira
/// <see cref="InvalidFormat"/>.
/// </summary>
public static partial class LoginLogText
{
    public const string InvalidFormat = "(formato inválido)";

    private const int MaxEmailLength = 254;

    /// <summary>Devolve "m***@dominio" para um e-mail bem formado e <see cref="InvalidFormat"/> para qualquer outra coisa.</summary>
    public static string EmailOrInvalid(string typed)
    {
        if (string.IsNullOrEmpty(typed) || typed.Length > MaxEmailLength)
        {
            return InvalidFormat;
        }

        Match match = WellFormed().Match(typed.Trim());
        return match.Success ? match.Groups[1].Value + "***@" + match.Groups[2].Value : InvalidFormat;
    }

    // Parte local e domínio com ponto e sufixo só de letras: "Senha@123", "a@b" e "@@@@" não casam
    [GeneratedRegex(@"^([A-Za-z0-9])[A-Za-z0-9._%+\-]*@([A-Za-z0-9\-]+(?:\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,})$", RegexOptions.None, matchTimeoutMilliseconds: 50)]
    private static partial Regex WellFormed();
}
