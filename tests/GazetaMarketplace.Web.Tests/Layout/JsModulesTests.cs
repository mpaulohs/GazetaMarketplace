using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Layout;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class JsModulesTests
#pragma warning restore CA1515
{
    private static readonly Regex _dangerous = new(
        @"\.(innerHTML|outerHTML)\b|insertAdjacentHTML|document\.write|\beval\s*\(|new\s+Function\s*\(|setTimeout\s*\(\s*['""`]",
        RegexOptions.Compiled);

    [TestMethod]
    public void NenhumModuloUsaInnerHtmlComTextoDoServidor()
    {
        string[] modules = Directory.GetFiles(RepositoryRoot.Wwwroot("js"), "*.js", SearchOption.AllDirectories);

        Assert.IsTrue(modules.Length >= 2, "esperava ao menos api.js e layout.js; a varredura não pode ser vazia");
        foreach (string modulo in modules)
        {
            Match finding = _dangerous.Match(File.ReadAllText(modulo));
            Assert.IsFalse(finding.Success, Path.GetFileName(modulo) + " usa " + finding.Value + " (RC-17: montar com textContent ou <template>)");
        }
    }

    [TestMethod]
    public void Modulos_SaoModulosEs_SemGlobais()
    {
        foreach (string modulo in Directory.GetFiles(RepositoryRoot.Wwwroot("js"), "*.js", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(modulo);
            Assert.IsFalse(Regex.IsMatch(text, @"^\s*var\s", RegexOptions.Multiline), Path.GetFileName(modulo) + " usa var");
            Assert.IsFalse(Regex.IsMatch(text, @"\bwindow\.[A-Za-z_]+\s*="), Path.GetFileName(modulo) + " cria global");
        }
    }

    [TestMethod]
    public void ApiJs_EnviaOTokenAntiforgery_ETrataProblemDetails()
    {
        string api = File.ReadAllText(RepositoryRoot.Wwwroot("js", "modules", "api.js"));

        StringAssert.Contains(api, "RequestVerificationToken");
        StringAssert.Contains(api, "request-verification-token");
        StringAssert.Contains(api, "traceId");
        StringAssert.Contains(api, "class ApiError");
        Assert.IsTrue(new[] { "export async function apiFetch", "export class ApiError" }.All(api.Contains));
    }
}
