using System.Collections.Generic;
using System.IO;
using System.Linq;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Arquitetura;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class JustificativaDapperTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void TodaEscritaDapper_TemComentarioComOMotivo()
    {
        string raiz = RepositorioHelper.Projeto("src");
        List<string> sem = [];
        foreach (string arquivo in Directory.EnumerateFiles(raiz, "*.cs", SearchOption.AllDirectories))
        {
            if (arquivo.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            {
                continue;
            }

            sem.AddRange(VerificadorDapper.LinhasSemJustificativa(File.ReadAllText(arquivo))
                .Select(linha => Path.GetFileName(arquivo) + ":" + linha));
        }

        Assert.IsEmpty(sem, "Escrita Dapper sem '// Dapper: <motivo>': " + string.Join(", ", sem));
    }

    [TestMethod]
    public void Verificador_AceitaChamadaJustificada()
    {
        const string codigo = """
            using Dapper;
            // Dapper: carga de 10 mil linhas; o EF faria um INSERT por linha
            await conexao.ExecuteAsync(sql, linhas);
            """;

        Assert.IsEmpty(VerificadorDapper.LinhasSemJustificativa(codigo));
    }

    [TestMethod]
    public void Verificador_ApontaChamadaSemMotivo_OuComMotivoVazio()
    {
        const string semComentario = "using Dapper;\nawait conexao.ExecuteAsync(sql, linhas);\n";
        const string motivoVazio = "using Dapper;\n// Dapper:\nawait conexao.Execute(sql);\n";
        const string comentarioDistante = "using Dapper;\n// Dapper: motivo bem longe\n\n\n\n\n\nconexao.Execute(sql);\n";

        Assert.HasCount(1, VerificadorDapper.LinhasSemJustificativa(semComentario));
        Assert.HasCount(1, VerificadorDapper.LinhasSemJustificativa(motivoVazio));
        Assert.HasCount(1, VerificadorDapper.LinhasSemJustificativa(comentarioDistante));
    }

    [TestMethod]
    public void Verificador_IgnoraArquivosSemDapper_EConsultasDeLeitura()
    {
        Assert.IsEmpty(VerificadorDapper.LinhasSemJustificativa("comando.ExecuteAsync(x);"));
        Assert.IsEmpty(VerificadorDapper.LinhasSemJustificativa("using Dapper;\nvar r = await c.QueryAsync<int>(sql);"));
    }
}
