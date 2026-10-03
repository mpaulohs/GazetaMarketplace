using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Health;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CultureTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task Cultura_E_PtBr_MesmoComAcceptLanguageDiferente()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/teste/cultura");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.9");

        HttpResponseMessage response = await client.SendAsync(request);

        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual("pt-BR", json.RootElement.GetProperty("culture").GetString());
        Assert.AreEqual("pt-BR", json.RootElement.GetProperty("ui").GetString());
        Assert.AreEqual("1234,5", json.RootElement.GetProperty("number").GetString());
    }
}
