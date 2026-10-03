using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>SendGrid de mentira: guarda as requisições recebidas e responde com o código pedido.</summary>
internal sealed class StubSendGridHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<CapturedRequest> _requests = new();

    public HttpStatusCode Status { get; set; } = HttpStatusCode.Accepted;

    /// <summary>Se preenchida, a requisição falha como se a rede tivesse caído.</summary>
    public Exception Fail { get; set; }

    public IReadOnlyList<CapturedRequest> Requests => _requests.ToList();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        _requests.Enqueue(new CapturedRequest(request.Method, request.RequestUri, request.Headers.Authorization?.ToString(), request.Content?.Headers.ContentType?.MediaType, body));

        if (Fail is not null)
        {
            throw Fail;
        }

        return new HttpResponseMessage(Status);
    }

    public sealed record CapturedRequest(HttpMethod Method, Uri Uri, string Authorization, string ContentType, string Body);
}

/// <summary>Logger que guarda tudo o que recebeu: mensagem pronta, propriedades e exceção.</summary>
internal sealed class ListLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<string> _lines = new();

    public IReadOnlyList<string> Lines => _lines.ToList();

    public string All => string.Join("\n", _lines);

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        string properties = state is IEnumerable<KeyValuePair<string, object>> pairs ? string.Join(" ", pairs.Select(p => p.Key + "=" + p.Value)) : string.Empty;
        _lines.Enqueue($"{logLevel}: {formatter(state, exception)} {properties} {exception}");
    }
}

internal sealed class StubEnvironment(string name) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = name;

    public string ApplicationName { get; set; } = "Teste";

    public string ContentRootPath { get; set; } = ".";

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
