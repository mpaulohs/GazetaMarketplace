using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GazetaMarketplace.Infrastructure;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistence;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DapperTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void IDbConnection_E_Scoped_ComAMesmaCadeiaDoEfCore()
    {
        const string connectionString = "Server=(local);Database=Teste;Integrated Security=true";
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string> { ["ConnectionStrings:DefaultConnection"] = connectionString })
            .Build();
        ServiceCollection services = new();
        services.AddInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        using IServiceScope a = provider.CreateScope();
        using IServiceScope b = provider.CreateScope();
        IDbConnection fromA = a.ServiceProvider.GetRequiredService<IDbConnection>();
        IDbConnection fromA2 = a.ServiceProvider.GetRequiredService<IDbConnection>();
        IDbConnection fromB = b.ServiceProvider.GetRequiredService<IDbConnection>();

        Assert.IsInstanceOfType<SqlConnection>(fromA);
        Assert.AreSame(fromA, fromA2);
        Assert.AreNotSame(fromA, fromB);
        // O EF normaliza o texto (Data Source, Application Name); o servidor e o banco são os mesmos
        SqlConnectionStringBuilder fromDapper = new(fromA.ConnectionString);
        SqlConnectionStringBuilder fromEf = new(a.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetConnectionString());
        Assert.AreEqual("(local)", fromDapper.DataSource);
        Assert.AreEqual("Teste", fromDapper.InitialCatalog);
        Assert.AreEqual(fromDapper.DataSource, fromEf.DataSource);
        Assert.AreEqual(fromDapper.InitialCatalog, fromEf.InitialCatalog);
    }

    [TestMethod]
    public async Task EscritaDapper_NaTransacaoDoEf_DesfazComORollback()
    {
        using TestDatabase database = new();
        using TestAppDbContext context = database.NewContext();
        await using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await context.Database.BeginTransactionAsync())
        {
            (DbConnection connection, DbTransaction dbTransaction) = context.GetConnectionAndTransaction();
            Assert.IsNotNull(dbTransaction);
            await connection.ExecuteAsync(
                "INSERT INTO Entities (Name, CreatedAt) VALUES (@Name, @Now)",
                new { Name = "via dapper", Now = "2026-10-02 12:00:00" }, dbTransaction);

            Assert.AreEqual(1, await context.Entities.CountAsync(), "o EF enxerga a escrita do Dapper na mesma transação");
            await transaction.RollbackAsync();
        }

        Assert.AreEqual(0, await context.Entities.CountAsync());
    }

    [TestMethod]
    public async Task EscritaDapper_NaTransacaoDoEf_ConfirmaComOCommit()
    {
        using TestDatabase database = new();
        using TestAppDbContext context = database.NewContext();
        await using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await context.Database.BeginTransactionAsync())
        {
            (DbConnection connection, DbTransaction dbTransaction) = context.GetConnectionAndTransaction();
            await connection.ExecuteAsync(
                "INSERT INTO Entities (Name, CreatedAt) VALUES (@Name, @Now)",
                new { Name = "via dapper", Now = "2026-10-02 12:00:00" }, dbTransaction);
            await transaction.CommitAsync();
        }

        Assert.AreEqual(1, await context.Entities.CountAsync());
    }

    [TestMethod]
    public void ObterConexaoETransacao_SemTransacao_DevolveTransacaoNula()
    {
        using TestDatabase database = new();
        using TestAppDbContext context = database.NewContext();

        (DbConnection connection, DbTransaction transaction) = context.GetConnectionAndTransaction();

        Assert.IsNotNull(connection);
        Assert.IsNull(transaction);
    }
}
