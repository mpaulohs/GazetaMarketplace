using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CitiesImport.Tests;

/// <summary>O script gerado (determinístico e idempotente) e a linha de comando: nada é gerado quando algo está errado.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ScriptAndCliTests
#pragma warning restore CA1515
{
    private const string DevTarget = "Server=127.0.0.1,1;Database=gazeta_dev;User Id=dev;Password=x;Encrypt=False;Connect Timeout=2";

    private static CityRow Row(int code, string name, string uf) => new(code, name, uf, GazetaMarketplace.Core.Search.Normalizer.Normalize(name));

    private static async Task<(int Code, string Out, string Err)> RunAsync(string[] args, params (string Name, string Value)[] variables)
    {
        using StringWriter output = new();
        using StringWriter error = new();
        int code = await Cli.RunAsync(args, name => variables.FirstOrDefault(v => v.Name == name).Value, output, error, CancellationToken.None);
        return (code, output.ToString(), error.ToString());
    }

    private static string WriteInput(string json)
    {
        string file = Ibge.TempFile("municipios.json");
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        File.WriteAllText(file, json);
        return file;
    }

    [TestMethod]
    public void Script_EhMergeIdempotente_NuncaApaga_ETerminaComCommit()
    {
        string script = ScriptGenerator.Script([Row(3550308, "São Paulo", "SP"), Row(3509502, "Campinas", "SP")], "teste", sample: false);

        StringAssert.Contains(script, "MERGE [Cities] AS t");
        StringAssert.Contains(script, "WHEN MATCHED AND");
        StringAssert.Contains(script, "WHEN NOT MATCHED THEN INSERT");
        StringAssert.Contains(script, "(3509502, N'Campinas', 'SP', N'campinas')");
        StringAssert.Contains(script, "(3550308, N'São Paulo', 'SP', N'sao paulo')");
        Assert.IsLessThan(script.IndexOf("3550308", StringComparison.Ordinal), script.IndexOf("3509502", StringComparison.Ordinal), "ordenado por código");
        Assert.DoesNotContain("DELETE", script, "município extinto não é apagado (BACKLOG)");
        Assert.DoesNotContain("AMOSTRA", script);
        StringAssert.Contains(script, "SET QUOTED_IDENTIFIER ON;");
        Assert.IsTrue(script.TrimEnd().EndsWith("GO", StringComparison.Ordinal));
        Assert.IsLessThan(script.IndexOf("COMMIT TRANSACTION", StringComparison.Ordinal), script.IndexOf("BEGIN TRANSACTION", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Script_EscapaAspasSimples_ENaoTemInjecao()
    {
        string script = ScriptGenerator.Script([Row(3545803, "Santa Bárbara d'Oeste", "SP"), Row(3550308, "X'); DROP TABLE Cities;--", "SP")], "teste", false);

        StringAssert.Contains(script, "N'Santa Bárbara d''Oeste'");
        StringAssert.Contains(script, "N'x''); drop table cities;--'");
        Assert.AreEqual(0, Regex.Matches(script, @"^\s*DROP", RegexOptions.Multiline).Count, "o nome nunca vira comando");
    }

    [TestMethod]
    public void Script_QuebraEmLotesDe500_ESaiIgualNaSegundaVez()
    {
        CityRow[] many = [.. Enumerable.Range(0, 1200).Select(i => Row(3500000 + i, "Cidade " + i, "SP"))];

        string first = ScriptGenerator.Script(many, "teste", false);
        string second = ScriptGenerator.Script([.. many.Reverse()], "teste", false);

        Assert.AreEqual(3, Regex.Matches(first, @"^MERGE \[Cities\]", RegexOptions.Multiline).Count, "1200 linhas = 500 + 500 + 200");
        Assert.AreEqual(first, second, "a ordem de entrada não muda o arquivo");
    }

    [TestMethod]
    public void Script_DaAmostra_AvisaQueNaoVaiParaProducao()
    {
        string script = ScriptGenerator.Script([Row(3550308, "São Paulo", "SP")], "sample", sample: true);

        StringAssert.Contains(script, "AMOSTRA DE TESTE");
        StringAssert.Contains(script, "NÃO aplique em produção");
    }

    [TestMethod]
    public async Task ScriptCommitado_BateComOQueAFerramentaGeraDoJsonDaAmostra()
    {
        string output = Ibge.TempFile("cities-sample.sql");

        (int code, _, _) = await RunAsync(["export", "--input", Repo.Path("db", "seed", "sample", "cities-sample.json"), "--source", "sample", "--out", output, "--sample"]);

        Assert.AreEqual(0, code);
        Assert.AreEqual(File.ReadAllText(Repo.Path("db", "seed", "sample", "cities-sample.sql")), File.ReadAllText(output), "o .sql da amostra está desatualizado: gere de novo");
    }

    [TestMethod]
    public async Task DadosRecusados_ParamSemGerarArquivo()
    {
        string input = WriteInput(Ibge.List(Ibge.City(3550308, "São Paulo", "SP"), Ibge.City(3550308, "Repetido", "SP")));
        string output = Ibge.TempFile("cities.sql");

        (int code, _, string error) = await RunAsync(["export", "--input", input, "--source", "teste", "--out", output]);

        Assert.AreEqual(4, code);
        StringAssert.Contains(error, "Carga recusada");
        Assert.IsFalse(Directory.Exists(Path.GetDirectoryName(output)), "nem a pasta de saída é criada");
    }

    [TestMethod]
    public async Task ArquivoInexistenteOuIlegivel_Uso2_SemGerarArquivo()
    {
        string output = Ibge.TempFile("cities.sql");

        (int missing, _, string error) = await RunAsync(["export", "--input", Ibge.TempFile("nao-existe.json"), "--source", "teste", "--out", output]);
        (int broken, _, _) = await RunAsync(["export", "--input", WriteInput("isto não é json"), "--source", "teste", "--out", output]);

        Assert.AreEqual(2, missing);
        StringAssert.Contains(error, "Não foi possível ler o arquivo do IBGE");
        Assert.AreEqual(2, broken);
        Assert.IsFalse(Directory.Exists(Path.GetDirectoryName(output)));
    }

    [TestMethod]
    public async Task UsoErrado_MostraAAjuda()
    {
        string input = WriteInput(Ibge.List(Ibge.City(3550308, "São Paulo", "SP")));
        (int none, _, string help) = await RunAsync([]);
        (int noInput, _, _) = await RunAsync(["export", "--source", "x", "--out", "a.sql"]);
        (int noOut, _, _) = await RunAsync(["export", "--input", input, "--source", "x"]);
        (int badSource, _, _) = await RunAsync(["export", "--input", input, "--source", "x'; DROP", "--out", "a.sql"]);
        (int noEnv, _, _) = await RunAsync(["load", "--input", input, "--source", "x"], (Cli.TargetVariable, DevTarget));

        Assert.AreEqual(2, none);
        StringAssert.Contains(help, "CitiesImport export");
        Assert.AreEqual(2, noInput);
        Assert.AreEqual(2, noOut);
        Assert.AreEqual(2, badSource, "o rótulo só aceita caracteres simples");
        Assert.AreEqual(2, noEnv);
    }

    [TestMethod]
    public async Task Carga_SemVariavelDeConexao_Para()
    {
        string input = WriteInput(Ibge.List(Ibge.City(3550308, "São Paulo", "SP")));

        (int code, _, string error) = await RunAsync(["load", "--input", input, "--source", "teste", "--environment", "Development"]);

        Assert.AreEqual(2, code);
        StringAssert.Contains(error, Cli.TargetVariable);
    }

    [TestMethod]
    [DataRow("Production", "Server=localhost;Database=gazeta_dev")]
    [DataRow("Development", "Server=prod-sql01;Database=gazeta")]
    [DataRow("Development", "Server=localhost;Database=gazeta_prod")]
    [DataRow("Staging", "Server=localhost;Database=gazeta_dev")]
    public async Task Carga_RecusaProducao_AntesDeLerOArquivoOuAbrirConexao(string environment, string connection)
    {
        (int code, _, string error) = await RunAsync(["load", "--input", Ibge.TempFile("nem-existe.json"), "--source", "teste", "--environment", environment], (Cli.TargetVariable, connection));

        Assert.AreEqual(4, code, "recusa antes de ler o arquivo");
        StringAssert.Contains(error, "carga em lote");
    }

    [TestMethod]
    public async Task Carga_BancoInacessivel_Falha3_SemMostrarACadeia()
    {
        string input = WriteInput(Ibge.List(Ibge.City(3550308, "São Paulo", "SP")));

        (int code, _, string error) = await RunAsync(["load", "--input", input, "--source", "teste", "--environment", "Development"], (Cli.TargetVariable, DevTarget));

        Assert.AreEqual(3, code);
        StringAssert.Contains(error, "foi desfeita");
        Assert.DoesNotContain("Password", error);
    }

    [TestMethod]
    public void Ferramenta_FicaForaDaSolucao_ENenhumArquivoTemCredencial()
    {
        string solution = File.ReadAllText(Repo.Path("GazetaMarketplace.slnx"));
        Regex credential = new(@"(Password|Pwd)\s*=\s*[^;\s""]+|User\s*Id\s*=\s*[^;\s""]+", RegexOptions.IgnoreCase);

        Assert.DoesNotContain("tools/CitiesImport", solution);
        StringAssert.Contains(solution, "CitiesImport.Tests");
        foreach (string file in Directory.EnumerateFiles(Repo.Path("tools", "CitiesImport"), "*.cs", SearchOption.AllDirectories).Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)))
        {
            Assert.IsFalse(credential.IsMatch(File.ReadAllText(file)), $"{Path.GetFileName(file)} contém credencial");
        }

        StringAssert.Contains(File.ReadAllText(Repo.Path("tools", "CitiesImport", "BatchLoader.cs")), "// Dapper: carga em lote; o EF Core geraria um INSERT por linha");
    }
}
