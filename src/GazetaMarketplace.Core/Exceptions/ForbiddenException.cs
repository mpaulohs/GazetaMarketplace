namespace GazetaMarketplace.Core.Exceptions;

/// <summary>Papel ou autoria insuficiente (403 FORBIDDEN).</summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Você não tem permissão.")
        : base(message, "FORBIDDEN", 403)
    {
    }
}
