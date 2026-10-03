using System;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistence;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AuditingTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task Salvar_PreencheCriacaoEAlteracao()
    {
        using TestDatabase database = new();
        database.User.UserId = 5;
        int id;
        using (TestAppDbContext context = database.NewContext())
        {
            TestEntity entity = new() { Name = "primeira" };
            context.Entities.Add(entity);
            await context.SaveChangesAsync();
            id = entity.Id;
        }

        database.Clock.Now = new DateTimeOffset(2026, 10, 2, 13, 0, 0, TimeSpan.Zero);
        database.User.UserId = 6;
        using (TestAppDbContext context = database.NewContext())
        {
            TestEntity entity = await context.Entities.FindAsync(id);
            entity.Name = "editada";
            await context.SaveChangesAsync();
        }

        using TestAppDbContext reading = database.NewContext();
        TestEntity read = await reading.Entities.FindAsync(id);
        Assert.AreEqual(new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc), read.CreatedAt);
        Assert.AreEqual(5, read.CreatedBy);
        Assert.AreEqual(new DateTime(2026, 10, 2, 13, 0, 0, DateTimeKind.Utc), read.UpdatedAt);
        Assert.AreEqual(6, read.UpdatedBy);
    }

    [TestMethod]
    public async Task AcaoDoSistema_GravaUsuarioNulo()
    {
        using TestDatabase database = new();
        database.User.UserId = null;
        using TestAppDbContext context = database.NewContext();
        TestEntity entity = new() { Name = "do sistema" };
        context.Entities.Add(entity);

        await context.SaveChangesAsync();

        Assert.IsNull(entity.CreatedBy);
        Assert.IsNull(entity.UpdatedAt);
    }

    [TestMethod]
    public void SaveChangesSincrono_TambemPreencheAuditoria()
    {
        using TestDatabase database = new();
        database.User.UserId = 9;
        using TestAppDbContext context = database.NewContext();
        TestEntity entity = new() { Name = "sync" };
        context.Entities.Add(entity);

        context.SaveChanges();

        Assert.AreEqual(9, entity.CreatedBy);
        Assert.AreEqual(new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc), entity.CreatedAt);
    }

    [TestMethod]
    public async Task EditarUmaLinha_NaoAlteraACriacao()
    {
        using TestDatabase database = new();
        database.User.UserId = 5;
        int id;
        using (TestAppDbContext context = database.NewContext())
        {
            TestEntity entity = new() { Name = "a" };
            context.Entities.Add(entity);
            await context.SaveChangesAsync();
            id = entity.Id;
        }

        using (TestAppDbContext context = database.NewContext())
        {
            TestEntity entity = await context.Entities.FindAsync(id);
            entity.Name = "b";
            entity.CreatedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            entity.CreatedBy = 99;
            await context.SaveChangesAsync();
        }

        using TestAppDbContext reading = database.NewContext();
        TestEntity read = await reading.Entities.FindAsync(id);
        Assert.AreEqual(5, read.CreatedBy);
        Assert.AreEqual(2026, read.CreatedAt.Year);
    }

    [TestMethod]
    public async Task DatasLidasDoBanco_VoltamComoUtc()
    {
        using TestDatabase database = new();
        using (TestAppDbContext context = database.NewContext())
        {
            context.Entities.Add(new TestEntity { Name = "utc" });
            await context.SaveChangesAsync();
        }

        using TestAppDbContext reading = database.NewContext();
        TestEntity read = await reading.Entities.FindAsync(1);
        Assert.AreEqual(DateTimeKind.Utc, read.CreatedAt.Kind);
    }
}
