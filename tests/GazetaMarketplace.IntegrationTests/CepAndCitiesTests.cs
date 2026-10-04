using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CitiesImport;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Location;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// <c>CepCache</c> e <c>Cities</c> no SQL Server real (tarefa 3.2): o CHECK do CEP, o índice único de município, a corrida de duas consultas ao mesmo CEP
/// e a carga idempotente dos municípios pelo script e pelo carregador em lote da ferramenta <c>CitiesImport</c>.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CepAndCitiesTests
#pragma warning restore CA1515
{
    private const int CheckViolation = 547;
    private const int DuplicateKey = 2601;
    private const int PrimaryKeyViolation = 2627;

    /// <summary>Monta o JSON do IBGE à mão (o formato com a UF na microrregião).</summary>
    private static class IbgeJson
    {
        public static string City(int id, string name, string uf) =>
            "{\"id\":" + id + ",\"nome\":\"" + name + "\",\"microrregiao\":{\"mesorregiao\":{\"UF\":{\"sigla\":\"" + uf + "\"}}}}";

        public static string List(params string[] cities) => "[" + string.Join(",", cities) + "]";
    }

    private sealed class CountingLookup : ICepLookup
    {
        private readonly Barrier _start;

        public CountingLookup(Barrier start) => _start = start;

        public Task<CepLookupResult> LookupAsync(string cep, CancellationToken cancellationToken)
        {
            _start.SignalAndWait(); // as duas chamadas só seguem juntas: as duas já viram o cache vazio
            return Task.FromResult(new CepLookupResult("campinas", "SP", 3509502));
        }
    }

    private static async Task<List<string>> ApplyScriptAsync(string connection, string script)
    {
        await using SqlConnection db = new(connection);
        await db.OpenAsync();
        await using (SqlCommand off = db.CreateCommand())
        {
            off.CommandText = "SET QUOTED_IDENTIFIER OFF"; // como o sqlcmd sem -I: o script tem de ligar por conta própria
            await off.ExecuteNonQueryAsync();
        }

        foreach (string batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline).Where(b => !string.IsNullOrWhiteSpace(b)))
        {
            await using SqlCommand command = db.CreateCommand();
            command.CommandText = batch;
            await command.ExecuteNonQueryAsync();
        }

        return await AdData.QueryAsync(connection, "SELECT CAST(IbgeCode AS varchar(10)) + '|' + Name + '|' + Uf + '|' + NameSearch FROM Cities ORDER BY IbgeCode");
    }

    private static string SampleJson() => File.ReadAllText(SqlServerFixture.RepositoryPath("db", "seed", "sample", "cities-sample.json"));

    [TestMethod]
    [TestCategory("Integration")]
    public async Task EsquemaReal_CepCacheEMunicipios_ChecksEIndiceUnico()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        const string cache = "INSERT INTO CepCache (Cep, City, Uf, IbgeCode, FetchedAt) VALUES (@c, N'Campinas', 'SP', 3509502, SYSUTCDATETIME())";
        const string city = "INSERT INTO Cities (IbgeCode, Name, Uf, NameSearch) VALUES (@code, @name, 'SP', @search)";

        Assert.AreEqual(0, await AdData.TryExecuteAsync(connection, cache, ("@c", "13015100")));
        Assert.AreEqual(PrimaryKeyViolation, await AdData.TryExecuteAsync(connection, cache, ("@c", "13015100")), "chave primária: um CEP, uma linha");
        foreach (string bad in new[] { "1301510", "1301510a", "abcdefgh", "130151000" })
        {
            Assert.IsTrue(await AdData.TryExecuteAsync(connection, cache, ("@c", bad)) is CheckViolation or 8152 or 2628, $"CEP '{bad}'");
        }

        Assert.AreEqual(0, await AdData.TryExecuteAsync(connection, city, ("@code", 3509502), ("@name", "Campinas"), ("@search", "campinas")));
        Assert.AreEqual(DuplicateKey, await AdData.TryExecuteAsync(connection, city, ("@code", 3509999), ("@name", "CAMPINAS"), ("@search", "campinas")), "mesmo nome normalizado na mesma UF");
        Assert.AreEqual(0, await AdData.TryExecuteAsync(connection, "INSERT INTO Cities (IbgeCode, Name, Uf, NameSearch) VALUES (3300001, N'Campinas', 'RJ', N'campinas')"), "o mesmo nome em outra UF é normal");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DuasConsultasDoMesmoCepAoMesmoTempo_UmaLinhaNoCache_AsDuasRecebemResposta()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();

        for (int round = 1; round <= 6; round++)
        {
            string cep = (13015100 + round).ToString(System.Globalization.CultureInfo.InvariantCulture);
            using Barrier start = new(2);
            async Task<CepResult> OneAsync()
            {
                await using AppDbContext context = SqlServerFixture.NewContext(connection);
                CepService service = new(context, new CountingLookup(start), new CityDirectory(context), TimeProvider.System);
                return await service.GetAsync(cep, CancellationToken.None);
            }

            CepResult[] results = await Task.WhenAll(Task.Run(OneAsync), Task.Run(OneAsync));

            Assert.IsTrue(results.All(r => r.City == "Campinas" && r.Source == "viacep"), $"rodada {round}");
            await using AppDbContext check = SqlServerFixture.NewContext(connection);
            Assert.AreEqual(1, await check.CepCache.CountAsync(e => e.Cep == cep), $"rodada {round}: uma linha só");
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CepService_ComMunicipiosCarregados_UsaONomeOficial_ESemCarga_AplicaARegra()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        await ApplyScriptAsync(connection, File.ReadAllText(SqlServerFixture.RepositoryPath("db", "seed", "sample", "cities-sample.sql")));
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        CepService service = new(context, new StubLookup(new CepLookupResult("SAO JOSE DO RIO PRETO", "SP", 3549805)), new CityDirectory(context), TimeProvider.System);

        CepResult official = await service.GetAsync("15000000", CancellationToken.None);
        CepResult standardized = await new CepService(context, new StubLookup(new CepLookupResult("DIAMANTINA", "MG", 3121605)), new CityDirectory(context), TimeProvider.System).GetAsync("39100000", CancellationToken.None);

        Assert.AreEqual("São José do Rio Preto", official.City, "nome oficial da amostra, com acentos");
        Assert.AreEqual("Diamantina", standardized.City, "fora da amostra: vale a regra da SPEC");
    }

    private sealed class StubLookup(CepLookupResult result) : ICepLookup
    {
        public Task<CepLookupResult> LookupAsync(string cep, CancellationToken cancellationToken) => Task.FromResult(result);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ScriptDaAmostra_Aplica40Municipios_EAplicarDeNovoNaoDuplicaNemMudaNada()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        string script = File.ReadAllText(SqlServerFixture.RepositoryPath("db", "seed", "sample", "cities-sample.sql"));

        List<string> first = await ApplyScriptAsync(connection, script);
        List<string> second = await ApplyScriptAsync(connection, script);

        Assert.HasCount(40, first);
        CollectionAssert.AreEqual(first, second);
        CollectionAssert.Contains(first, "3545803|Santa Bárbara d'Oeste|SP|santa barbara d'oeste");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task NovaCarga_AtualizaONomeCorrigido_NaoApagaOQueSaiuDoArquivo_EAdicionaOsNovos()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        await ApplyScriptAsync(connection, ScriptOf(IbgeJson.List(IbgeJson.City(3550308, "sao paulo", "SP"), IbgeJson.City(3509502, "Campinas", "SP"))));

        List<string> after = await ApplyScriptAsync(connection, ScriptOf(IbgeJson.List(IbgeJson.City(3550308, "São Paulo", "SP"), IbgeJson.City(3525904, "Jundiaí", "SP"))));

        CollectionAssert.AreEqual(
            new[] { "3509502|Campinas|SP|campinas", "3525904|Jundiaí|SP|jundiai", "3550308|São Paulo|SP|sao paulo" },
            after,
            "corrigiu o nome, acrescentou Jundiaí e manteve Campinas (o MERGE nunca apaga)");
    }

    private static string ScriptOf(string json)
    {
        ValidationResult result = CityValidator.Validate(IbgeReader.Read(json));
        Assert.IsTrue(result.IsValid, CityValidator.Report(result));
        return ScriptGenerator.Script(result.Cities, "teste", sample: false);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CargaEmLote_GravaOsMesmosMunicipiosDoScript_ERecusaProducao()
    {
        string viaScript = await SqlServerFixture.CreateMigratedDatabaseAsync();
        string viaLoader = await SqlServerFixture.CreateMigratedDatabaseAsync();
        string input = Path.Combine(Path.GetTempPath(), "ci-" + Guid.NewGuid().ToString("N") + ".json");
        await File.WriteAllTextAsync(input, SampleJson());
        await ApplyScriptAsync(viaScript, File.ReadAllText(SqlServerFixture.RepositoryPath("db", "seed", "sample", "cities-sample.sql")));

        using StringWriter output = new();
        using StringWriter error = new();
        int code = await Cli.RunAsync(["load", "--input", input, "--source", "sample", "--environment", "Testing"], name => name == Cli.TargetVariable ? viaLoader : null, output, error, CancellationToken.None);
        int refused = await Cli.RunAsync(["load", "--input", input, "--source", "sample", "--environment", "Production"], name => name == Cli.TargetVariable ? viaLoader : null, output, error, CancellationToken.None);

        Assert.AreEqual(0, code, error.ToString());
        Assert.AreEqual(4, refused);
        string query = "SELECT CAST(IbgeCode AS varchar(10)) + '|' + Name + '|' + Uf + '|' + NameSearch FROM Cities ORDER BY IbgeCode";
        CollectionAssert.AreEqual(await AdData.QueryAsync(viaScript, query), await AdData.QueryAsync(viaLoader, query), "o carregador roda os mesmos MERGE do script");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task NameSearchDoScript_EIgualAoNormalizerDoSite_ParaTodoMunicipioDaAmostra()
    {
        // Uma só implementação (a ferramenta usa o Normalizer do Core), conferida contra o que o banco guardou
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        await ApplyScriptAsync(connection, File.ReadAllText(SqlServerFixture.RepositoryPath("db", "seed", "sample", "cities-sample.sql")));
        await using AppDbContext context = SqlServerFixture.NewContext(connection);

        List<City> stored = await context.Cities.AsNoTracking().ToListAsync();

        Assert.HasCount(40, stored);
        Assert.IsTrue(stored.All(c => c.NameSearch == GazetaMarketplace.Core.Search.Normalizer.Normalize(c.Name)));
        Assert.IsTrue(stored.All(c => BrazilianStates.FromMunicipalityCode(c.IbgeCode)?.Uf == c.Uf), "o código de cada município da amostra é da UF dele");
    }
}
