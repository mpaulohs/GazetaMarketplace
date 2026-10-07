namespace GazetaMarketplace.Infrastructure.Recovery;

/// <summary>O que o envio em segundo plano deve mandar.</summary>
public enum RecoveryJobKind
{
    /// <summary>E-mail com o link de redefinição de senha.</summary>
    PasswordReset,

    /// <summary>Aviso ao dono da conta de que houve tentativas repetidas de entrar (SC-03).</summary>
    LockoutNotice
}

/// <summary>Um envio de e-mail a fazer, fora do caminho da resposta (RC-13).</summary>
/// <param name="Email">E-mail digitado, em minúsculas.</param>
/// <param name="BaseUrl">Endereço do site a usar no link.</param>
/// <param name="TraceId">Código de correlação da requisição que fez o pedido.</param>
/// <param name="Kind">Redefinição de senha (padrão) ou aviso de bloqueio.</param>
public sealed record RecoveryJob(string Email, string BaseUrl, string TraceId, RecoveryJobKind Kind = RecoveryJobKind.PasswordReset);
