using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// Prontidão do banco (<c>/health/ready</c>, ADR-010) contra um SQL Server de verdade. O SQLite dos testes de unidade não distingue
/// "banco sem migration" de "banco pronto" do mesmo jeito, e é a diferença que o servidor de hospedagem precisa enxergar.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ReadinessTests
#pragma warning restore CA1515
{
    [TestMethod]
    [TestCategory("Integration")]
    public async Task BancoVazio_EstaAcessivel_MasSemMigrationAplicada()
    {
        string connectionString = await SqlServerFixture.CreateEmptyDatabaseAsync();
        await using AppDbContext context = SqlServerFixture.NewContext(connectionString);

        DatabaseReadiness state = await new EfDatabaseReadiness(context).CheckAsync(CancellationToken.None);

        Assert.IsTrue(state.DatabaseReachable);
        Assert.IsFalse(state.MigrationApplied);
        Assert.IsFalse(state.IsReady);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task BancoMigrado_EstaPronto()
    {
        string connectionString = await SqlServerFixture.CreateMigratedDatabaseAsync();
        await using AppDbContext context = SqlServerFixture.NewContext(connectionString);

        DatabaseReadiness state = await new EfDatabaseReadiness(context).CheckAsync(CancellationToken.None);

        Assert.IsTrue(state.IsReady);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task BancoMigradoSoPelaPrimeiraMigration_NaoEstaPronto()
    {
        // Esquecer de aplicar o script mais novo é o risco que a prontidão existe para pegar
        string connectionString = await SqlServerFixture.CreateMigratedDatabaseAsync();
        await using (SqlConnection connection = new(connectionString))
        {
            await connection.OpenAsync();
            await using SqlCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM __EFMigrationsHistory WHERE MigrationId LIKE '%AddIdentity'";
            await command.ExecuteNonQueryAsync();
        }

        await using AppDbContext context = SqlServerFixture.NewContext(connectionString);
        DatabaseReadiness state = await new EfDatabaseReadiness(context).CheckAsync(CancellationToken.None);

        Assert.IsTrue(state.DatabaseReachable);
        Assert.IsFalse(state.MigrationApplied);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task BancoQueNaoExiste_NaoEstaAcessivel()
    {
        string missing = new SqlConnectionStringBuilder(await SqlServerFixture.CreateEmptyDatabaseAsync()) { InitialCatalog = "nao_existe_" + System.Guid.NewGuid().ToString("N"), ConnectTimeout = 5 }.ConnectionString;
        await using AppDbContext context = SqlServerFixture.NewContext(missing);

        DatabaseReadiness state = await new EfDatabaseReadiness(context).CheckAsync(CancellationToken.None);

        Assert.IsFalse(state.DatabaseReachable);
        Assert.IsFalse(state.IsReady);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task HealthReady_Responde200ComBancoMigrado_E503ComBancoSemMigration()
    {
        using IntegrationWebFactory ready = new(await SqlServerFixture.CreateMigratedDatabaseAsync());
        using IntegrationWebFactory notMigrated = new(await SqlServerFixture.CreateEmptyDatabaseAsync());

        Assert.AreEqual(HttpStatusCode.OK, (await ready.CreateClient().GetAsync("/health/ready")).StatusCode);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, (await notMigrated.CreateClient().GetAsync("/health/ready")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await notMigrated.CreateClient().GetAsync("/health/live")).StatusCode, "vivo não depende do banco");
    }
}
