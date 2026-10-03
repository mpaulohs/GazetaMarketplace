using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CorsTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task Nenhuma_PoliticaCors_Registrada()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        CorsOptions options = factory.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
        Assert.IsNull(options.DefaultPolicyName is null ? null : options.GetPolicy(options.DefaultPolicyName));

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/teste/log");
        request.Headers.Add("Origin", "https://outro-site.example");
        HttpResponseMessage response = await client.SendAsync(request);

        Assert.IsFalse(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.IsFalse(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }
}
