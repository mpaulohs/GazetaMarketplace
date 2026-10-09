using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Configuration;

/// <summary>
/// As variáveis de ambiente do site em produção moram no <c>web.config</c> do servidor, e a publicação gera um <c>web.config</c> novo, sem elas: um
/// pacote publicado por cima do servidor derrubava o site (502, sem log, porque até o caminho dos logs vinha de uma variável). A publicação passou a
/// mesclar o <c>web.Production.config</c> (fora do git, na máquina de quem publica) no <c>web.config</c> gerado. Estes testes rodam só o alvo de
/// mesclagem do MSBuild, sobre arquivos de mentira, sem publicar nada.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ProductionConfigMergeTests
#pragma warning restore CA1515
{
    private const string Secret = "SEGREDO-QUE-NAO-PODE-APARECER-NO-LOG-123";

    // O que o SDK gera na publicação: o módulo do IIS e, às vezes, só o ASPNETCORE_ENVIRONMENT
    private const string GeneratedWebConfig = """
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <location path="." inheritInChildApplications="false">
            <system.webServer>
              <handlers>
                <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
              </handlers>
              <aspNetCore processPath=".\GazetaMarketplace.Web.exe" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="OutOfProcess">
                <environmentVariables>
                  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development" />
                </environmentVariables>
              </aspNetCore>
            </system.webServer>
          </location>
        </configuration>
        """;

    private const string ProductionConfig = $$"""
        <?xml version="1.0" encoding="utf-8"?>
        <configuration xmlns:xdt="http://schemas.microsoft.com/XML-Document-Transform">
          <location>
            <system.webServer>
              <aspNetCore>
                <environmentVariables xdt:Transform="InsertIfMissing">
                  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" xdt:Transform="InsertIfMissing" />
                  <environmentVariable name="ConnectionStrings__DefaultConnection" value="Server=x;Password={{Secret}}" xdt:Transform="InsertIfMissing" />
                  <environmentVariable name="Logging__FileDirectory" value="h:\logs" xdt:Transform="InsertIfMissing" />
                  <environmentVariable name="ForwardedHeaders__Cloudflare" value="true" xdt:Transform="InsertIfMissing" />
                </environmentVariables>
              </aspNetCore>
            </system.webServer>
          </location>
        </configuration>
        """;

    private static string TargetsFile => RepositoryRoot.FullPath("src", "GazetaMarketplace.Web", "MergeProductionConfig.targets");

    private sealed record Run(int ExitCode, string Output, string WebConfigPath);

    /// <summary>Roda o alvo <c>MergeProductionConfig</c> numa pasta temporária; <paramref name="production"/> nulo = sem o arquivo web.Production.config.</summary>
    private static async Task<Run> MergeAsync(string webConfig, string production, string productionPath = null)
    {
        string folder = Path.Combine(Path.GetTempPath(), "mpc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string webConfigPath = Path.Combine(folder, "web.config");
        await File.WriteAllTextAsync(webConfigPath, webConfig);
        string productionFile = productionPath ?? Path.Combine(folder, "web.Production.config");
        if (production is not null)
        {
            await File.WriteAllTextAsync(productionFile, production);
        }

        string project = Path.Combine(folder, "merge.proj");
        await File.WriteAllTextAsync(project, $"""
            <Project>
              <Import Project="{TargetsFile}" />
              <Target Name="Run" DependsOnTargets="MergeProductionConfig" />
            </Project>
            """);

        ProcessStartInfo start = new("dotnet")
        {
            ArgumentList = { "msbuild", project, "-t:Run", "-nologo", "-v:n", $"-p:PublishIntermediateOutputPath={folder}{Path.DirectorySeparatorChar}", $"-p:ProductionConfigFile={productionFile}" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = folder
        };
        using Process process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new Run(process.ExitCode, await output + await error, webConfigPath);
    }

    private static Dictionary<string, string> Variables(string webConfigPath) =>
        XDocument.Load(webConfigPath).Descendants("environmentVariable")
            .ToDictionary(e => (string)e.Attribute("name"), e => (string)e.Attribute("value"));

    [TestMethod]
    public async Task ComOArquivo_AsVariaveisEntramNoWebConfigGerado_ESobrescrevemAsDoSdk()
    {
        Run run = await MergeAsync(GeneratedWebConfig, ProductionConfig);

        Assert.AreEqual(0, run.ExitCode, run.Output);
        Dictionary<string, string> variables = Variables(run.WebConfigPath);
        Assert.AreEqual("Production", variables["ASPNETCORE_ENVIRONMENT"], "o valor do arquivo de produção vale sobre o que o SDK pôs");
        Assert.AreEqual("Server=x;Password=" + Secret, variables["ConnectionStrings__DefaultConnection"]);
        Assert.AreEqual("h:\\logs", variables["Logging__FileDirectory"]);
        Assert.AreEqual("true", variables["ForwardedHeaders__Cloudflare"]);
        Assert.AreEqual(4, variables.Count, "uma linha por nome, sem repetir");
        Directory.Delete(Path.GetDirectoryName(run.WebConfigPath)!, recursive: true);
    }

    [TestMethod]
    public async Task ComOArquivo_OResto_DoWebConfig_FicaIgual()
    {
        Run run = await MergeAsync(GeneratedWebConfig, ProductionConfig);

        XElement aspNetCore = XDocument.Load(run.WebConfigPath).Descendants("aspNetCore").Single();
        Assert.AreEqual("OutOfProcess", (string)aspNetCore.Attribute("hostingModel"));
        Assert.AreEqual(".\\GazetaMarketplace.Web.exe", (string)aspNetCore.Attribute("processPath"));
        Assert.IsNotNull(XDocument.Load(run.WebConfigPath).Descendants("add").SingleOrDefault(a => (string)a.Attribute("name") == "aspNetCore"), "o módulo do IIS continua");
        Directory.Delete(Path.GetDirectoryName(run.WebConfigPath)!, recursive: true);
    }

    [TestMethod]
    public async Task SemBlocoDeVariaveis_NoWebConfigGerado_OBlocoECriado()
    {
        string withoutBlock = GeneratedWebConfig
            .Replace("<environmentVariables>", string.Empty, StringComparison.Ordinal)
            .Replace("""<environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development" />""", string.Empty, StringComparison.Ordinal)
            .Replace("</environmentVariables>", string.Empty, StringComparison.Ordinal);

        Run run = await MergeAsync(withoutBlock, ProductionConfig);

        Assert.AreEqual(0, run.ExitCode, run.Output);
        Assert.AreEqual(4, Variables(run.WebConfigPath).Count);
        Directory.Delete(Path.GetDirectoryName(run.WebConfigPath)!, recursive: true);
    }

    [TestMethod]
    public async Task NaSaida_AparecemOsNomes_NuncaOsValores()
    {
        Run run = await MergeAsync(GeneratedWebConfig, ProductionConfig);

        Assert.AreEqual(0, run.ExitCode, run.Output);
        StringAssert.Contains(run.Output, "ConnectionStrings__DefaultConnection");
        StringAssert.Contains(run.Output, "4 variáveis");
        Assert.DoesNotContain(Secret, run.Output, "senha e chaves não vão para o log da publicação");
        Directory.Delete(Path.GetDirectoryName(run.WebConfigPath)!, recursive: true);
    }

    [TestMethod]
    public async Task SemOArquivo_PublicaMesmoAssim_MasAvisaEmDestaque_ENaoMexeNoWebConfig()
    {
        Run run = await MergeAsync(GeneratedWebConfig, production: null);

        Assert.AreEqual(0, run.ExitCode, run.Output);
        StringAssert.Contains(run.Output, "web.Production.config não encontrado");
        StringAssert.Contains(run.Output, "SEM as variáveis de ambiente");
        Assert.AreEqual(1, Variables(run.WebConfigPath).Count, "continua só com o que o SDK pôs");
        Directory.Delete(Path.GetDirectoryName(run.WebConfigPath)!, recursive: true);
    }

    [TestMethod]
    public async Task WebConfigSemOModuloDoAspNetCore_FalhaEmVezDeSairSemVariaveis()
    {
        Run run = await MergeAsync("<?xml version=\"1.0\"?><configuration><system.webServer /></configuration>", ProductionConfig);

        Assert.AreNotEqual(0, run.ExitCode, "publicar um pacote sem as variáveis é o erro que este alvo existe para evitar");
        StringAssert.Contains(run.Output, "aspNetCore");
        Directory.Delete(Path.GetDirectoryName(run.WebConfigPath)!, recursive: true);
    }

    [TestMethod]
    public async Task ArquivoDeProducaoQuebrado_Falha_SemVazarOConteudo()
    {
        Run run = await MergeAsync(GeneratedWebConfig, "<configuration><oops>" + Secret);

        Assert.AreNotEqual(0, run.ExitCode, run.Output);
        StringAssert.Contains(run.Output, "web.Production.config");
        Assert.DoesNotContain(Secret, run.Output);
        Directory.Delete(Path.GetDirectoryName(run.WebConfigPath)!, recursive: true);
    }

    [TestMethod]
    public async Task OExemploDoRepositorio_PodeSerMesclado()
    {
        // O exemplo é o modelo do arquivo real: se ele deixar de ser mesclável, o passo a passo do runbook deixa de funcionar
        string example = RepositoryRoot.FullPath("src", "GazetaMarketplace.Web", "web.Production.config.example");

        Run run = await MergeAsync(GeneratedWebConfig, await File.ReadAllTextAsync(example));

        Assert.AreEqual(0, run.ExitCode, run.Output);
        Dictionary<string, string> variables = Variables(run.WebConfigPath);
        foreach (string required in new[] { "ConnectionStrings__DefaultConnection", "Logging__FileDirectory", "PhotoStorage__BasePath", "DataProtection__KeysDirectory", "SendGrid__ApiKey", "Site__BaseUrl", "ForwardedHeaders__Cloudflare" })
        {
            Assert.IsTrue(variables.ContainsKey(required), required);
        }

        Directory.Delete(Path.GetDirectoryName(run.WebConfigPath)!, recursive: true);
    }
}
