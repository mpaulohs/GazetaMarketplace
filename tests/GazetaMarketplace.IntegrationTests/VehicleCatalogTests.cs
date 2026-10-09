using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.VehicleCatalog;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VehicleCatalogExport;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// O catálogo de veículos de ponta a ponta no SQL Server real (ADR-008): origem simulada → exportação → script e carga em lote → tabelas do site →
/// endpoints. O catálogo reduzido de teste tem ids que colidem entre carros e motos (Honda = 1 nos dois) para provar a chave composta.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class VehicleCatalogTests
#pragma warning restore CA1515
{
    private const int Brands = 10;
    private const int Models = 45;
    private const int Years = 214;
    private const int Versions = 307;

    private static readonly string GoldenScript = SqlServerFixture.RepositoryPath("db", "seed", "sample", "vehicle-catalog-sample.sql");

    // ---------- ajudantes ----------

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> QueryAsync(string connectionString, string sql)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        List<string> rows = [];
        while (await reader.ReadAsync())
        {
            rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));
        }

        return rows;
    }

    /// <summary>Banco de origem simulado: o esquema real do GazetaOnline (colunas CarBrandId, CarModelId…) com o catálogo reduzido de teste.</summary>
    private static async Task<string> OriginAsync(bool withOrphans = false)
    {
        string connection = await SqlServerFixture.CreateEmptyDatabaseAsync();
        await ExecuteAsync(connection, await File.ReadAllTextAsync(SqlServerFixture.RepositoryPath("tests", "VehicleCatalogExport.Tests", "Data", "sample-catalog.sql")));
        if (withOrphans)
        {
            await ExecuteAsync(connection, """
                INSERT INTO CarModels (CarModelId, CarBrandId, Name) VALUES (900, 99, N'Modelo de marca que não existe');
                INSERT INTO CarYearModels (CarYearModelId, CarModelId, Year) VALUES (900, 99999, N'2020'), (901, 28, N'2020');
                INSERT INTO CarVersions (CarVersionId, CarYearModelId, Name) VALUES (900, 99999, N'Versão de ano que não existe');
                INSERT INTO MotorcycleVersions (MotorcycleVersionId, MotorcycleYearModelId, Name) VALUES (900, 99999, N'Versão sem ano');
                """);
        }

        return connection;
    }

    private static async Task<(int Code, string Output, string Script, string Report)> ExportAsync(string origin, string source = "sample")
    {
        string folder = Path.Combine(Path.GetTempPath(), "vce-int-" + Guid.NewGuid().ToString("N"));
        string file = Path.Combine(folder, "vehicle-catalog.sql");
        string report = Path.Combine(folder, "orphans.txt");
        using StringWriter output = new();
        using StringWriter error = new();
        int code = await Cli.RunAsync(
            ["export", "--source", source, "--out", file, "--report", report],
            name => name == Cli.OriginVariable ? origin : null, output, error, CancellationToken.None);
        Assert.AreEqual(0, code, error.ToString());
        string script = File.ReadAllText(file);
        string reportText = File.ReadAllText(report);
        Directory.Delete(folder, recursive: true);
        return (code, output.ToString(), script, reportText);
    }

    private static async Task ApplyAsync(string connectionString, string script)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        // O script é aplicado como o sqlcmd faz: lote a lote, separado por GO
        foreach (string batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline).Where(b => !string.IsNullOrWhiteSpace(b)))
        {
            await using SqlCommand command = connection.CreateCommand();
            command.CommandText = batch;
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task<int[]> CountsAsync(string connectionString)
    {
        List<string> row = await QueryAsync(connectionString,
            "SELECT (SELECT COUNT(*) FROM VehicleBrands), (SELECT COUNT(*) FROM VehicleModels), (SELECT COUNT(*) FROM VehicleModelYears), (SELECT COUNT(*) FROM VehicleVersions)");
        return [.. row[0].Split('|').Select(int.Parse)];
    }

    private static async Task<string[]> DumpAsync(string connectionString)
    {
        List<string> rows = [];
        rows.AddRange(await QueryAsync(connectionString, "SELECT 'B', Kind, Id, '', Name, Source FROM VehicleBrands ORDER BY Kind, Id"));
        rows.AddRange(await QueryAsync(connectionString, "SELECT 'M', Kind, Id, BrandId, Name, Source FROM VehicleModels ORDER BY Kind, Id"));
        rows.AddRange(await QueryAsync(connectionString, "SELECT 'Y', Kind, ModelId, Year, '', Source FROM VehicleModelYears ORDER BY Kind, ModelId, Year"));
        rows.AddRange(await QueryAsync(connectionString, "SELECT 'V', Kind, Id, CONCAT(ModelId, '/', Year), Name, Source FROM VehicleVersions ORDER BY Kind, Id"));
        return [.. rows];
    }

    private static async Task<string> LoadedByScriptAsync(string source = "sample")
    {
        string target = await SqlServerFixture.CreateMigratedDatabaseAsync();
        await ApplyAsync(target, (await ExportAsync(await OriginAsync(), source)).Script);
        return target;
    }

    // ---------- exportação ----------

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Exportacao_DoCatalogoReduzido_TemAsContagensEsperadas_SemDescartes_ECoincideComOScriptVersionado()
    {
        (int _, string output, string script, string report) = await ExportAsync(await OriginAsync());

        StringAssert.Contains(output, $"Marcas: {Brands} | Modelos: {Models} | Anos: {Years} | Versões: {Versions} | Descartados: 0");
        Assert.AreEqual("Nenhum registro descartado.\n", report);
        Assert.AreEqual(
            File.ReadAllText(GoldenScript).Replace("\r\n", "\n", StringComparison.Ordinal),
            script.Replace("\r\n", "\n", StringComparison.Ordinal),
            "db/seed/sample/vehicle-catalog-sample.sql está desatualizado: gere de novo com a ferramenta (ver db/seed/README.md)");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Orfaos_DaOrigem_SaoDescartadosERelatados_ENaoEntramNoScript()
    {
        (int _, string output, string script, string report) = await ExportAsync(await OriginAsync(withOrphans: true));

        StringAssert.Contains(output, $"Marcas: {Brands} | Modelos: {Models} | Anos: {Years} | Versões: {Versions} | Descartados: 5");
        StringAssert.Contains(report, "Registros descartados: 5");
        foreach (string line in new[] { "car\tmodelo\t900\tsem marca", "car\tano\t99999/2020\tsem modelo", "car\tversão\t900\tsem ano", "moto\tversão\t900\tsem ano" })
        {
            StringAssert.Contains(report, line);
        }

        // O quinto descarte é o ano 2020 do modelo 28, que o catálogo já tem (ano repetido no modelo)
        StringAssert.Contains(report, "car\tano\t28/2020\tano repetido no modelo");
        Assert.DoesNotContain("Modelo de marca que não existe", script);
        Assert.DoesNotContain("Versão sem ano", script);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Origem_ComAnoEmTextoNaoNumerico_ENomeDeVersaoNulo_VaiParaORelatorio_SemQuebrarALeitura()
    {
        string origin = await OriginAsync();
        await ExecuteAsync(origin, """
            INSERT INTO CarYearModels (CarYearModelId, CarModelId, Year) VALUES (950, 1, N'Zero km'), (951, 1, NULL), (952, 1, N'1900'), (953, 1, N' 2031 ');
            INSERT INTO CarVersions (CarVersionId, CarYearModelId, Name) VALUES (950, 950, N'Versão de ano em texto'), (951, 1, NULL), (952, 1, N'   ');
            """);

        (int _, string output, string script, string report) = await ExportAsync(origin);

        // 2031 (com espaços) é um ano válido para o site (até 2100) e entra; "Zero km", nulo e 1900 não
        StringAssert.Contains(output, $"Marcas: {Brands} | Modelos: {Models} | Anos: {Years + 1} | Versões: {Versions} | Descartados: 6");
        foreach (string line in new[] { "car\tano\t1/\tano inválido", "car\tano\t1/1900\tano fora de 1950 a 2100", "car\tversão\t950\tsem ano", "car\tversão\t951\tsem nome", "car\tversão\t952\tsem nome" })
        {
            StringAssert.Contains(report, line);
        }

        Assert.DoesNotContain("Versão de ano em texto", script);
        StringAssert.Contains(script, "(1, 2031, 'car')");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Origem_ComIsPublishedEmZero_TrazOCatalogoInteiro_PoisOFlagNaoEDeCuradoria()
    {
        string origin = await OriginAsync();

        CollectionAssert.AreEqual(new[] { "0" }, await QueryAsync(origin, "SELECT COUNT(*) FROM CarBrands WHERE IsPublished = 1"), "premissa do teste: nenhuma marca marcada como publicada");
        (int _, string output, _, _) = await ExportAsync(origin);

        StringAssert.Contains(output, $"Marcas: {Brands} | Modelos: {Models} | Anos: {Years} | Versões: {Versions} | Descartados: 0");
    }

    // ---------- script e carga em lote ----------

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Script_AplicadoDuasVezes_NaoDuplicaNada_ETemAsContagensDoCatalogo()
    {
        string target = await SqlServerFixture.CreateMigratedDatabaseAsync();
        string script = (await ExportAsync(await OriginAsync())).Script;

        await ApplyAsync(target, script);
        CollectionAssert.AreEqual(new[] { Brands, Models, Years, Versions }, await CountsAsync(target));
        string[] first = await DumpAsync(target);

        await ApplyAsync(target, script);

        CollectionAssert.AreEqual(new[] { Brands, Models, Years, Versions }, await CountsAsync(target));
        CollectionAssert.AreEqual(first, await DumpAsync(target), "a segunda aplicação não muda nenhuma linha");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CargaEmLote_DeixaOMesmoBancoQueOScript_ERodarDuasVezesNaoDuplica()
    {
        string byScript = await LoadedByScriptAsync();
        string byBatch = await SqlServerFixture.CreateMigratedDatabaseAsync();
        ValidationResult result = CatalogValidator.Validate(await OriginReader.ReadAsync(await OriginAsync(), CancellationToken.None));

        await BatchLoader.LoadAsync(byBatch, "Testing", result.Data, "sample", CancellationToken.None);
        await BatchLoader.LoadAsync(byBatch, "Testing", result.Data, "sample", CancellationToken.None);

        CollectionAssert.AreEqual(new[] { Brands, Models, Years, Versions }, await CountsAsync(byBatch));
        CollectionAssert.AreEqual(await DumpAsync(byScript), await DumpAsync(byBatch), "mesmas linhas, mesmos valores, mesma origem");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CargaEmLote_ViaLinhaDeComando_CarregaEConfereAContagem()
    {
        string origin = await OriginAsync();
        string target = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using StringWriter output = new();
        using StringWriter error = new();

        int code = await Cli.RunAsync(
            ["load", "--source", "sample", "--environment", "Testing"],
            name => name == Cli.OriginVariable ? origin : name == Cli.TargetVariable ? target : null, output, error, CancellationToken.None);

        Assert.AreEqual(0, code, error.ToString());
        StringAssert.Contains(output.ToString(), "Carga concluída.");
        CollectionAssert.AreEqual(new[] { Brands, Models, Years, Versions }, await CountsAsync(target));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CargaEmLote_RecusaProducao_ENaoGravaNada()
    {
        string origin = await OriginAsync();
        string target = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using StringWriter output = new();
        using StringWriter error = new();

        int code = await Cli.RunAsync(
            ["load", "--source", "sample", "--environment", "Production"],
            name => name == Cli.OriginVariable ? origin : name == Cli.TargetVariable ? target : null, output, error, CancellationToken.None);

        Assert.AreEqual(4, code);
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, await CountsAsync(target));
        ValidationResult result = CatalogValidator.Validate(await OriginReader.ReadAsync(origin, CancellationToken.None));
        _ = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => BatchLoader.LoadAsync(target, "Production", result.Data, "sample", CancellationToken.None));
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, await CountsAsync(target));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CargaQueFalhaNoMeio_FoiDesfeitaPorInteiro()
    {
        string target = await SqlServerFixture.CreateMigratedDatabaseAsync();
        ValidationResult result = CatalogValidator.Validate(await OriginReader.ReadAsync(await OriginAsync(), CancellationToken.None));
        // Uma versão apontando para um ano que não existe: as marcas, modelos e anos já gravados precisam voltar atrás
        CatalogData broken = result.Data with { Versions = [.. result.Data.Versions, new VersionRow(9999, Kind.Car, 1, 1999, "Quebrada")] };

        _ = await Assert.ThrowsExactlyAsync<SqlException>(() => BatchLoader.LoadAsync(target, "Testing", broken, "sample", CancellationToken.None));

        CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, await CountsAsync(target));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task NovaCargaComOutraOrigem_AtualizaAOrigemEOsNomes_SemMudarAsContagens()
    {
        string target = await LoadedByScriptAsync("sample");
        await ExecuteAsync(target, "UPDATE VehicleBrands SET Name = N'honda errada' WHERE Id = 1 AND Kind = 'car'; DELETE FROM VehicleVersions WHERE Id = 1 AND Kind = 'moto'");

        await ApplyAsync(target, (await ExportAsync(await OriginAsync(), "fipe-2027-01")).Script);

        CollectionAssert.AreEqual(new[] { Brands, Models, Years, Versions }, await CountsAsync(target));
        Assert.AreEqual(0, (await QueryAsync(target, "SELECT 1 FROM (SELECT Source FROM VehicleBrands UNION ALL SELECT Source FROM VehicleModels UNION ALL SELECT Source FROM VehicleModelYears UNION ALL SELECT Source FROM VehicleVersions) x WHERE Source <> 'fipe-2027-01'")).Count);
        CollectionAssert.AreEqual(new[] { "Honda" }, await QueryAsync(target, "SELECT Name FROM VehicleBrands WHERE Id = 1 AND Kind = 'car'"), "o nome corrompido voltou ao da origem (comparação exata)");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ScriptLidoSemUtf8_AbortaEDesfazTudo_PelaGuardaDeAcentos()
    {
        string target = await SqlServerFixture.CreateMigratedDatabaseAsync();
        string script = (await ExportAsync(await OriginAsync())).Script;
        // O sqlcmd do Windows sem -f 65001 lê os bytes UTF-8 como Latin-1: "Ã" + resto do acento. Reproduz exatamente isso.
        string misread = System.Text.Encoding.Latin1.GetString(new System.Text.UTF8Encoding(false).GetBytes(script));
        Assert.Contains("Ã", misread, "premissa do teste: o catálogo reduzido tem nome com acento");

        SqlException failure = await Assert.ThrowsExactlyAsync<SqlException>(() => ApplyAsync(target, misread));

        StringAssert.Contains(failure.Message, "Acentos corrompidos");
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, await CountsAsync(target), "a transação inteira foi desfeita");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ScriptLidoComUtf8_PassaPelaGuardaDeAcentos_ENomesComAcentoLegitimoEntram()
    {
        string origin = await OriginAsync();
        await ExecuteAsync(origin, "INSERT INTO CarBrands (CarBrandId, Name) VALUES (800, N'ÂNGULO São Paulo Edition'), (801, N'Água Ação');");
        string target = await SqlServerFixture.CreateMigratedDatabaseAsync();

        await ApplyAsync(target, (await ExportAsync(origin)).Script);

        CollectionAssert.AreEqual(new[] { "ÂNGULO São Paulo Edition", "Água Ação" }, await QueryAsync(target, "SELECT Name FROM VehicleBrands WHERE Kind = 'car' AND Id IN (800, 801) ORDER BY Id"));
    }

    // ---------- chaves e restrições no SQL Server ----------

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ChavesCompostas_AceitamOMesmoIdEmCarroEMoto_ERecusamOQueNaoFecha()
    {
        string target = await LoadedByScriptAsync();

        CollectionAssert.AreEqual(new[] { "car|Honda", "moto|Honda" }, await QueryAsync(target, "SELECT Kind, Name FROM VehicleBrands WHERE Id = 1 ORDER BY Kind"), "o mesmo id nos dois tipos");
        _ = await Assert.ThrowsExactlyAsync<SqlException>(() => ExecuteAsync(target, "INSERT INTO VehicleBrands (Id, Kind, Name, Source) VALUES (1, 'car', N'Repetida', 'x')"), "a chave (1, car) já existe");
        _ = await Assert.ThrowsExactlyAsync<SqlException>(() => ExecuteAsync(target, "INSERT INTO VehicleBrands (Id, Kind, Name, Source) VALUES (50, 'bike', N'Tipo inválido', 'x')"), "o tipo só pode ser car ou moto");
        _ = await Assert.ThrowsExactlyAsync<SqlException>(() => ExecuteAsync(target, "INSERT INTO VehicleModels (Id, Kind, BrandId, Name, Source) VALUES (500, 'moto', 9, N'Marca de moto que não existe', 'x')"), "a marca 9 não existe nas motos");
        _ = await Assert.ThrowsExactlyAsync<SqlException>(() => ExecuteAsync(target, "INSERT INTO VehicleModelYears (ModelId, Year, Kind, Source) VALUES (999, 2020, 'car', 'x')"), "o modelo 999 não existe");
        _ = await Assert.ThrowsExactlyAsync<SqlException>(() => ExecuteAsync(target, "INSERT INTO VehicleVersions (Id, Kind, ModelId, Year, Name, Source) VALUES (9000, 'car', 1, 1999, N'Ano que o modelo não tem', 'x')"), "o Civic não tem 1999");
        _ = await Assert.ThrowsExactlyAsync<SqlException>(() => ExecuteAsync(target, "DELETE FROM VehicleBrands WHERE Id = 1 AND Kind = 'car'"), "marca com modelos não se apaga (Restrict)");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ModeloSoDeCarro_NaoAceitaAnoNemVersaoComoSeFosseDeMoto()
    {
        string target = await LoadedByScriptAsync();

        // O modelo 20 (Toro) existe em carros e o 20 não existe em motos (só vão até 17): o ano 2020 de moto para ele não pode entrar
        _ = await Assert.ThrowsExactlyAsync<SqlException>(() => ExecuteAsync(target, "INSERT INTO VehicleModelYears (ModelId, Year, Kind, Source) VALUES (20, 2020, 'moto', 'x')"));
        CollectionAssert.AreEqual(new[] { Brands, Models, Years, Versions }, await CountsAsync(target));
    }

    // ---------- endpoints sobre o SQL Server real ----------

    private static async Task<(HttpStatusCode Status, JsonElement Body, HttpResponseMessage Response)> GetAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        string text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, JsonDocument.Parse(text).RootElement, response);
    }

    private static string[] Names(JsonElement array) => [.. array.EnumerateArray().Select(e => e.GetProperty("name").GetString())];

    private static int[] Ints(JsonElement array) => [.. array.EnumerateArray().Select(e => e.GetInt32())];

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Endpoints_PorTipo_CarroEMotoComIdsIguais_DaoListasDiferentes()
    {
        using IntegrationWebFactory factory = new(await LoadedByScriptAsync());
        using HttpClient client = factory.CreateBrowser();

        CollectionAssert.AreEqual(new[] { "Chevrolet", "Fiat", "Honda", "Toyota", "Volkswagen" }, Names((await GetAsync(client, "/api/v1/vehicle-catalog/brands?kind=car")).Body));
        CollectionAssert.AreEqual(new[] { "BMW", "Honda", "Kawasaki", "Suzuki", "Yamaha" }, Names((await GetAsync(client, "/api/v1/vehicle-catalog/brands?kind=moto")).Body));

        // A Honda de carros e a Honda de motos são a marca 1 nas duas
        CollectionAssert.AreEqual(new[] { "City", "Civic", "CR-V", "Fit", "HR-V" }, Names((await GetAsync(client, "/api/v1/vehicle-catalog/brands/1/models?kind=car")).Body));
        CollectionAssert.AreEqual(new[] { "Biz 125", "CB 500", "CG 160", "PCX", "XRE 300" }, Names((await GetAsync(client, "/api/v1/vehicle-catalog/brands/1/models?kind=moto")).Body));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Endpoints_AnosEVersoes_SeguemAOrdemDoContrato_EOsCasosDeBorda()
    {
        using IntegrationWebFactory factory = new(await LoadedByScriptAsync());
        using HttpClient client = factory.CreateBrowser();

        int[] civicYears = Ints((await GetAsync(client, "/api/v1/vehicle-catalog/models/1/years?kind=car")).Body);
        CollectionAssert.AreEqual(civicYears.OrderByDescending(y => y).ToArray(), civicYears, "anos do mais novo ao mais antigo");
        Assert.AreEqual(2016, civicYears[^1]);

        string[] versions2019 = Names((await GetAsync(client, "/api/v1/vehicle-catalog/models/1/years/2019/versions?kind=car")).Body);
        Assert.HasCount(3, versions2019, "ano com várias versões");
        CollectionAssert.AreEqual(versions2019.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray(), versions2019, "versões em ordem alfabética");

        Assert.AreEqual(0, (await GetAsync(client, "/api/v1/vehicle-catalog/models/1/years/2016/versions?kind=car")).Body.GetArrayLength(), "ano sem versões: 200 e lista vazia");
        Assert.AreEqual(0, (await GetAsync(client, "/api/v1/vehicle-catalog/models/8/years?kind=car")).Body.GetArrayLength(), "Etios não tem anos: 200 e lista vazia");
        (HttpStatusCode mobiStatus, JsonElement mobi, _) = await GetAsync(client, "/api/v1/vehicle-catalog/models/19/years?kind=car");
        Assert.AreEqual(HttpStatusCode.OK, mobiStatus);
        Assert.IsGreaterThan(0, mobi.GetArrayLength(), "Mobi tem anos");
        Assert.AreEqual(0, (await GetAsync(client, $"/api/v1/vehicle-catalog/models/19/years/{Ints(mobi)[0]}/versions?kind=car")).Body.GetArrayLength(), "Mobi tem anos e nenhuma versão");
        Assert.AreEqual(0, (await GetAsync(client, "/api/v1/vehicle-catalog/brands/4/models?kind=moto")).Body.GetArrayLength(), "Suzuki (moto) não tem modelos");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Endpoints_ErrosSeguemOContrato_404_400_ECabecalhoDeCache()
    {
        using IntegrationWebFactory factory = new(await LoadedByScriptAsync());
        using HttpClient client = factory.CreateBrowser();

        (HttpStatusCode brand, JsonElement brandBody, _) = await GetAsync(client, "/api/v1/vehicle-catalog/brands/99/models?kind=car");
        Assert.AreEqual(HttpStatusCode.NotFound, brand);
        Assert.AreEqual("NOT_FOUND", brandBody.GetProperty("code").GetString());
        Assert.AreEqual(HttpStatusCode.NotFound, (await GetAsync(client, "/api/v1/vehicle-catalog/models/99/years?kind=moto")).Status);
        Assert.AreEqual(HttpStatusCode.NotFound, (await GetAsync(client, "/api/v1/vehicle-catalog/models/1/years/2005/versions?kind=car")).Status, "o Civic não tem 2005");
        Assert.AreEqual(HttpStatusCode.BadRequest, (await GetAsync(client, "/api/v1/vehicle-catalog/brands")).Status, "sem kind");
        Assert.AreEqual(HttpStatusCode.BadRequest, (await GetAsync(client, "/api/v1/vehicle-catalog/brands?kind=bike")).Status);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await GetAsync(client, "/api/v1/vehicle-catalog/models/1/years/1900/versions?kind=car")).Status, "ano fora de 1950 a 2100");

        (_, _, HttpResponseMessage ok) = await GetAsync(client, "/api/v1/vehicle-catalog/brands?kind=car");
        Assert.AreEqual(TimeSpan.FromMinutes(10), ok.Headers.CacheControl.MaxAge);
        Assert.IsTrue(ok.Headers.CacheControl.Public);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task TrocarAOrigem_NaoMudaOQueOsEndpointsDevolvem()
    {
        string target = await LoadedByScriptAsync("sample");
        using IntegrationWebFactory factory = new(target);
        using HttpClient client = factory.CreateBrowser();
        string before = (await GetAsync(client, "/api/v1/vehicle-catalog/models/1/years/2019/versions?kind=car")).Body.GetRawText();

        await ApplyAsync(target, (await ExportAsync(await OriginAsync(), "fipe-2027-01")).Script);
        factory.Services.GetRequiredService<IVehicleCatalog>().Invalidate();

        Assert.AreEqual(before, (await GetAsync(client, "/api/v1/vehicle-catalog/models/1/years/2019/versions?kind=car")).Body.GetRawText());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ScriptEMigrations_DaoOMesmoEsquemaParaOCatalogo()
    {
        // O script idempotente das migrations (que vai ao provedor) cria as quatro tabelas, os índices das chaves e as restrições
        string byScript = await SqlServerFixture.CreateEmptyDatabaseAsync();
        await SqlServerFixture.ApplyScriptAsync(byScript);
        const string schema = "SELECT TABLE_NAME + '.' + COLUMN_NAME + ':' + DATA_TYPE + ':' + IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME LIKE 'Vehicle%' ORDER BY 1";

        CollectionAssert.AreEqual(
            await QueryAsync(await SqlServerFixture.CreateMigratedDatabaseAsync(), schema),
            await QueryAsync(byScript, schema));
        Assert.AreEqual(3, (await QueryAsync(byScript, "SELECT name FROM sys.foreign_keys WHERE name LIKE 'FK_Vehicle%'")).Count);
        await using AppDbContext context = SqlServerFixture.NewContext(byScript);
        Assert.AreEqual(0, await context.VehicleBrands.CountAsync(), "as tabelas nascem vazias: a carga é um passo à parte");
    }
}
