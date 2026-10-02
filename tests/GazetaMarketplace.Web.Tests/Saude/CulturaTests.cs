using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Saude;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CulturaTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task Cultura_E_PtBr_MesmoComAcceptLanguageDiferente()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();
        using HttpRequestMessage pedido = new(HttpMethod.Get, "/api/v1/teste/cultura");
        pedido.Headers.Add("Accept-Language", "en-US,en;q=0.9");

        HttpResponseMessage resposta = await cliente.SendAsync(pedido);

        using JsonDocument json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.AreEqual("pt-BR", json.RootElement.GetProperty("cultura").GetString());
        Assert.AreEqual("pt-BR", json.RootElement.GetProperty("ui").GetString());
        Assert.AreEqual("1234,5", json.RootElement.GetProperty("numero").GetString());
    }
}
