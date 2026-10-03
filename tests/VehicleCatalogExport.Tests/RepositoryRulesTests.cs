using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VehicleCatalogExport.Tests;

/// <summary>Regras do repositório sobre a ferramenta: ela só lê a origem e não guarda credencial.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class RepositoryRulesTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void LeitorDaOrigem_NaoTemNenhumComandoDeEscrita()
    {
        string code = File.ReadAllText(Repo.Path("tools", "VehicleCatalogExport", "OriginReader.cs"));
        // Os comentários podem falar em escrita; só o código conta
        string executable = string.Join('\n', code.Split('\n').Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal) && !l.TrimStart().StartsWith("///", StringComparison.Ordinal)));

        foreach (string verb in new[] { "INSERT", "UPDATE", "DELETE", "MERGE", "DROP", "ALTER", "TRUNCATE", "EXEC", "CREATE" })
        {
            Assert.IsFalse(Regex.IsMatch(executable, @"\b" + verb + @"\b", RegexOptions.IgnoreCase), $"OriginReader.cs não pode conter {verb}");
        }

        Assert.IsFalse(executable.Contains("ExecuteAsync", StringComparison.Ordinal) || executable.Contains("ExecuteNonQuery", StringComparison.Ordinal), "só QueryAsync");
        StringAssert.Contains(executable, "ApplicationIntent = ApplicationIntent.ReadOnly");
    }

    [TestMethod]
    public void NenhumArquivoDaFerramentaOuDoSeed_TemCredencial()
    {
        string[] roots = [Repo.Path("tools", "VehicleCatalogExport"), Repo.Path("db", "seed"), Repo.Path("tests", "VehicleCatalogExport.Tests", "Data")];
        Regex credential = new(@"(Password|Pwd)\s*=\s*[^;\s""]+|User\s*Id\s*=\s*[^;\s""]+", RegexOptions.IgnoreCase);
        foreach (string root in roots.Where(Directory.Exists))
        {
            foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
            {
                Assert.IsFalse(credential.IsMatch(File.ReadAllText(file)), $"{Path.GetRelativePath(Repo.Path(), file)} contém credencial");
            }
        }
    }

    [TestMethod]
    public void CargaEmLote_TemOComentarioDeJustificativaDoDapper()
    {
        string code = File.ReadAllText(Repo.Path("tools", "VehicleCatalogExport", "BatchLoader.cs"));

        StringAssert.Contains(code, "// Dapper: carga em lote; o EF Core geraria um INSERT por linha");
    }

    [TestMethod]
    public void Ferramenta_FicaForaDaSolucaoDoSite()
    {
        string solution = File.ReadAllText(Repo.Path("GazetaMarketplace.slnx"));

        Assert.DoesNotContain("tools/VehicleCatalogExport", solution);
        StringAssert.Contains(solution, "VehicleCatalogExport.Tests");
    }
}
