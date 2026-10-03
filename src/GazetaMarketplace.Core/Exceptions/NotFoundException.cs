namespace GazetaMarketplace.Core.Exceptions;

/// <summary>Recurso inexistente ou não publicado (404 NOT_FOUND).</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message, "NOT_FOUND", 404)
    {
    }

    public NotFoundException(string entity, object id)
        : base($"{entity} {id} não encontrado.", "NOT_FOUND", 404)
    {
    }
}
