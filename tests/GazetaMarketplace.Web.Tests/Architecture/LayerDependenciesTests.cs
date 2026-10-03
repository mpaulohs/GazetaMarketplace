using System.Linq;
using System.Xml.Linq;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Architecture;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class LayerDependenciesTests
#pragma warning restore CA1515
{
    private const string Core = "src/GazetaMarketplace.Core/GazetaMarketplace.Core.csproj";
    private const string Infrastructure = "src/GazetaMarketplace.Infrastructure/GazetaMarketplace.Infrastructure.csproj";
    private const string Web = "src/GazetaMarketplace.Web/GazetaMarketplace.Web.csproj";

    private static XDocument Load(string relative) => XDocument.Load(RepositoryHelper.Project(relative));

    private static string[] Includes(XDocument doc, string element) =>
        doc.Descendants(element).Select(e => (string)e.Attribute("Include")).ToArray();

    [TestMethod]
    public void Core_NaoReferencia_AspNetNemEfCore()
    {
        XDocument doc = Load(Core);

        Assert.IsEmpty(Includes(doc, "ProjectReference"), "O Core não referencia outros projetos.");
        Assert.IsEmpty(Includes(doc, "FrameworkReference"), "O Core não referencia o framework ASP.NET.");
        string[] forbidden = Includes(doc, "PackageReference")
            .Where(p => p.StartsWith("Microsoft.AspNetCore", System.StringComparison.Ordinal)
                     || p.StartsWith("Microsoft.EntityFrameworkCore", System.StringComparison.Ordinal))
            .ToArray();
        Assert.IsEmpty(forbidden, "Pacotes proibidos no Core: " + string.Join(", ", forbidden));
        Assert.AreEqual("Microsoft.NET.Sdk", (string)doc.Root.Attribute("Sdk"), "O Core usa o SDK base, não o Web.");
    }

    [TestMethod]
    public void Infrastructure_ReferenciaSomenteCore()
    {
        XDocument doc = Load(Infrastructure);

        string[] references = Includes(doc, "ProjectReference");
        Assert.HasCount(1, references);
        StringAssert.EndsWith(references[0].Replace('\\', '/'), "GazetaMarketplace.Core/GazetaMarketplace.Core.csproj");

        XDocument web = Load(Web);
        string[] webReferences = Includes(web, "ProjectReference").Select(r => r.Replace('\\', '/')).ToArray();
        Assert.HasCount(2, webReferences);
        Assert.IsTrue(webReferences.Any(r => r.EndsWith("GazetaMarketplace.Core/GazetaMarketplace.Core.csproj", System.StringComparison.Ordinal)));
        Assert.IsTrue(webReferences.Any(r => r.EndsWith("GazetaMarketplace.Infrastructure/GazetaMarketplace.Infrastructure.csproj", System.StringComparison.Ordinal)));
    }
}
