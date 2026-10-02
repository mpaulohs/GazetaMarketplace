using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Layout;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class XssTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task TextoDeUsuario_EhCodificado_NaoExecuta()
    {
        string html = await BaixarAsync("/teste/estados-hostis");

        Assert.IsFalse(html.Contains("<script>alert", System.StringComparison.Ordinal), "o texto hostil saiu sem codificação");
        StringAssert.Contains(html, "&lt;script&gt;alert(");
        Assert.AreEqual(0, Regex.Matches(html, @"<script\b(?![^>]*\bsrc=)", RegexOptions.IgnoreCase).Count);
    }

    [TestMethod]
    public async Task EnderecoDaAcao_SoAceitaCaminhoLocal()
    {
        string html = await BaixarAsync("/teste/estados-hostis");

        Assert.IsFalse(html.Contains("javascript:", System.StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(html.Contains("evil.example", System.StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<string> BaixarAsync(string caminho)
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();
        HttpResponseMessage resposta = await cliente.GetAsync(caminho);
        Assert.IsTrue(resposta.IsSuccessStatusCode);
        return await resposta.Content.ReadAsStringAsync();
    }
}
