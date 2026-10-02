using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Layout;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ModulosJsTests
#pragma warning restore CA1515
{
    private static readonly Regex _perigosos = new(
        @"\.(innerHTML|outerHTML)\b|insertAdjacentHTML|document\.write|\beval\s*\(|new\s+Function\s*\(|setTimeout\s*\(\s*['""`]",
        RegexOptions.Compiled);

    [TestMethod]
    public void NenhumModuloUsaInnerHtmlComTextoDoServidor()
    {
        string[] modulos = Directory.GetFiles(RaizDoRepositorio.Wwwroot("js"), "*.js", SearchOption.AllDirectories);

        Assert.IsTrue(modulos.Length >= 2, "esperava ao menos api.js e layout.js; a varredura não pode ser vazia");
        foreach (string modulo in modulos)
        {
            Match achado = _perigosos.Match(File.ReadAllText(modulo));
            Assert.IsFalse(achado.Success, Path.GetFileName(modulo) + " usa " + achado.Value + " (RC-17: montar com textContent ou <template>)");
        }
    }

    [TestMethod]
    public void Modulos_SaoModulosEs_SemGlobais()
    {
        foreach (string modulo in Directory.GetFiles(RaizDoRepositorio.Wwwroot("js"), "*.js", SearchOption.AllDirectories))
        {
            string texto = File.ReadAllText(modulo);
            Assert.IsFalse(Regex.IsMatch(texto, @"^\s*var\s", RegexOptions.Multiline), Path.GetFileName(modulo) + " usa var");
            Assert.IsFalse(Regex.IsMatch(texto, @"\bwindow\.[A-Za-z_]+\s*="), Path.GetFileName(modulo) + " cria global");
        }
    }

    [TestMethod]
    public void ApiJs_EnviaOTokenAntiforgery_ETrataProblemDetails()
    {
        string api = File.ReadAllText(RaizDoRepositorio.Wwwroot("js", "modules", "api.js"));

        StringAssert.Contains(api, "RequestVerificationToken");
        StringAssert.Contains(api, "request-verification-token");
        StringAssert.Contains(api, "traceId");
        StringAssert.Contains(api, "class ApiError");
        Assert.IsTrue(new[] { "export async function apiFetch", "export class ApiError" }.All(api.Contains));
    }
}
