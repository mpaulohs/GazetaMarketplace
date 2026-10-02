using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Seguranca;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CorsTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task Nenhuma_PoliticaCors_Registrada()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        CorsOptions opcoes = fabrica.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
        Assert.IsNull(opcoes.DefaultPolicyName is null ? null : opcoes.GetPolicy(opcoes.DefaultPolicyName));

        using HttpRequestMessage pedido = new(HttpMethod.Get, "/api/v1/teste/log");
        pedido.Headers.Add("Origin", "https://outro-site.example");
        HttpResponseMessage resposta = await cliente.SendAsync(pedido);

        Assert.IsFalse(resposta.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.IsFalse(resposta.Headers.Contains("Access-Control-Allow-Credentials"));
    }
}
