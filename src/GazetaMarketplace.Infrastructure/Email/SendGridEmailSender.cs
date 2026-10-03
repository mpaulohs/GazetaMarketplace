using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Infrastructure.Email;

/// <summary>
/// Envio pela API v3 do SendGrid (<c>POST /v3/mail/send</c>) com <see cref="HttpClient"/>, sem SDK (ADR-009).
/// A chave vem das opções (variável de ambiente) e só viaja no cabeçalho Authorization. O log leva apenas o destinatário
/// (mascarado pelo MaskingEnricher): nunca o corpo, o link nem a chave.
/// </summary>
public sealed class SendGridEmailSender(
    HttpClient http,
    IOptions<SendGridOptions> options,
    ILogger<SendGridEmailSender> log) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        SendGridOptions settings = options.Value;

        var body = new
        {
            personalizations = new[] { new { to = new[] { new { email = message.To } } } },
            from = new { email = settings.FromEmail },
            subject = message.Subject,
            content = new[]
            {
                new { type = "text/plain", value = message.TextBody },
                new { type = "text/html", value = message.HtmlBody }
            }
        };

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri(new Uri(settings.BaseUrl), "/v3/mail/send"))
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException error)
        {
            throw new EmailDeliveryException("O SendGrid não respondeu.", error);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new EmailDeliveryException(
                    "O SendGrid recusou o envio (HTTP " + ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture) + ").");
            }
        }

        log.LogInformation("E-mail enviado para {Recipient}", message.To);
    }
}
