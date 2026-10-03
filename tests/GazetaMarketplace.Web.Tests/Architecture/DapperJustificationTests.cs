using System.Collections.Generic;
using System.IO;
using System.Linq;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Architecture;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DapperJustificationTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void TodaEscritaDapper_TemComentarioComOMotivo()
    {
        string root = RepositoryHelper.Project("src");
        List<string> without = [];
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            {
                continue;
            }

            without.AddRange(DapperVerifier.LinesWithoutJustification(File.ReadAllText(file))
                .Select(line => Path.GetFileName(file) + ":" + line));
        }

        Assert.IsEmpty(without, "Escrita Dapper sem '// Dapper: <motivo>': " + string.Join(", ", without));
    }

    [TestMethod]
    public void Verificador_AceitaChamadaJustificada()
    {
        const string code = """
            using Dapper;
            // Dapper: carga de 10 mil linhas; o EF faria um INSERT por linha
            await conexao.ExecuteAsync(sql, linhas);
            """;

        Assert.IsEmpty(DapperVerifier.LinesWithoutJustification(code));
    }

    [TestMethod]
    public void Verificador_ApontaChamadaSemMotivo_OuComMotivoVazio()
    {
        const string withoutComment = "using Dapper;\nawait conexao.ExecuteAsync(sql, linhas);\n";
        const string emptyReason = "using Dapper;\n// Dapper:\nawait conexao.Execute(sql);\n";
        const string distantComment = "using Dapper;\n// Dapper: motivo bem longe\n\n\n\n\n\nconexao.Execute(sql);\n";

        Assert.HasCount(1, DapperVerifier.LinesWithoutJustification(withoutComment));
        Assert.HasCount(1, DapperVerifier.LinesWithoutJustification(emptyReason));
        Assert.HasCount(1, DapperVerifier.LinesWithoutJustification(distantComment));
    }

    [TestMethod]
    public void Verificador_IgnoraArquivosSemDapper_EConsultasDeLeitura()
    {
        Assert.IsEmpty(DapperVerifier.LinesWithoutJustification("comando.ExecuteAsync(x);"));
        Assert.IsEmpty(DapperVerifier.LinesWithoutJustification("using Dapper;\nvar r = await c.QueryAsync<int>(sql);"));
    }
}
