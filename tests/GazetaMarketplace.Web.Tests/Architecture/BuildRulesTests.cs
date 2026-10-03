using System.IO;
using System.Linq;
using System.Xml.Linq;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Architecture;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class BuildRulesTests
#pragma warning restore CA1515
{
    private static readonly string[] Forbidden =
    [
        "Nullable", "ImplicitUsings", "TreatWarningsAsErrors", "TargetFramework", "LangVersion"
    ];

    [TestMethod]
    public void Projetos_NaoSobrescrevem_NullableEImplicitUsings()
    {
        string root = RepositoryHelper.Root();
        string[] projects = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "tests"), "*.csproj", SearchOption.AllDirectories))
            .ToArray();

        Assert.IsGreaterThanOrEqualTo(5, projects.Length, "Esperados Web, Core, Infrastructure e os 2 projetos de teste.");

        foreach (string project in projects)
        {
            string[] overrides = XDocument.Load(project)
                .Descendants()
                .Select(e => e.Name.LocalName)
                .Where(name => Forbidden.Contains(name))
                .ToArray();
            Assert.IsEmpty(overrides,
                Path.GetFileName(project) + " sobrescreve: " + string.Join(", ", overrides));
        }

        string props = File.ReadAllText(Path.Combine(root, "Directory.Build.props"));
        StringAssert.Contains(props, "<Nullable>disable</Nullable>");
        StringAssert.Contains(props, "<ImplicitUsings>disable</ImplicitUsings>");
        StringAssert.Contains(props, "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>");
    }

    [TestMethod]
    public void Solucao_ContemOsTresProjetosDeProducao_ESemExemplosDoTemplate()
    {
        string root = RepositoryHelper.Root();
        string slnx = File.ReadAllText(Path.Combine(root, "GazetaMarketplace.slnx"));
        StringAssert.Contains(slnx, "GazetaMarketplace.Core.csproj");
        StringAssert.Contains(slnx, "GazetaMarketplace.Infrastructure.csproj");
        StringAssert.Contains(slnx, "GazetaMarketplace.Web.csproj");
        Assert.IsFalse(slnx.Contains("ClaudeStack") || slnx.Contains("Example."));
        Assert.IsFalse(Directory.Exists(Path.Combine(root, "src", "ClaudeStack.API")));
        Assert.IsFalse(Directory.Exists(Path.Combine(root, "src", "ClaudeStack.Web")));
    }
}
