using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Seguranca;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AntiforgeryTests
#pragma warning restore CA1515
{
    private static async Task<string> ObterTokenAsync(HttpClient cliente)
    {
        using JsonDocument json = JsonDocument.Parse(await cliente.GetStringAsync("/api/v1/teste/token"));
        return json.RootElement.GetProperty("token").GetString();
    }

    [TestMethod]
    public async Task PostSemToken_E_Recusado()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.PostAsync("/teste/formulario",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["campo"] = "valor" }));

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [TestMethod]
    public async Task PostDeFormulario_ComToken_E_Aceito()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();
        string token = await ObterTokenAsync(cliente);

        HttpResponseMessage resposta = await cliente.PostAsync("/teste/formulario",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
    }

    [TestMethod]
    public async Task EndpointJson_ExigeCabecalhoRequestVerificationToken()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage semToken = await cliente.PostAsJsonAsync("/api/v1/teste/escrita", new { a = 1 });
        Assert.AreEqual(HttpStatusCode.BadRequest, semToken.StatusCode);
        Assert.AreEqual("application/problem+json", semToken.Content.Headers.ContentType.MediaType);
        using JsonDocument problema = JsonDocument.Parse(await semToken.Content.ReadAsStringAsync());
        Assert.AreEqual("VALIDATION_ERROR", problema.RootElement.GetProperty("code").GetString());

        string token = await ObterTokenAsync(cliente);
        using HttpRequestMessage invalido = new(HttpMethod.Post, "/api/v1/teste/escrita") { Content = JsonContent.Create(new { a = 1 }) };
        invalido.Headers.Add("RequestVerificationToken", "token-falso");
        Assert.AreEqual(HttpStatusCode.BadRequest, (await cliente.SendAsync(invalido)).StatusCode);

        using HttpRequestMessage valido = new(HttpMethod.Post, "/api/v1/teste/escrita") { Content = JsonContent.Create(new { a = 1 }) };
        valido.Headers.Add("RequestVerificationToken", token);
        Assert.AreEqual(HttpStatusCode.OK, (await cliente.SendAsync(valido)).StatusCode);
    }
}
