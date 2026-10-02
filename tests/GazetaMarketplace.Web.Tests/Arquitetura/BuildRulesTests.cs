using System.IO;
using System.Linq;
using System.Xml.Linq;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Arquitetura;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class BuildRulesTests
#pragma warning restore CA1515
{
    private static readonly string[] Proibidas =
    [
        "Nullable", "ImplicitUsings", "TreatWarningsAsErrors", "TargetFramework", "LangVersion"
    ];

    [TestMethod]
    public void Projetos_NaoSobrescrevem_NullableEImplicitUsings()
    {
        string raiz = RepositorioHelper.Raiz();
        string[] projetos = Directory
            .EnumerateFiles(Path.Combine(raiz, "src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(raiz, "tests"), "*.csproj", SearchOption.AllDirectories))
            .ToArray();

        Assert.IsGreaterThanOrEqualTo(5, projetos.Length, "Esperados Web, Core, Infrastructure e os 2 projetos de teste.");

        foreach (string projeto in projetos)
        {
            string[] sobrescritas = XDocument.Load(projeto)
                .Descendants()
                .Select(e => e.Name.LocalName)
                .Where(nome => Proibidas.Contains(nome))
                .ToArray();
            Assert.IsEmpty(sobrescritas,
                Path.GetFileName(projeto) + " sobrescreve: " + string.Join(", ", sobrescritas));
        }

        string props = File.ReadAllText(Path.Combine(raiz, "Directory.Build.props"));
        StringAssert.Contains(props, "<Nullable>disable</Nullable>");
        StringAssert.Contains(props, "<ImplicitUsings>disable</ImplicitUsings>");
        StringAssert.Contains(props, "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>");
    }

    [TestMethod]
    public void Solucao_ContemOsTresProjetosDeProducao_ESemExemplosDoTemplate()
    {
        string raiz = RepositorioHelper.Raiz();
        string slnx = File.ReadAllText(Path.Combine(raiz, "GazetaMarketplace.slnx"));
        StringAssert.Contains(slnx, "GazetaMarketplace.Core.csproj");
        StringAssert.Contains(slnx, "GazetaMarketplace.Infrastructure.csproj");
        StringAssert.Contains(slnx, "GazetaMarketplace.Web.csproj");
        Assert.IsFalse(slnx.Contains("ClaudeStack") || slnx.Contains("Example."));
        Assert.IsFalse(Directory.Exists(Path.Combine(raiz, "src", "ClaudeStack.API")));
        Assert.IsFalse(Directory.Exists(Path.Combine(raiz, "src", "ClaudeStack.Web")));
    }
}
