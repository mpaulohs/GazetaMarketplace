using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
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
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync("/api/v1/teste/log");

        Assert.IsTrue(resposta.Headers.TryGetValues("X-Correlation-ID", out var valores));
        string id = valores.Single();
        Assert.IsFalse(string.IsNullOrWhiteSpace(id));

        // cada linha de log da requisição leva o mesmo id
        var evento = fabrica.Logs.Eventos.Single(e => ColetorSink.Texto(e) == "Linha de teste");
        Assert.AreEqual("\"" + id + "\"", evento.Properties["CorrelationId"].ToString());
    }

    [TestMethod]
    public async Task Requisicao_ComCabecalho_PropagaOMesmoId()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();
        using HttpRequestMessage pedido = new(HttpMethod.Get, "/api/v1/teste/log");
        pedido.Headers.Add("X-Correlation-ID", "abc-123_XYZ");

        HttpResponseMessage resposta = await cliente.SendAsync(pedido);

        Assert.AreEqual("abc-123_XYZ", resposta.Headers.GetValues("X-Correlation-ID").Single());
    }

    [TestMethod]
    [DataRow("com espaço e <script>")]
    [DataRow("0123456789012345678901234567890123456789012345678901234567890123456789")]
    public async Task Requisicao_ComCabecalhoInseguro_GeraUmNovoId(string inseguro)
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();
        using HttpRequestMessage pedido = new(HttpMethod.Get, "/api/v1/teste/log");
        pedido.Headers.TryAddWithoutValidation("X-Correlation-ID", inseguro);

        HttpResponseMessage resposta = await cliente.SendAsync(pedido);

        string id = resposta.Headers.GetValues("X-Correlation-ID").Single();
        Assert.AreNotEqual(inseguro, id);
        Assert.IsFalse(string.IsNullOrWhiteSpace(id));
    }
}
