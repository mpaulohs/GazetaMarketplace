using System;

namespace GazetaMarketplace.Infrastructure.Email;

/// <summary>O provedor de e-mail recusou ou não respondeu. A mensagem leva só o código HTTP: nunca a chave, o destinatário ou o corpo.</summary>
public sealed class EmailDeliveryException : Exception
{
    public EmailDeliveryException(string message)
        : base(message)
    {
    }

    public EmailDeliveryException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
