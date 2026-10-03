using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Infrastructure.Email;

/// <summary>
/// Remetente de desenvolvimento: não envia nada. Em Development escreve o e-mail inteiro no console, para a pessoa copiar o link;
/// fora de Development só registra "e-mail enviado para m***@dominio", sem o link. O console, e não o log, porque o log
/// mascara <c>code=</c> e o link ficaria ilegível. Em Production o remetente é sempre o <see cref="SendGridEmailSender"/>.
/// </summary>
public sealed class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _log;
    private readonly IHostEnvironment _environment;
    private readonly TextWriter _console;

    [ActivatorUtilitiesConstructor]
    public LogEmailSender(ILogger<LogEmailSender> log, IHostEnvironment environment)
        : this(log, environment, Console.Out)
    {
    }

    public LogEmailSender(ILogger<LogEmailSender> log, IHostEnvironment environment, TextWriter console)
    {
        _log = log;
        _environment = environment;
        _console = console;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_environment.IsDevelopment())
        {
            await _console.WriteLineAsync($"--- E-mail para {message.To} ---\nAssunto: {message.Subject}\n\n{message.TextBody}\n--- fim do e-mail ---");
        }

        _log.LogInformation("E-mail enviado para {Recipient}", message.To);
    }
}
