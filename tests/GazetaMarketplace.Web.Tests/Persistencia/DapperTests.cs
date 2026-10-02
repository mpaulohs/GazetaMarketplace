using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GazetaMarketplace.Infrastructure;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistencia;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DapperTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void IDbConnection_E_Scoped_ComAMesmaCadeiaDoEfCore()
    {
        const string cadeia = "Server=(local);Database=Teste;Integrated Security=true";
        IConfiguration configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string> { ["ConnectionStrings:DefaultConnection"] = cadeia })
            .Build();
        ServiceCollection servicos = new();
        servicos.AddInfrastructure(configuracao);
        using ServiceProvider provedor = servicos.BuildServiceProvider(validateScopes: true);

        using IServiceScope a = provedor.CreateScope();
        using IServiceScope b = provedor.CreateScope();
        IDbConnection daA = a.ServiceProvider.GetRequiredService<IDbConnection>();
        IDbConnection daA2 = a.ServiceProvider.GetRequiredService<IDbConnection>();
        IDbConnection daB = b.ServiceProvider.GetRequiredService<IDbConnection>();

        Assert.IsInstanceOfType<SqlConnection>(daA);
        Assert.AreSame(daA, daA2);
        Assert.AreNotSame(daA, daB);
        // O EF normaliza o texto (Data Source, Application Name); o servidor e o banco são os mesmos
        SqlConnectionStringBuilder doDapper = new(daA.ConnectionString);
        SqlConnectionStringBuilder doEf = new(a.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetConnectionString());
        Assert.AreEqual("(local)", doDapper.DataSource);
        Assert.AreEqual("Teste", doDapper.InitialCatalog);
        Assert.AreEqual(doDapper.DataSource, doEf.DataSource);
        Assert.AreEqual(doDapper.InitialCatalog, doEf.InitialCatalog);
    }

    [TestMethod]
    public async Task EscritaDapper_NaTransacaoDoEf_DesfazComORollback()
    {
        using BancoDeTestes banco = new();
        using AppDbContextDeTeste contexto = banco.NovoContexto();
        await using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transacao = await contexto.Database.BeginTransactionAsync())
        {
            (DbConnection conexao, DbTransaction dbTransacao) = contexto.ObterConexaoETransacao();
            Assert.IsNotNull(dbTransacao);
            await conexao.ExecuteAsync(
                "INSERT INTO Entidades (Nome, CreatedAt) VALUES (@Nome, @Agora)",
                new { Nome = "via dapper", Agora = "2026-10-02 12:00:00" }, dbTransacao);

            Assert.AreEqual(1, await contexto.Entidades.CountAsync(), "o EF enxerga a escrita do Dapper na mesma transação");
            await transacao.RollbackAsync();
        }

        Assert.AreEqual(0, await contexto.Entidades.CountAsync());
    }

    [TestMethod]
    public async Task EscritaDapper_NaTransacaoDoEf_ConfirmaComOCommit()
    {
        using BancoDeTestes banco = new();
        using AppDbContextDeTeste contexto = banco.NovoContexto();
        await using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transacao = await contexto.Database.BeginTransactionAsync())
        {
            (DbConnection conexao, DbTransaction dbTransacao) = contexto.ObterConexaoETransacao();
            await conexao.ExecuteAsync(
                "INSERT INTO Entidades (Nome, CreatedAt) VALUES (@Nome, @Agora)",
                new { Nome = "via dapper", Agora = "2026-10-02 12:00:00" }, dbTransacao);
            await transacao.CommitAsync();
        }

        Assert.AreEqual(1, await contexto.Entidades.CountAsync());
    }

    [TestMethod]
    public void ObterConexaoETransacao_SemTransacao_DevolveTransacaoNula()
    {
        using BancoDeTestes banco = new();
        using AppDbContextDeTeste contexto = banco.NovoContexto();

        (DbConnection conexao, DbTransaction transacao) = contexto.ObterConexaoETransacao();

        Assert.IsNotNull(conexao);
        Assert.IsNull(transacao);
    }
}
