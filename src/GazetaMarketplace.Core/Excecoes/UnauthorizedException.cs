namespace GazetaMarketplace.Core.Excecoes;

/// <summary>Sessão ausente ou vencida (401 UNAUTHORIZED).</summary>
public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "É preciso entrar para continuar.")
        : base(message, "UNAUTHORIZED", 401)
    {
    }
}
