using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Infrastructure.Location;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Cep;

/// <summary>O cliente do ViaCEP isolado: uma tentativa, tempo limite, os formatos de resposta e o que nunca é lido.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ViaCepLookupTests
#pragma warning restore CA1515
{
    private const string Cep = "13015100";

    private sealed class LogSpy : ILogger<ViaCepLookup>
    {
        public System.Collections.Generic.List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }

    private static (ViaCepLookup Lookup, LogSpy Log) Create(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler, TimeSpan? timeout = null)
    {
        LogSpy log = new();
        HttpClient http = new(new DelegatingStub(handler)) { BaseAddress = new Uri("https://viacep.com.br/ws/"), Timeout = timeout ?? TimeSpan.FromSeconds(5) };
        return (new ViaCepLookup(http, new FakeCurrentUser { CorrelationId = "trace-123" }, log), log);
    }

    private static (ViaCepLookup Lookup, LogSpy Log) Create(HttpStatusCode status, string body) =>
        Create((_, _) => Task.FromResult(CepEndpointTests.Respond(status, body)));

    private sealed class DelegatingStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => handler(request, cancellationToken);
    }

    [TestMethod]
    public async Task Encontrado_DevolveCidadeUfEIbge()
    {
        (ViaCepLookup lookup, _) = Create(HttpStatusCode.OK, """{"cep":"13015-100","localidade":" Campinas ","uf":"sp","ibge":"3509502"}""");

        CepLookupResult result = await lookup.LookupAsync(Cep, CancellationToken.None);

        Assert.AreEqual(new CepLookupResult("Campinas", "SP", 3509502), result);
    }

    [TestMethod]
    public async Task SemCodigoDoIbge_DevolveNuloNoCodigo()
    {
        (ViaCepLookup lookup, _) = Create(HttpStatusCode.OK, """{"localidade":"Campinas","uf":"SP"}""");

        Assert.IsNull((await lookup.LookupAsync(Cep, CancellationToken.None)).IbgeCode);
    }

    [TestMethod]
    [DataRow("""{"erro": true}""")]
    [DataRow("""{"erro": "true"}""")]
    [DataRow("""{"erro":true,"localidade":"Campinas","uf":"SP"}""")]
    public async Task ErroDoViaCep_EhCepInexistente_Nulo_NaoFalhaDoServico(string body)
    {
        (ViaCepLookup lookup, LogSpy log) = Create(HttpStatusCode.OK, body);

        Assert.IsNull(await lookup.LookupAsync("99999999", CancellationToken.None));
        Assert.IsEmpty(log.Entries, "não é falha: nada é registrado como aviso");
    }

    [TestMethod]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.BadGateway)]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.BadRequest)]
    [DataRow(HttpStatusCode.Forbidden)]
    public async Task RespostaHttpComErro_EhServicoIndisponivel_ERegistraAvisoComCepETraceId(HttpStatusCode status)
    {
        (ViaCepLookup lookup, LogSpy log) = Create(status, "x");

        ServiceUnavailableException error = await Assert.ThrowsExactlyAsync<ServiceUnavailableException>(() => lookup.LookupAsync(Cep, CancellationToken.None));

        Assert.AreEqual("CEP_SERVICE_UNAVAILABLE", error.Code);
        Assert.AreEqual(503, error.StatusCode);
        (LogLevel level, string message) = log.Entries.Single();
        Assert.AreEqual(LogLevel.Warning, level);
        StringAssert.Contains(message, Cep);
        StringAssert.Contains(message, "trace-123");
    }

    [TestMethod]
    public async Task FalhaDeRede_EhServicoIndisponivel()
    {
        (ViaCepLookup lookup, _) = Create((_, _) => throw new HttpRequestException("sem rede"));

        await Assert.ThrowsExactlyAsync<ServiceUnavailableException>(() => lookup.LookupAsync(Cep, CancellationToken.None));
    }

    [TestMethod]
    public async Task ViaCepLento_EstouraOTempoLimite_ViraServicoIndisponivel_SemEsperarAResposta()
    {
        (ViaCepLookup lookup, _) = Create(async (_, token) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(20), token); // respeita o cancelamento do HttpClient
            return CepEndpointTests.Respond(HttpStatusCode.OK, "{}");
        }, timeout: TimeSpan.FromMilliseconds(300));
        System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

        await Assert.ThrowsExactlyAsync<ServiceUnavailableException>(() => lookup.LookupAsync(Cep, CancellationToken.None));

        Assert.IsLessThan(5000, clock.ElapsedMilliseconds, "desistiu no tempo limite, não depois de 20 s");
    }

    [TestMethod]
    public async Task ClienteQueDesiste_NaoViraServicoIndisponivel()
    {
        using CancellationTokenSource cancel = new();
        (ViaCepLookup lookup, LogSpy log) = Create(async (_, token) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(20), token);
            return CepEndpointTests.Respond(HttpStatusCode.OK, "{}");
        });
        Task<CepLookupResult> pending = lookup.LookupAsync(Cep, cancel.Token);

        await cancel.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => pending);
        Assert.IsEmpty(log.Entries, "o cliente desistiu: não é falha do ViaCEP");
    }

    [TestMethod]
    [DataRow("não é json")]
    [DataRow("")]
    [DataRow("[]")]
    [DataRow("42")]
    [DataRow("""{"localidade":"Campinas"}""")]
    [DataRow("""{"uf":"SP"}""")]
    [DataRow("""{"localidade":"","uf":"SP"}""")]
    [DataRow("""{"localidade":"Campinas","uf":"XX"}""")]
    [DataRow("""{"localidade":123,"uf":"SP"}""")]
    public async Task RespostaForaDoFormato_EhServicoIndisponivel(string body)
    {
        (ViaCepLookup lookup, _) = Create(HttpStatusCode.OK, body);

        await Assert.ThrowsExactlyAsync<ServiceUnavailableException>(() => lookup.LookupAsync(Cep, CancellationToken.None));
    }

    [TestMethod]
    public async Task RuaEBairro_NuncaSaoLidos_AindaQueVenhamEstranhos()
    {
        // Se o cliente lesse logradouro/bairro como texto, o número e o objeto abaixo quebrariam a leitura
        (ViaCepLookup lookup, _) = Create(HttpStatusCode.OK, """{"logradouro":12345,"bairro":{"x":1},"localidade":"Campinas","uf":"SP","ibge":"3509502"}""");

        CepLookupResult result = await lookup.LookupAsync(Cep, CancellationToken.None);

        Assert.AreEqual("Campinas", result.City);
    }

    [TestMethod]
    public async Task CepInvalido_NaoChegaAoHttp()
    {
        int calls = 0;
        (ViaCepLookup lookup, _) = Create((_, _) =>
        {
            calls++;
            return Task.FromResult(CepEndpointTests.Respond(HttpStatusCode.OK, "{}"));
        });

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => lookup.LookupAsync("1234", CancellationToken.None));

        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public void Padroes_SaoOEnderecoEOTempoDoAdr007()
    {
        ViaCepOptions options = new();

        Assert.AreEqual("https://viacep.com.br/ws/", options.BaseUrl);
        Assert.AreEqual(5, options.TimeoutSeconds);
    }
}
