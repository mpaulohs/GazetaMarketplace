using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Middleware;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ErrosTests
#pragma warning restore CA1515
{
    [TestMethod]
    [DataRow("notfound", 404, "NOT_FOUND")]
    [DataRow("conflict", 409, "CONFLICT")]
    [DataRow("validation", 400, "VALIDATION_ERROR")]
    [DataRow("forbidden", 403, "FORBIDDEN")]
    [DataRow("unauthorized", 401, "UNAUTHORIZED")]
    [DataRow("cep", 503, "CEP_SERVICE_UNAVAILABLE")]
    [DataRow("inesperado", 500, "INTERNAL_ERROR")]
    public async Task CadaExcecao_MapeiaParaSeuCodigoEStatus(string tipo, int status, string codigo)
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync("/api/v1/teste/erro/" + tipo);

        Assert.AreEqual((HttpStatusCode)status, resposta.StatusCode);
        Assert.AreEqual("application/problem+json", resposta.Content.Headers.ContentType.MediaType);
        using JsonDocument json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        JsonElement raiz = json.RootElement;
        Assert.AreEqual(status, raiz.GetProperty("status").GetInt32());
        Assert.AreEqual(codigo, raiz.GetProperty("code").GetString());
        Assert.AreEqual("/api/v1/teste/erro/" + tipo, raiz.GetProperty("instance").GetString());
        Assert.AreEqual(resposta.Headers.GetValues("X-Correlation-ID").Single(), raiz.GetProperty("traceId").GetString());
        if (tipo == "validation")
        {
            Assert.AreEqual("E-mail inválido", raiz.GetProperty("errors").GetProperty("email")[0].GetString());
        }
    }

    [TestMethod]
    public async Task ErroInesperado_NaoExpoePilha_EtrazTraceId()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync("/api/v1/teste/erro/inesperado");

        string corpo = await resposta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("segredo-interno", corpo);
        Assert.DoesNotContain("StackTrace", corpo);
        Assert.DoesNotContain(" at GazetaMarketplace", corpo);
        string traceId = resposta.Headers.GetValues("X-Correlation-ID").Single();
        StringAssert.Contains(corpo, traceId);

        // o código de referência leva ao log: erro com pilha e o mesmo CorrelationId
        LogEvent evento = fabrica.Logs.Eventos.Single(e => e.Level == LogEventLevel.Error);
        Assert.IsNotNull(evento.Exception);
        StringAssert.Contains(evento.Exception.Message, "segredo-interno-123");
        Assert.AreEqual("\"" + traceId + "\"", evento.Properties["CorrelationId"].ToString());
    }

    [TestMethod]
    public async Task ErroDeCliente_EhRegistradoComoWarning()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        await cliente.GetAsync("/api/v1/teste/erro/notfound");

        Assert.IsTrue(fabrica.Logs.Eventos.Any(e => e.Level == LogEventLevel.Warning));
        Assert.IsFalse(fabrica.Logs.Eventos.Any(e => e.Level == LogEventLevel.Error));
    }

    [TestMethod]
    public async Task TraceId_DoProblemDetails_IgualAoCodigoDeReferenciaDaTela()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        using HttpRequestMessage api = new(HttpMethod.Get, "/api/v1/teste/erro/inesperado");
        api.Headers.Add("X-Correlation-ID", "ref-0042");
        HttpResponseMessage respostaApi = await cliente.SendAsync(api);
        using JsonDocument json = JsonDocument.Parse(await respostaApi.Content.ReadAsStringAsync());

        using HttpRequestMessage pagina = new(HttpMethod.Get, "/teste/pagina-erro");
        pagina.Headers.Add("X-Correlation-ID", "ref-0042");
        HttpResponseMessage respostaPagina = await cliente.SendAsync(pagina);
        string html = await respostaPagina.Content.ReadAsStringAsync();

        Assert.AreEqual("ref-0042", json.RootElement.GetProperty("traceId").GetString());
        Assert.AreEqual(HttpStatusCode.InternalServerError, respostaPagina.StatusCode);
        StringAssert.Contains(html, "Código de referência");
        StringAssert.Contains(html, "ref-0042");
        Assert.DoesNotContain("segredo-interno", html);
        Assert.DoesNotContain("StackTrace", html);
        Assert.DoesNotContain(" at GazetaMarketplace", html);
    }
}
