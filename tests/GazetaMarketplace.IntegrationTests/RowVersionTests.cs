using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>Entidade só de teste: ainda não existe entidade editável real (a primeira é Ads, na tarefa 3.1).</summary>
internal sealed class TestEntity : BaseEntity
{
    public string Name { get; set; }
}

internal sealed class TestAppDbContext(DbContextOptions<TestAppDbContext> options, ICurrentUser user, TimeProvider time)
    : AppDbContext(options, user, time)
{
    public DbSet<TestEntity> Entities => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestEntity>(e =>
        {
            e.ToTable("Entities");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });
    }
}

/// <summary>
/// Concorrência otimista com o <c>rowversion</c> do SQL Server (ADR-004). O SQLite não tem o tipo: o fixture dos testes de unidade o simula
/// com triggers, o que não é prova. Aqui o servidor gera o valor.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class RowVersionTests
#pragma warning restore CA1515
{
    private sealed class MovableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static async Task<(string ConnectionString, FakeCurrentUser User, MovableClock Clock)> StartAsync()
    {
        string connectionString = await SqlServerFixture.CreateEmptyDatabaseAsync();
        FakeCurrentUser user = new() { UserId = 7 };
        MovableClock clock = new();
        await using TestAppDbContext context = NewContext(connectionString, user, clock);
        await context.Database.EnsureCreatedAsync();
        return (connectionString, user, clock);
    }

    private static TestAppDbContext NewContext(string connectionString, FakeCurrentUser user, TimeProvider clock) =>
        new(new DbContextOptionsBuilder<TestAppDbContext>().UseSqlServer(connectionString).Options, user, clock);

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ColunaRowVersion_EDoTipoRowversionDoSqlServer()
    {
        (string connectionString, _, _) = await StartAsync();

        List<string> type = await ScriptTests.QueryAsync(
            connectionString, "SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Entities' AND COLUMN_NAME = 'RowVersion'");

        CollectionAssert.AreEqual(new[] { "timestamp" }, type, "rowversion aparece como timestamp no catálogo do SQL Server");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task SalvarNovaEntidade_ORowVersionVemDoServidor_EAuditoriaPreencheCriacao()
    {
        (string connectionString, FakeCurrentUser user, MovableClock clock) = await StartAsync();
        await using TestAppDbContext context = NewContext(connectionString, user, clock);
        TestEntity entity = new() { Name = "primeira" };

        context.Entities.Add(entity);
        await context.SaveChangesAsync();

        Assert.IsNotNull(entity.RowVersion);
        Assert.AreEqual(8, entity.RowVersion.Length);
        Assert.AreEqual(clock.Now.UtcDateTime, entity.CreatedAt);
        Assert.AreEqual(7, entity.CreatedBy);
        Assert.IsNull(entity.UpdatedAt);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task EditarUmaLinha_TrocaORowVersion_PreenchesAEdicao_EMantemACriacao()
    {
        (string connectionString, FakeCurrentUser user, MovableClock clock) = await StartAsync();
        await using TestAppDbContext context = NewContext(connectionString, user, clock);
        TestEntity entity = new() { Name = "antes" };
        context.Entities.Add(entity);
        await context.SaveChangesAsync();
        byte[] before = entity.RowVersion;
        DateTime created = entity.CreatedAt;

        clock.Now += TimeSpan.FromHours(2);
        user.UserId = 9;
        entity.Name = "depois";
        entity.CreatedAt = DateTime.UtcNow.AddYears(-5); // tentativa de reescrever a criação
        await context.SaveChangesAsync();

        CollectionAssert.AreNotEqual(before, entity.RowVersion);
        Assert.AreEqual(clock.Now.UtcDateTime, entity.UpdatedAt);
        Assert.AreEqual(9, entity.UpdatedBy);

        await using TestAppDbContext fresh = NewContext(connectionString, user, clock);
        TestEntity reloaded = await fresh.Entities.AsNoTracking().SingleAsync();
        Assert.AreEqual(created, reloaded.CreatedAt, "a criação não muda");
        Assert.AreEqual(DateTimeKind.Utc, reloaded.CreatedAt.Kind, "datas voltam do banco como UTC");
        Assert.AreEqual(7, reloaded.CreatedBy);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DuasEdicoesDaMesmaLinha_AsegundaViraConflictException_ENaoSobrescreve()
    {
        (string connectionString, FakeCurrentUser user, MovableClock clock) = await StartAsync();
        await using TestAppDbContext seed = NewContext(connectionString, user, clock);
        seed.Entities.Add(new TestEntity { Name = "original" });
        await seed.SaveChangesAsync();

        await using TestAppDbContext first = NewContext(connectionString, user, clock);
        await using TestAppDbContext second = NewContext(connectionString, user, clock);
        TestEntity byFirst = await first.Entities.SingleAsync();
        TestEntity bySecond = await second.Entities.SingleAsync();

        byFirst.Name = "da primeira pessoa";
        await first.SaveChangesAsync();
        bySecond.Name = "da segunda pessoa";

        await Assert.ThrowsExactlyAsync<ConflictException>(() => second.SaveChangesAsync(CancellationToken.None));
        await using TestAppDbContext check = NewContext(connectionString, user, clock);
        Assert.AreEqual("da primeira pessoa", (await check.Entities.AsNoTracking().SingleAsync()).Name);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task EdicoesEmSequencia_NoMesmoContexto_NaoGeramConflito()
    {
        (string connectionString, FakeCurrentUser user, MovableClock clock) = await StartAsync();
        await using TestAppDbContext context = NewContext(connectionString, user, clock);
        TestEntity entity = new() { Name = "um" };
        context.Entities.Add(entity);
        await context.SaveChangesAsync();

        entity.Name = "dois";
        await context.SaveChangesAsync();
        entity.Name = "três";
        await context.SaveChangesAsync();

        Assert.AreEqual("três", (await NewContext(connectionString, user, clock).Entities.AsNoTracking().SingleAsync()).Name);
    }
}
