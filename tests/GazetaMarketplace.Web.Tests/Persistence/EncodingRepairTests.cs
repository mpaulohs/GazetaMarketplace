using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GazetaMarketplace.Infrastructure.Data.Seeds;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistence;

/// <summary>
/// Acentos corrompidos pelo <c>sqlcmd</c> do Windows (ImÃ³veis em vez de Imóveis). Sem <c>-f 65001</c>, ele lê o arquivo na página de código do sistema
/// e grava no <c>nvarchar</c> o texto já errado. Duas regras impedem a volta do problema: o script de reparo é só ASCII (nenhum acento para ser mal lido)
/// e todo comando <c>sqlcmd</c> dos documentos manda ler em UTF-8.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class EncodingRepairTests
#pragma warning restore CA1515
{
    private static readonly string[] DocumentsWithSqlcmd =
    [
        "docs/INFRA.md",
        "docs/DEPLOY-RUNBOOK.md",
        "docs/GO-LIVE-CHECKLIST.md",
        "db/seed/README.md",
        "docs/RODAR-TESTES-DE-INTEGRACAO-E-E2E.md",
        "docs/VERIFY-CHECKLIST.md"
    ];

    private static string RepairScriptPath() => RepositoryRoot.FullPath("db", "scripts", "reparar-acentos-categorias.sql");

    [TestMethod]
    public void ScriptDeReparo_TemSoAscii()
    {
        byte[] bytes = File.ReadAllBytes(RepairScriptPath());

        int offset = Array.FindIndex(bytes, b => b > 127);

        Assert.AreEqual(-1, offset, offset < 0 ? null : "Byte fora do ASCII na posição " + offset + ": o script não pode depender do encoding com que o sqlcmd o lê (use NCHAR(n) para os acentos)");
    }

    [TestMethod]
    public void ScriptDeReparo_CobreTodasAsCategoriasAcentuadasDaCarga_ComONomeCerto()
    {
        Dictionary<int, string> expected = InitialCategories.All
            .Where(c => c.Name.Any(ch => ch > 127))
            .ToDictionary(c => c.Id, c => c.Name);
        Dictionary<int, string> inScript = ParseCorrections(File.ReadAllText(RepairScriptPath()));

        Assert.IsNotEmpty(expected);
        CollectionAssert.AreEquivalent(expected.Keys.ToArray(), inScript.Keys.ToArray(), "O script precisa ter uma linha por categoria acentuada da carga, e nenhuma além delas");
        foreach ((int id, string name) in expected)
        {
            Assert.AreEqual(name, inScript[id], "Categoria " + id + ": o nome montado com NCHAR() difere do nome da carga");
        }
    }

    [TestMethod]
    public void ScriptDeReparo_SoMexeEmLinhaComMojibake_EGravaAuditoria()
    {
        string script = File.ReadAllText(RepairScriptPath());

        // Só corrige onde há Ã (195) ou Â (194): o nome editado pelo Administrador não é tocado, e rodar duas vezes não muda nada
        StringAssert.Contains(script, "NCHAR(195)");
        StringAssert.Contains(script, "NCHAR(194)");
        StringAssert.Contains(script, "Latin1_General_BIN2");
        StringAssert.Contains(script, "UpdatedAt");
    }

    [TestMethod]
    public void ComandosSqlcmdDosDocumentos_LeemEmUtf8()
    {
        List<string> without = [];
        foreach (string document in DocumentsWithSqlcmd)
        {
            string[] lines = File.ReadAllLines(RepositoryRoot.FullPath(document.Split('/')), Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                if (IsSqlcmdLoadingAFile(lines[i]) && !lines[i].Contains("-f 65001", StringComparison.Ordinal))
                {
                    without.Add(document + ":" + (i + 1));
                }
            }
        }

        Assert.IsEmpty(without, "Comando sqlcmd sem '-f 65001': " + string.Join(", ", without));
    }

    [TestMethod]
    public void DocumentosComSqlcmd_AvisamQueOF65001EObrigatorio()
    {
        string[] withoutNote =
        [
            .. DocumentsWithSqlcmd.Where(document =>
            {
                string text = File.ReadAllText(RepositoryRoot.FullPath(document.Split('/')), Encoding.UTF8);
                return !text.Contains("O `-f 65001` é obrigatório.", StringComparison.Ordinal);
            })
        ];

        Assert.IsEmpty(withoutNote, "Falta a nota do '-f 65001' em: " + string.Join(", ", withoutNote));
    }

    // Linha que executa o sqlcmd (e não só o cita em prosa) e lê um arquivo: com -Q a consulta vai na própria linha, em ASCII
    private static bool IsSqlcmdLoadingAFile(string line) =>
        Regex.IsMatch(line, @"sqlcmd`?\s+-\w", RegexOptions.CultureInvariant) && !line.Contains(" -Q ", StringComparison.Ordinal);

    // Linhas "(Id, N'Im' + NCHAR(243) + N'veis')," do script de reparo, montadas de volta no nome real
    private static Dictionary<int, string> ParseCorrections(string script)
    {
        Dictionary<int, string> result = [];
        foreach (Match line in Regex.Matches(script, @"^\s*\((\d+),\s*(.+?)\),?\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant))
        {
            StringBuilder name = new();
            foreach (Match part in Regex.Matches(line.Groups[2].Value, @"N'((?:[^']|'')*)'|NCHAR\((\d+)\)", RegexOptions.CultureInvariant))
            {
                name.Append(part.Groups[2].Success
                    ? char.ConvertFromUtf32(int.Parse(part.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture))
                    : part.Groups[1].Value.Replace("''", "'", StringComparison.Ordinal));
            }

            result[int.Parse(line.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)] = name.ToString();
        }

        return result;
    }
}
