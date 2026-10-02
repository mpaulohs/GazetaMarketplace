namespace GazetaMarketplace.Core.Excecoes;

/// <summary>Serviço externo do CEP sem resposta depois da nova tentativa (503 CEP_SERVICE_UNAVAILABLE).</summary>
public sealed class ServiceUnavailableException : AppException
{
    public ServiceUnavailableException(string message)
        : base(message, "CEP_SERVICE_UNAVAILABLE", 503)
    {
    }
}
