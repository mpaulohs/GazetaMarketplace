using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistencia;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ProntidaoDoBancoTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task BancoAcessivel_SemMigrationAplicada_NaoEstaPronto()
    {
        using BancoDeTestes banco = new();
        using AppDbContext contexto = banco.NovoAppDbContext();

        ProntidaoDoBanco estado = await new ProntidaoDoBancoEf(contexto).VerificarAsync(default);

        Assert.IsTrue(estado.BancoAcessivel);
        Assert.IsFalse(estado.MigrationAplicada);
        Assert.IsFalse(estado.Pronto);
    }

    [TestMethod]
    public async Task BancoAcessivel_ComTodasAsMigrationsAplicadas_EstaPronto()
    {
        using BancoDeTestes banco = new();
        using AppDbContext contexto = banco.NovoAppDbContext();
        // O SQLite não executa as migrations (são em T-SQL); só o histórico importa para a prontidão
        await contexto.Database.ExecuteSqlRawAsync(contexto.GetService<IHistoryRepository>().GetCreateScript());
        foreach (string id in contexto.Database.GetMigrations())
        {
            await contexto.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, '10.0.12')", id);
        }

        ProntidaoDoBanco estado = await new ProntidaoDoBancoEf(contexto).VerificarAsync(default);

        Assert.IsGreaterThan(0, contexto.Database.GetMigrations().Count());
        Assert.IsTrue(estado.Pronto);
    }

    [TestMethod]
    public async Task BancoInacessivel_NaoEstaPronto()
    {
        using AppDbContext contexto = new(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=/pasta-que-nao-existe/x.db").Options,
            new UsuarioFalso(), new RelogioFalso());

        ProntidaoDoBanco estado = await new ProntidaoDoBancoEf(contexto).VerificarAsync(default);

        Assert.IsFalse(estado.BancoAcessivel);
        Assert.IsFalse(estado.Pronto);
    }
}
