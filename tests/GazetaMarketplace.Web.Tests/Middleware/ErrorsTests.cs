using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Middleware;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ErrorsTests
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
    public async Task CadaExcecao_MapeiaParaSeuCodigoEStatus(string type, int status, string code)
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/teste/erro/" + type);

        Assert.AreEqual((HttpStatusCode)status, response.StatusCode);
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType.MediaType);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = json.RootElement;
        Assert.AreEqual(status, root.GetProperty("status").GetInt32());
        Assert.AreEqual(code, root.GetProperty("code").GetString());
        Assert.AreEqual("/api/v1/teste/erro/" + type, root.GetProperty("instance").GetString());
        Assert.AreEqual(response.Headers.GetValues("X-Correlation-ID").Single(), root.GetProperty("traceId").GetString());
        if (type == "validation")
        {
            Assert.AreEqual("E-mail inválido", root.GetProperty("errors").GetProperty("email")[0].GetString());
        }
    }

    [TestMethod]
    public async Task ErroInesperado_NaoExpoePilha_EtrazTraceId()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/teste/erro/inesperado");

        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("segredo-interno", body);
        Assert.DoesNotContain("StackTrace", body);
        Assert.DoesNotContain(" at GazetaMarketplace", body);
        string traceId = response.Headers.GetValues("X-Correlation-ID").Single();
        StringAssert.Contains(body, traceId);

        // o código de referência leva ao log: erro com pilha e o mesmo CorrelationId
        LogEvent logEvent = factory.Logs.Events.Single(e => e.Level == LogEventLevel.Error);
        Assert.IsNotNull(logEvent.Exception);
        StringAssert.Contains(logEvent.Exception.Message, "segredo-interno-123");
        Assert.AreEqual("\"" + traceId + "\"", logEvent.Properties["CorrelationId"].ToString());
    }

    [TestMethod]
    public async Task ErroDeCliente_EhRegistradoComoWarning()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        await client.GetAsync("/api/v1/teste/erro/notfound");

        Assert.IsTrue(factory.Logs.Events.Any(e => e.Level == LogEventLevel.Warning));
        Assert.IsFalse(factory.Logs.Events.Any(e => e.Level == LogEventLevel.Error));
    }

    [TestMethod]
    public async Task TraceId_DoProblemDetails_IgualAoCodigoDeReferenciaDaTela()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpRequestMessage api = new(HttpMethod.Get, "/api/v1/teste/erro/inesperado");
        api.Headers.Add("X-Correlation-ID", "ref-0042");
        HttpResponseMessage apiResponse = await client.SendAsync(api);
        using JsonDocument json = JsonDocument.Parse(await apiResponse.Content.ReadAsStringAsync());

        using HttpRequestMessage page = new(HttpMethod.Get, "/teste/pagina-erro");
        page.Headers.Add("X-Correlation-ID", "ref-0042");
        HttpResponseMessage pageResponse = await client.SendAsync(page);
        string html = await pageResponse.Content.ReadAsStringAsync();

        Assert.AreEqual("ref-0042", json.RootElement.GetProperty("traceId").GetString());
        Assert.AreEqual(HttpStatusCode.InternalServerError, pageResponse.StatusCode);
        StringAssert.Contains(html, "Código de referência");
        StringAssert.Contains(html, "ref-0042");
        Assert.DoesNotContain("segredo-interno", html);
        Assert.DoesNotContain("StackTrace", html);
        Assert.DoesNotContain(" at GazetaMarketplace", html);
    }
}
