using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VehicleCatalogExport.Tests;

/// <summary>A recusa da carga em produção e os caminhos de falha da linha de comando: nada é gerado quando algo está errado.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class GuardAndCliTests
#pragma warning restore CA1515
{
    // Servidor que não existe: a conexão é recusada na hora (porta 1 de loopback) e o teste não espera rede
    private const string Unreachable = "Server=127.0.0.1,1;Database=gazeta_origem;User Id=somente_leitura;Password=x;Encrypt=False;Connect Timeout=2";
    private const string DevTarget = "Server=127.0.0.1,1;Database=gazeta_dev;User Id=dev;Password=x;Encrypt=False;Connect Timeout=2";

    [TestMethod]
    [DataRow("Production")]
    [DataRow("production")]
    [DataRow("Staging")]
    [DataRow("")]
    [DataRow(null)]
    public void Guarda_RecusaQualquerAmbienteQueNaoSejaDevelopmentOuTesting(string environment)
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => ProductionGuard.EnsureNotProduction(DevTarget, environment));
    }

    [TestMethod]
    [DataRow("Server=prod-sql01;Database=gazeta;Integrated Security=true")]
    [DataRow("Server=localhost;Database=Gazeta_PRODUCAO_prod;Integrated Security=true")]
    [DataRow("Server=localhost;Database=gazeta;Application Name=Gazeta-Production;Integrated Security=true")]
    public void Guarda_RecusaCadeiaMarcadaComoProducao_MesmoEmDevelopment(string connection)
    {
        InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(() => ProductionGuard.EnsureNotProduction(connection, "Development"));
        StringAssert.Contains(error.Message, "produção");
    }

    [TestMethod]
    [DataRow("Development")]
    [DataRow("Testing")]
    public void Guarda_AceitaBancoDeDesenvolvimentoEDeTeste(string environment)
    {
        ProductionGuard.EnsureNotProduction("Server=localhost,14330;Database=gazeta_dev;User Id=sa;Password=x", environment);
    }

    private static async Task<(int Code, string Out, string Err)> RunAsync(string[] args, params (string Name, string Value)[] variables)
    {
        using StringWriter output = new();
        using StringWriter error = new();
        int code = await Cli.RunAsync(args, name => variables.FirstOrDefault(v => v.Name == name).Value, output, error, CancellationToken.None);
        return (code, output.ToString(), error.ToString());
    }

    private static string TempFile() => Path.Combine(Path.GetTempPath(), "vce-" + Guid.NewGuid().ToString("N"), "vehicle-catalog.sql");

    [TestMethod]
    public async Task SemVariavelDeConexao_ParaSemGerarArquivo()
    {
        string file = TempFile();

        (int code, _, string error) = await RunAsync(["export", "--source", "teste", "--out", file]);

        Assert.AreEqual(2, code);
        StringAssert.Contains(error, Cli.OriginVariable);
        Assert.IsFalse(File.Exists(file));
        Assert.IsFalse(Directory.Exists(Path.GetDirectoryName(file)), "nem a pasta de saída é criada");
    }

    [TestMethod]
    public async Task ConexaoRecusada_ErroClaro_ENenhumArquivoParcial()
    {
        string file = TempFile();

        (int code, _, string error) = await RunAsync(["export", "--source", "teste", "--out", file], (Cli.OriginVariable, Unreachable));

        Assert.AreEqual(3, code);
        StringAssert.Contains(error, "Não foi possível ler a origem");
        Assert.IsFalse(Directory.Exists(Path.GetDirectoryName(file)), "nenhum arquivo, nem temporário");
        Assert.DoesNotContain("Password", error, "a mensagem não repete a cadeia de conexão");
    }

    [TestMethod]
    public async Task UsoErrado_MostraAAjuda()
    {
        (int none, _, string help) = await RunAsync([]);
        (int noSource, _, _) = await RunAsync(["export", "--out", "x.sql"], (Cli.OriginVariable, Unreachable));
        (int noOut, _, _) = await RunAsync(["export", "--source", "teste"], (Cli.OriginVariable, Unreachable));
        (int unknown, _, _) = await RunAsync(["apagar"]);

        Assert.AreEqual(2, none);
        StringAssert.Contains(help, "Uso:");
        Assert.AreEqual(2, noSource);
        Assert.AreEqual(2, noOut);
        Assert.AreEqual(2, unknown);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("com espaço")]
    [DataRow("aspas'")]
    [DataRow("a-origem-com-mais-de-sessenta-caracteres-nao-cabe-na-coluna-source-ok")]
    public async Task RotuloDeOrigemInvalido_EUsoErrado(string source)
    {
        (int code, _, _) = await RunAsync(["export", "--source", source, "--out", TempFile()], (Cli.OriginVariable, Unreachable));

        Assert.AreEqual(2, code);
    }

    [TestMethod]
    public async Task Carga_ComAmbienteProducao_ERecusadaAntesDeTocarNaOrigem()
    {
        // A origem é inalcançável: se a recusa viesse depois da leitura, o código seria 3, não 4
        (int code, _, string error) = await RunAsync(
            ["load", "--source", "teste", "--environment", "Production"], (Cli.OriginVariable, Unreachable), (Cli.TargetVariable, DevTarget));

        Assert.AreEqual(4, code);
        StringAssert.Contains(error, "Development ou Testing");
    }

    [TestMethod]
    public async Task Carga_ContraCadeiaMarcadaComoProducao_ERecusada()
    {
        (int code, _, _) = await RunAsync(
            ["load", "--source", "teste", "--environment", "Development"],
            (Cli.OriginVariable, Unreachable),
            (Cli.TargetVariable, "Server=prod-sql01;Database=gazeta;Integrated Security=true"));

        Assert.AreEqual(4, code);
    }

    [TestMethod]
    public async Task Carga_SemVariavelDeDestino_ParaComUsoErrado()
    {
        (int code, _, string error) = await RunAsync(["load", "--source", "teste", "--environment", "Development"], (Cli.OriginVariable, Unreachable));

        Assert.AreEqual(2, code);
        StringAssert.Contains(error, Cli.TargetVariable);
    }
}
