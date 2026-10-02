using System.Collections.Generic;

namespace GazetaMarketplace.Core.Excecoes;

/// <summary>Dados inválidos; <see cref="Errors"/> traz a mensagem por campo (400 VALIDATION_ERROR).</summary>
public sealed class ValidationException : AppException
{
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("Os dados enviados são inválidos.", "VALIDATION_ERROR", 400)
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
