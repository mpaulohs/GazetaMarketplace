using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Team;

/// <summary>Situação do link de redefinição de senha que a pessoa abriu.</summary>
public enum RecoveryLinkState
{
    /// <summary>Dentro de 1 hora e ainda não usado.</summary>
    Valid = 0,

    /// <summary>Passou de 1 hora (US-007-S04).</summary>
    Expired = 1,

    /// <summary>A senha já foi trocada depois que o link foi gerado (US-007-S05).</summary>
    Used = 2,

    /// <summary>Não é um link nosso, ou a conta não existe mais ou foi desativada.</summary>
    Invalid = 3
}

/// <summary>Resultado de definir a nova senha pelo link: deu certo, o link não vale mais ou a senha foi recusada.</summary>
public sealed record PasswordResetResult(bool Succeeded, RecoveryLinkState LinkState, IReadOnlyList<string> PasswordErrors)
{
    public static PasswordResetResult Success { get; } = new(true, RecoveryLinkState.Valid, []);

    public static PasswordResetResult BadLink(RecoveryLinkState state) => new(false, state, []);

    public static PasswordResetResult WeakPassword(IReadOnlyList<string> errors) => new(false, RecoveryLinkState.Valid, errors);
}

/// <summary>Textos da recuperação de senha (US-007), como estão na SPEC.</summary>
public static class PasswordRecoveryMessages
{
    public const string RequestAccepted = "Se o e-mail estiver cadastrado, enviaremos as instruções";

    public const string LinkExpired = "Este link expirou";

    public const string LinkUsed = "Este link já foi usado";

    public const string LinkInvalid = "Este link não é válido";

    public const string PasswordsDoNotMatch = "As senhas não coincidem";

    public const string PasswordChanged = "Senha alterada. Entre com a nova senha.";

    public const string EmailSubject = "Redefinição de senha — GazetaMarketplace";

    public const string LockoutNoticeSubject = "Tentativas de entrar na sua conta — GazetaMarketplace";
}

/// <summary>Recuperar a senha esquecida por e-mail (US-007).</summary>
public interface IPasswordRecovery
{
    /// <summary>
    /// Registra o pedido, aplica os limites por e-mail (3 por hora) e por IP (10 por hora) e, dentro deles, deixa o envio
    /// na fila. Faz o mesmo trabalho e termina da mesma forma exista a conta ou não (RC-13); quem chama mostra sempre a mesma resposta.
    /// </summary>
    /// <param name="email">E-mail digitado.</param>
    /// <param name="source">IP de origem da requisição.</param>
    /// <param name="fallbackBaseUrl">Endereço do site a usar só se <c>Site:BaseUrl</c> não estiver configurado (nunca em Production).</param>
    /// <param name="traceId">Código de correlação da requisição, para ligar uma falha de envio ao pedido.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    Task RequestAsync(string email, string source, string fallbackBaseUrl, string traceId, CancellationToken cancellationToken);

    /// <summary>Diz se o link ainda vale, sem gastá-lo.</summary>
    Task<RecoveryLinkState> CheckLinkAsync(int userId, string code, CancellationToken cancellationToken);

    /// <summary>
    /// Troca a senha pelo link. Senha fraca não gasta o link. Dando certo, o link deixa de valer, as falhas de entrada e o bloqueio
    /// da conta zeram (RC-12) e a ação vai para a auditoria.
    /// </summary>
    Task<PasswordResetResult> ResetAsync(int userId, string code, string newPassword, CancellationToken cancellationToken);
}
