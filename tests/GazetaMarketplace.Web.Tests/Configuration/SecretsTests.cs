using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Configuration;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SecretsTests
#pragma warning restore CA1515
{
    // As três pastas do plano do SmarterASP (AR-01): caminho de pasta não é segredo
    private static readonly Regex FolderPath = new(@"^h:\\root\\home\\[A-Za-z0-9-]+\\www\\gazeta-(fotos|logs|chaves)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static readonly string[] Extensions = [".json", ".config", ".example", ".xml", ".props", ".cs", ".cshtml", ".yml", ".yaml"];
    private static readonly string[] IgnoredFolders = ["bin", "obj", "lib", "node_modules", ".git"];

    private static IEnumerable<string> ProductionFiles()
    {
        string root = RepositoryHelper.Root();
        return Directory.EnumerateFiles(Path.Combine(root, "src"), "*", SearchOption.AllDirectories)
            .Where(f => Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(p => IgnoredFolders.Contains(p)))
            // appsettings.Development.json é local e fica fora do git (.gitignore)
            .Where(f => !f.EndsWith("appsettings.Development.json", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Repositorio_NaoContem_ConnectionStringComSenha()
    {
        Regex password = new(@"\b(Password|Pwd)\s*=\s*[^;""'<\s(][^;""'<]*", RegexOptions.IgnoreCase);
        Regex sendGridKey = new(@"SG\.[A-Za-z0-9_\-]{16,}");

        List<string> findings = [];
        foreach (string file in ProductionFiles())
        {
            string text = File.ReadAllText(file);
            if (password.IsMatch(text) || sendGridKey.IsMatch(text))
            {
                findings.Add(Path.GetRelativePath(RepositoryHelper.Root(), file));
            }
        }

        Assert.IsEmpty(findings, "Possível segredo em: " + string.Join(", ", findings));
    }

    [TestMethod]
    public void ArquivoDeExemplo_TemAvisoDeGuardaESemValorReal()
    {
        string path = RepositoryHelper.Project("src/GazetaMarketplace.Web/web.Production.config.example");
        Assert.IsTrue(File.Exists(path), "web.Production.config.example não existe.");

        string text = File.ReadAllText(path);
        string header = text.Substring(0, Math.Min(text.Length, 600));
        StringAssert.Contains(header, "NUNCA");
        StringAssert.Contains(header, "git");
        StringAssert.Contains(header, "cofre de senhas");

        MatchCollection values = Regex.Matches(text, @"<environmentVariable\s+name=""[^""]+""\s+value=""([^""]*)""");
        Assert.IsGreaterThan(0, values.Count);
        foreach (Match m in values)
        {
            string value = m.Groups[1].Value;
            // "true"/"false" é o valor do interruptor do DPAPI (DataProtection__ProtectWithDpapi) e h:\root\home\... é o caminho das pastas do plano: nenhum dos dois é segredo
            bool placeholder = value.StartsWith('(') || value is "Production" or "true" or "false" || FolderPath.IsMatch(value);
            Assert.IsTrue(placeholder, "Valor que não é placeholder no arquivo de exemplo: " + value);
        }
    }

    [TestMethod]
    public void GitIgnore_ProtegeArquivosLocaisDeConfiguracao()
    {
        string gitignore = File.ReadAllText(RepositoryHelper.Project(".gitignore"));
        StringAssert.Contains(gitignore, "appsettings.Development.json");
        StringAssert.Contains(gitignore, "web.Production.config");
        StringAssert.Contains(gitignore, "*.pubxml.user");
    }
}
