using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Interfaces;

/// <summary>Um e-mail pronto para sair: sempre com a versão em texto simples e a versão em HTML.</summary>
public sealed record EmailMessage(string To, string Subject, string TextBody, string HtmlBody);

/// <summary>
/// Envio de e-mail (ADR-009). Em Production é o SendGrid; em desenvolvimento, o console.
/// Quem implementa nunca grava o corpo da mensagem no log de Production: ele leva links com credencial.
/// </summary>
public interface IEmailSender
{
    /// <summary>Envia a mensagem ou lança uma exceção se o provedor recusar.</summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
