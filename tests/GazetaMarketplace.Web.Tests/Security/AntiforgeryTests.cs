using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AntiforgeryTests
#pragma warning restore CA1515
{
    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        using JsonDocument json = JsonDocument.Parse(await client.GetStringAsync("/api/v1/teste/token"));
        return json.RootElement.GetProperty("token").GetString();
    }

    [TestMethod]
    public async Task PostSemToken_E_Recusado()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/teste/formulario",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["campo"] = "valor" }));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task PostDeFormulario_ComToken_E_Aceito()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();
        string token = await GetTokenAsync(client);

        HttpResponseMessage response = await client.PostAsync("/teste/formulario",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task EndpointJson_ExigeCabecalhoRequestVerificationToken()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage withoutToken = await client.PostAsJsonAsync("/api/v1/teste/escrita", new { a = 1 });
        Assert.AreEqual(HttpStatusCode.BadRequest, withoutToken.StatusCode);
        Assert.AreEqual("application/problem+json", withoutToken.Content.Headers.ContentType.MediaType);
        using JsonDocument problem = JsonDocument.Parse(await withoutToken.Content.ReadAsStringAsync());
        Assert.AreEqual("VALIDATION_ERROR", problem.RootElement.GetProperty("code").GetString());

        string token = await GetTokenAsync(client);
        using HttpRequestMessage invalid = new(HttpMethod.Post, "/api/v1/teste/escrita") { Content = JsonContent.Create(new { a = 1 }) };
        invalid.Headers.Add("RequestVerificationToken", "token-falso");
        Assert.AreEqual(HttpStatusCode.BadRequest, (await client.SendAsync(invalid)).StatusCode);

        using HttpRequestMessage valid = new(HttpMethod.Post, "/api/v1/teste/escrita") { Content = JsonContent.Create(new { a = 1 }) };
        valid.Headers.Add("RequestVerificationToken", token);
        Assert.AreEqual(HttpStatusCode.OK, (await client.SendAsync(valid)).StatusCode);
    }
}
