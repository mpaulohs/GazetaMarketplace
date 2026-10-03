using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Middleware;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CorrelationIdTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task Requisicao_SemCabecalho_GeraEDevolveOId()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/teste/log");

        Assert.IsTrue(response.Headers.TryGetValues("X-Correlation-ID", out var valores));
        string id = valores.Single();
        Assert.IsFalse(string.IsNullOrWhiteSpace(id));

        // cada linha de log da requisição leva o mesmo id
        var logEvent = factory.Logs.Events.Single(e => CollectorSink.Text(e) == "Linha de teste");
        Assert.AreEqual("\"" + id + "\"", logEvent.Properties["CorrelationId"].ToString());
    }

    [TestMethod]
    public async Task Requisicao_ComCabecalho_PropagaOMesmoId()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/teste/log");
        request.Headers.Add("X-Correlation-ID", "abc-123_XYZ");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.AreEqual("abc-123_XYZ", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [TestMethod]
    [DataRow("com espaço e <script>")]
    [DataRow("0123456789012345678901234567890123456789012345678901234567890123456789")]
    public async Task Requisicao_ComCabecalhoInseguro_GeraUmNovoId(string insecure)
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/teste/log");
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", insecure);

        HttpResponseMessage response = await client.SendAsync(request);

        string id = response.Headers.GetValues("X-Correlation-ID").Single();
        Assert.AreNotEqual(insecure, id);
        Assert.IsFalse(string.IsNullOrWhiteSpace(id));
    }
}
