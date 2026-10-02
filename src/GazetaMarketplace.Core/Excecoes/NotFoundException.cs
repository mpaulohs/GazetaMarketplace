namespace GazetaMarketplace.Core.Excecoes;

/// <summary>Recurso inexistente ou não publicado (404 NOT_FOUND).</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message, "NOT_FOUND", 404)
    {
    }

    public NotFoundException(string entidade, object id)
        : base($"{entidade} {id} não encontrado.", "NOT_FOUND", 404)
    {
    }
}
