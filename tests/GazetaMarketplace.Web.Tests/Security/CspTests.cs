using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CspTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task Csp_NaoPermiteScriptInline()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/");

        string csp = response.Headers.GetValues("Content-Security-Policy").Single();
        string[] directives = csp.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("default-src 'self'", directives);
        Assert.Contains("script-src 'self'", directives);
        Assert.Contains("style-src 'self'", directives);
        Assert.Contains("img-src 'self' data:", directives);
        Assert.Contains("frame-ancestors 'none'", directives);
        Assert.Contains("form-action 'self'", directives);
        Assert.DoesNotContain("unsafe-inline", csp.Split(';').First(d => d.Contains("script-src")));
        Assert.DoesNotContain("unsafe-eval", csp);
    }
}
