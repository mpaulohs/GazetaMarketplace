namespace GazetaMarketplace.Core.Exceptions;

/// <summary>Edição concorrente, nome repetido ou limite atingido (409 CONFLICT).</summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, "CONFLICT", 409)
    {
    }
}
