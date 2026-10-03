using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// <see cref="SqlBuilder"/> e <see cref="SqlFragments"/> contra o T-SQL de verdade (ADR-004): filtro "somente publicados", ordenação por lista
/// permitida e <c>OFFSET/FETCH</c>. A tabela <c>Ads</c> só nasce na tarefa 3.1; aqui uma tabela de teste com o mesmo alias <c>a</c> e as mesmas colunas usadas.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DapperTests
#pragma warning restore CA1515
{
    private static readonly IReadOnlyDictionary<string, string> Allowed = new Dictionary<string, string>
    {
        ["preco"] = "a.PriceCents",
        ["titulo"] = "a.Title"
    };

    private static async Task<SqlConnection> CreateAdsAsync()
    {
        SqlConnection connection = new(await SqlServerFixture.CreateEmptyDatabaseAsync());
        await connection.OpenAsync();
        await connection.ExecuteAsync("""
            CREATE TABLE TestAds (
                Id int IDENTITY PRIMARY KEY,
                Title nvarchar(120) NOT NULL,
                Status tinyint NOT NULL,
                PriceCents bigint NULL,
                PublishedAt datetime2 NULL)
            """);

        // Dapper: massa de teste de uma tabela descartável, sem RowVersion nem auditoria a conferir
        for (int i = 1; i <= 30; i++)
        {
            await connection.ExecuteAsync(
                "INSERT INTO TestAds (Title, Status, PriceCents) VALUES (@Title, @Status, @Price)",
                new { Title = "Anúncio " + i.ToString("00", System.Globalization.CultureInfo.InvariantCulture), Status = (byte)(i % 3 == 0 ? 3 : 1 + (i % 2)), Price = i % 10 == 0 ? (long?)null : i * 1000L });
        }

        return connection;
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task SomentePublicados_DevolveSoStatusPublicado()
    {
        await using SqlConnection connection = await CreateAdsAsync();
        SqlQuery query = new SqlBuilder().Select("a.Id, a.Title, a.Status").From("TestAds a").OnlyPublished().OrderBy(null, null, Allowed, "a.Id").Build();

        List<dynamic> rows = (await connection.QueryAsync(query.Sql, query.Parameters)).ToList();

        Assert.AreEqual(10, rows.Count);
        Assert.IsTrue(rows.All(r => (byte)r.Status == SqlFragments.PublishedStatus));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Ordenacao_PorChavePermitida_AscEDesc_ComNulosNoInicioDoAsc()
    {
        await using SqlConnection connection = await CreateAdsAsync();

        SqlQuery asc = new SqlBuilder().Select("a.Id, a.PriceCents").From("TestAds a").OrderBy("preco", "asc", Allowed, "a.Id").Build();
        SqlQuery desc = new SqlBuilder().Select("a.Id, a.PriceCents").From("TestAds a").OrderBy("preco", "desc", Allowed, "a.Id").Build();
        List<long?> ascending = (await connection.QueryAsync<long?>(asc.Sql.Replace("a.Id, a.PriceCents", "a.PriceCents"), asc.Parameters)).ToList();
        List<long?> descending = (await connection.QueryAsync<long?>(desc.Sql.Replace("a.Id, a.PriceCents", "a.PriceCents"), desc.Parameters)).ToList();

        Assert.IsNull(ascending[0], "no T-SQL os nulos vêm primeiro na ordem crescente");
        Assert.AreEqual(1000L, ascending[3]);
        Assert.AreEqual(29000L, descending[0]);
        Assert.IsNull(descending[^1]);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task OrdenacaoForaDaLista_ComTentativaDeInjecao_CaiNaOrdemPadrao_ENadaEApagado()
    {
        await using SqlConnection connection = await CreateAdsAsync();
        SqlQuery query = new SqlBuilder().Select("a.Id").From("TestAds a").OrderBy("preco; DROP TABLE TestAds", "asc; DROP TABLE TestAds", Allowed, "a.Id DESC").Build();

        List<int> ids = (await connection.QueryAsync<int>(query.Sql, query.Parameters)).ToList();

        Assert.AreEqual(30, ids.Count);
        Assert.AreEqual(30, ids[0], "ordem padrão decrescente por Id");
        Assert.AreEqual(30, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TestAds"));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Paginacao_OffsetFetch_DevolveAJanelaCerta_EOUltimaPaginaParcial()
    {
        await using SqlConnection connection = await CreateAdsAsync();

        SqlQuery second = new SqlBuilder().Select("a.Id").From("TestAds a").OrderBy(null, null, Allowed, "a.Id").Page(2, 10).Build();
        SqlQuery last = new SqlBuilder().Select("a.Id").From("TestAds a").OrderBy(null, null, Allowed, "a.Id").Page(4, 8).Build();
        SqlQuery beyond = new SqlBuilder().Select("a.Id").From("TestAds a").OrderBy(null, null, Allowed, "a.Id").Page(9, 10).Build();

        CollectionAssert.AreEqual(Enumerable.Range(11, 10).ToArray(), (await connection.QueryAsync<int>(second.Sql, second.Parameters)).ToArray());
        CollectionAssert.AreEqual(new[] { 25, 26, 27, 28, 29, 30 }, (await connection.QueryAsync<int>(last.Sql, last.Parameters)).ToArray());
        Assert.AreEqual(0, (await connection.QueryAsync<int>(beyond.Sql, beyond.Parameters)).Count());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task FiltroPublicadoComPaginacaoEParametroDeTexto_FuncionaJunto_ETextoMaliciosoViraValor()
    {
        await using SqlConnection connection = await CreateAdsAsync();
        SqlQuery query = new SqlBuilder().Select("a.Id").From("TestAds a").OnlyPublished()
            .Where("a.Title LIKE @Title").Parameter("Title", "Anúncio 1%")
            .OrderBy("titulo", "desc", Allowed, "a.Id").Page(1, 5).Build();
        SqlQuery malicious = new SqlBuilder().Select("a.Id").From("TestAds a")
            .Where("a.Title LIKE @Title").Parameter("Title", "%'; DROP TABLE TestAds; --")
            .OrderBy(null, null, Allowed, "a.Id").Build();

        List<int> ids = (await connection.QueryAsync<int>(query.Sql, query.Parameters)).ToList();

        CollectionAssert.AreEqual(new[] { 18, 15, 12 }, ids, "publicados cujo título começa com 'Anúncio 1', do maior para o menor título");
        Assert.AreEqual(0, (await connection.QueryAsync<int>(malicious.Sql, malicious.Parameters)).Count());
        Assert.AreEqual(30, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TestAds"));
    }
}
