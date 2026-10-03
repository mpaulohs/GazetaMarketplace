using GazetaMarketplace.Core.Interfaces;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>Usuário atual controlável; nulo representa uma ação do sistema.</summary>
internal sealed class FakeCurrentUser : ICurrentUser
{
    public int? UserId { get; set; }

    public string CorrelationId { get; set; } = "integration-test";
}

/// <summary>Remetente de e-mail de teste: guarda o que "saiu", para o teste ler o link.</summary>
internal sealed class RecordingEmailSender : IEmailSender
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<EmailMessage> _sent = new();

    public System.Collections.Generic.IReadOnlyList<EmailMessage> Sent => [.. _sent];

    public System.Threading.Tasks.Task SendAsync(EmailMessage message, System.Threading.CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
