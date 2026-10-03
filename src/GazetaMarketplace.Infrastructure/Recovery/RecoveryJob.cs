namespace GazetaMarketplace.Infrastructure.Recovery;

/// <summary>Um envio de e-mail de redefinição a fazer, fora do caminho da resposta (RC-13).</summary>
/// <param name="Email">E-mail digitado, em minúsculas.</param>
/// <param name="BaseUrl">Endereço do site a usar no link.</param>
/// <param name="TraceId">Código de correlação da requisição que fez o pedido.</param>
public sealed record RecoveryJob(string Email, string BaseUrl, string TraceId);
