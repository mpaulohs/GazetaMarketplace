using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Seguranca;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CspTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task Csp_NaoPermiteScriptInline()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync("/");

        string csp = resposta.Headers.GetValues("Content-Security-Policy").Single();
        string[] diretivas = csp.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("default-src 'self'", diretivas);
        Assert.Contains("script-src 'self'", diretivas);
        Assert.Contains("style-src 'self'", diretivas);
        Assert.Contains("img-src 'self' data:", diretivas);
        Assert.Contains("frame-ancestors 'none'", diretivas);
        Assert.Contains("form-action 'self'", diretivas);
        Assert.DoesNotContain("unsafe-inline", csp.Split(';').First(d => d.Contains("script-src")));
        Assert.DoesNotContain("unsafe-eval", csp);
    }
}
