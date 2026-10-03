using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistence;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DatabaseReadinessTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task BancoAcessivel_SemMigrationAplicada_NaoEstaPronto()
    {
        using TestDatabase database = new();
        using AppDbContext context = database.NewAppDbContext();

        DatabaseReadiness state = await new EfDatabaseReadiness(context).CheckAsync(default);

        Assert.IsTrue(state.DatabaseReachable);
        Assert.IsFalse(state.MigrationApplied);
        Assert.IsFalse(state.IsReady);
    }

    [TestMethod]
    public async Task BancoAcessivel_ComTodasAsMigrationsAplicadas_EstaPronto()
    {
        using TestDatabase database = new();
        using AppDbContext context = database.NewAppDbContext();
        // O SQLite não executa as migrations (são em T-SQL); só o histórico importa para a prontidão
        await context.Database.ExecuteSqlRawAsync(context.GetService<IHistoryRepository>().GetCreateScript());
        foreach (string id in context.Database.GetMigrations())
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, '10.0.12')", id);
        }

        DatabaseReadiness state = await new EfDatabaseReadiness(context).CheckAsync(default);

        Assert.IsGreaterThan(0, context.Database.GetMigrations().Count());
        Assert.IsTrue(state.IsReady);
    }

    [TestMethod]
    public async Task BancoInacessivel_NaoEstaPronto()
    {
        using AppDbContext context = new(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=/pasta-que-nao-existe/x.db").Options,
            new FakeCurrentUser(), new FakeClock());

        DatabaseReadiness state = await new EfDatabaseReadiness(context).CheckAsync(default);

        Assert.IsFalse(state.DatabaseReachable);
        Assert.IsFalse(state.IsReady);
    }
}
