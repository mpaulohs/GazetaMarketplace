using System.Linq;
using System.Xml.Linq;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Arquitetura;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DependenciasDasCamadasTests
#pragma warning restore CA1515
{
    private const string Core = "src/GazetaMarketplace.Core/GazetaMarketplace.Core.csproj";
    private const string Infrastructure = "src/GazetaMarketplace.Infrastructure/GazetaMarketplace.Infrastructure.csproj";
    private const string Web = "src/GazetaMarketplace.Web/GazetaMarketplace.Web.csproj";

    private static XDocument Carregar(string relativo) => XDocument.Load(RepositorioHelper.Projeto(relativo));

    private static string[] Includes(XDocument doc, string elemento) =>
        doc.Descendants(elemento).Select(e => (string)e.Attribute("Include")).ToArray();

    [TestMethod]
    public void Core_NaoReferencia_AspNetNemEfCore()
    {
        XDocument doc = Carregar(Core);

        Assert.IsEmpty(Includes(doc, "ProjectReference"), "O Core não referencia outros projetos.");
        Assert.IsEmpty(Includes(doc, "FrameworkReference"), "O Core não referencia o framework ASP.NET.");
        string[] proibidos = Includes(doc, "PackageReference")
            .Where(p => p.StartsWith("Microsoft.AspNetCore", System.StringComparison.Ordinal)
                     || p.StartsWith("Microsoft.EntityFrameworkCore", System.StringComparison.Ordinal))
            .ToArray();
        Assert.IsEmpty(proibidos, "Pacotes proibidos no Core: " + string.Join(", ", proibidos));
        Assert.AreEqual("Microsoft.NET.Sdk", (string)doc.Root.Attribute("Sdk"), "O Core usa o SDK base, não o Web.");
    }

    [TestMethod]
    public void Infrastructure_ReferenciaSomenteCore()
    {
        XDocument doc = Carregar(Infrastructure);

        string[] referencias = Includes(doc, "ProjectReference");
        Assert.HasCount(1, referencias);
        StringAssert.EndsWith(referencias[0].Replace('\\', '/'), "GazetaMarketplace.Core/GazetaMarketplace.Core.csproj");

        XDocument web = Carregar(Web);
        string[] referenciasWeb = Includes(web, "ProjectReference").Select(r => r.Replace('\\', '/')).ToArray();
        Assert.HasCount(2, referenciasWeb);
        Assert.IsTrue(referenciasWeb.Any(r => r.EndsWith("GazetaMarketplace.Core/GazetaMarketplace.Core.csproj", System.StringComparison.Ordinal)));
        Assert.IsTrue(referenciasWeb.Any(r => r.EndsWith("GazetaMarketplace.Infrastructure/GazetaMarketplace.Infrastructure.csproj", System.StringComparison.Ordinal)));
    }
}
