using System;

namespace GazetaMarketplace.Core.Excecoes;

/// <summary>
/// Base das exceções esperadas do contrato de erros (ARCHITECTURE.md §8): cada uma carrega o
/// código estável e o status HTTP que o cliente recebe.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message, string code, int statusCode)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }

    public int StatusCode { get; }
}
