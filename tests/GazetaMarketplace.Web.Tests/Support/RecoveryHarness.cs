using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Email;
using GazetaMarketplace.Infrastructure.Recovery;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Remetente de e-mail de teste: guarda o que "saiu". Pode ser travado (para provar que a resposta não espera o envio) ou falhar.</summary>
internal sealed class SpyEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyList<EmailMessage> Sent => _sent.ToList();

    /// <summary>Enquanto não estiver concluída, o envio fica parado.</summary>
    public TaskCompletionSource Gate { get; set; }

    /// <summary>Se preenchida, o envio falha com esta exceção.</summary>
    public Exception FailWith { get; set; }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (Gate is not null)
        {
            await Gate.Task.WaitAsync(cancellationToken);
        }

        if (FailWith is not null)
        {
            throw FailWith;
        }

        _sent.Enqueue(message);
    }
}

/// <summary>A tela de redefinição aberta pelo link do e-mail: o que o link carrega.</summary>
internal sealed record RecoveryLink(int Id, string Code, string Url)
{
    public string PathAndQuery => new Uri(Url).PathAndQuery;
}

/// <summary>Site de teste com banco em memória, relógio controlável e o remetente de e-mail trocado por um espião.</summary>
internal sealed partial class RecoveryHarness : IDisposable
{
    public const string Neutral = "Se o e-mail estiver cadastrado, enviaremos as instruções";

    /// <param name="configuration">Configuração extra do site.</param>
    /// <param name="environment">Ambiente do host; em "Production" o remetente é o SendGrid de verdade.</param>
    /// <param name="sendGridHandler">Se informado, o <see cref="SendGridEmailSender"/> fala com este SendGrid de mentira, em vez do espião.</param>
    public RecoveryHarness(Dictionary<string, string> configuration = null, string environment = "Testing", HttpMessageHandler sendGridHandler = null)
    {
        Mail = new SpyEmailSender();
        Factory = new WebFactory(
            environment: environment,
            configuration: configuration,
            services: services =>
            {
                if (sendGridHandler is null)
                {
                    services.AddSingleton<IEmailSender>(Mail);
                }
                else
                {
                    services.AddHttpClient<IEmailSender, SendGridEmailSender>().ConfigurePrimaryHttpMessageHandler(() => sendGridHandler);
                }
            },
            withDatabase: true);
        Client = TeamClient.Create(Factory);
    }

    public WebFactory Factory { get; }

    public SpyEmailSender Mail { get; }

    public HttpClient Client { get; }

    /// <summary>Preenche e envia o formulário "Esqueci minha senha".</summary>
    public async Task<HttpResponseMessage> RequestAsync(string email, string ip = "10.0.0.1", string host = null)
    {
        string page = await Client.GetStringAsync("/painel/esqueci-minha-senha");
        string token = TokenRegex().Match(page).Groups[1].Value;
        using FormUrlEncodedContent form = new(new Dictionary<string, string> { ["Email"] = email, ["__RequestVerificationToken"] = token });
        using HttpRequestMessage request = new(HttpMethod.Post, "/painel/esqueci-minha-senha") { Content = form };
        request.Headers.Add(WebFactory.RemoteIpHeader, ip);
        if (host is not null)
        {
            request.Headers.Host = host;
        }

        return await Client.SendAsync(request);
    }

    /// <summary>Espera o envio em segundo plano terminar.</summary>
    public Task WaitForSendingAsync() =>
        Factory.Services.GetRequiredService<PasswordRecoveryQueue>().WaitUntilIdleAsync(TimeSpan.FromSeconds(15));

    /// <summary>O link do último e-mail enviado.</summary>
    public RecoveryLink LastLink()
    {
        EmailMessage message = Mail.Sent.Last();
        Match match = LinkRegex().Match(message.TextBody);
        Assert_(match.Success, "o e-mail não tem o link de redefinição");
        return new RecoveryLink(int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), match.Groups[2].Value, match.Value);
    }

    /// <summary>Abre o link (GET) como a pessoa faria.</summary>
    public Task<HttpResponseMessage> OpenAsync(RecoveryLink link) => Client.GetAsync(link.PathAndQuery);

    /// <summary>Envia a tela "Definir nova senha" para o link, como se a pessoa a tivesse aberto.</summary>
    public async Task<HttpResponseMessage> SetNewPasswordAsync(RecoveryLink link, string newPassword, string confirm = null)
    {
        // O token antiforgery vem de uma página qualquer com formulário: com o link vencido ou usado, a tela do link não tem formulário
        string page = await Client.GetStringAsync("/painel/esqueci-minha-senha");
        string token = TokenRegex().Match(page).Groups[1].Value;
        using FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["Id"] = link.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Code"] = link.Code,
            ["NewPassword"] = newPassword,
            ["ConfirmPassword"] = confirm ?? newPassword,
            ["__RequestVerificationToken"] = token
        });
        return await Client.PostAsync("/painel/redefinir-senha", form);
    }

    /// <summary>Tira o que muda de uma requisição para outra (tokens antiforgery), para comparar duas respostas.</summary>
    public static string Normalize(string html) => TokenValueRegex().Replace(html, "TOKEN");

    /// <summary>Grava tentativas antigas direto no banco, para testar os limites sem repetir centenas de requisições.</summary>
    public void SeedAttempts(int count, DateTime requestedAt, string emailPrefix = "semente", string ip = "172.16.0.1")
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        for (int i = 0; i < count; i++)
        {
            db.PasswordRecoveryAttempts.Add(new GazetaMarketplace.Core.Entities.PasswordRecoveryAttempt
            {
                Email = $"{emailPrefix}{i}@exemplo.com.br",
                Ip = ip,
                RequestedAt = requestedAt
            });
        }

        db.SaveChanges();
    }

    public int AttemptCount()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>().PasswordRecoveryAttempts.Count();
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
    }

    private static void Assert_(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    [GeneratedRegex(@"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"https?://[^\s]+/painel/redefinir-senha\?id=(\d+)&code=([A-Za-z0-9_\-]+)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"(?<=(value|content)="")[A-Za-z0-9_\-]{40,}(?="")")]
    private static partial Regex TokenValueRegex();
}
