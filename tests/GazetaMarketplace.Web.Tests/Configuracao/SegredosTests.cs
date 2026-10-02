using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Configuracao;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SegredosTests
#pragma warning restore CA1515
{
    private static readonly string[] Extensoes = [".json", ".config", ".example", ".xml", ".props", ".cs", ".cshtml", ".yml", ".yaml"];
    private static readonly string[] PastasIgnoradas = ["bin", "obj", "lib", "node_modules", ".git"];

    private static IEnumerable<string> ArquivosDeProducao()
    {
        string raiz = RepositorioHelper.Raiz();
        return Directory.EnumerateFiles(Path.Combine(raiz, "src"), "*", SearchOption.AllDirectories)
            .Where(f => Extensoes.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(p => PastasIgnoradas.Contains(p)))
            // appsettings.Development.json é local e fica fora do git (.gitignore)
            .Where(f => !f.EndsWith("appsettings.Development.json", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Repositorio_NaoContem_ConnectionStringComSenha()
    {
        Regex senha = new(@"\b(Password|Pwd)\s*=\s*[^;""'<\s(][^;""'<]*", RegexOptions.IgnoreCase);
        Regex chaveSendGrid = new(@"SG\.[A-Za-z0-9_\-]{16,}");

        List<string> achados = [];
        foreach (string arquivo in ArquivosDeProducao())
        {
            string texto = File.ReadAllText(arquivo);
            if (senha.IsMatch(texto) || chaveSendGrid.IsMatch(texto))
            {
                achados.Add(Path.GetRelativePath(RepositorioHelper.Raiz(), arquivo));
            }
        }

        Assert.IsEmpty(achados, "Possível segredo em: " + string.Join(", ", achados));
    }

    [TestMethod]
    public void ArquivoDeExemplo_TemAvisoDeGuardaESemValorReal()
    {
        string caminho = RepositorioHelper.Projeto("src/GazetaMarketplace.Web/web.Production.config.example");
        Assert.IsTrue(File.Exists(caminho), "web.Production.config.example não existe.");

        string texto = File.ReadAllText(caminho);
        string cabecalho = texto.Substring(0, Math.Min(texto.Length, 600));
        StringAssert.Contains(cabecalho, "NUNCA");
        StringAssert.Contains(cabecalho, "git");
        StringAssert.Contains(cabecalho, "cofre de senhas");

        MatchCollection valores = Regex.Matches(texto, @"<environmentVariable\s+name=""[^""]+""\s+value=""([^""]*)""");
        Assert.IsGreaterThan(0, valores.Count);
        foreach (Match m in valores)
        {
            string valor = m.Groups[1].Value;
            bool placeholder = valor.StartsWith('(') || valor == "Production";
            Assert.IsTrue(placeholder, "Valor que não é placeholder no arquivo de exemplo: " + valor);
        }
    }

    [TestMethod]
    public void GitIgnore_ProtegeArquivosLocaisDeConfiguracao()
    {
        string gitignore = File.ReadAllText(RepositorioHelper.Projeto(".gitignore"));
        StringAssert.Contains(gitignore, "appsettings.Development.json");
        StringAssert.Contains(gitignore, "web.Production.config");
        StringAssert.Contains(gitignore, "*.pubxml.user");
    }
}
