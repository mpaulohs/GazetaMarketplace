using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Persistence;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ConcurrencyTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task DuasEdicoes_DaMesmaLinha_GeramConflito()
    {
        using TestDatabase database = new();
        using TestAppDbContext first = database.NewContext();
        using TestAppDbContext second = database.NewContext();
        first.Entities.Add(new TestEntity { Name = "original" });
        await first.SaveChangesAsync();

        TestEntity fromFirst = await first.Entities.FindAsync(1);
        TestEntity fromSecond = await second.Entities.FindAsync(1);
        fromFirst.Name = "da primeira pessoa";
        fromSecond.Name = "da segunda pessoa";
        await first.SaveChangesAsync();

        ConflictException error = await Assert.ThrowsExactlyAsync<ConflictException>(() => second.SaveChangesAsync());
        Assert.AreEqual("CONFLICT", error.Code);
        Assert.AreEqual(409, error.StatusCode);
    }

    [TestMethod]
    public async Task EdicoesEmSequencia_NoMesmoContexto_NaoGeramConflito()
    {
        using TestDatabase database = new();
        using TestAppDbContext context = database.NewContext();
        TestEntity entity = new() { Name = "v1" };
        context.Entities.Add(entity);
        await context.SaveChangesAsync();

        entity.Name = "v2";
        await context.SaveChangesAsync();
        entity.Name = "v3";
        await context.SaveChangesAsync();

        Assert.AreEqual("v3", entity.Name);
    }
}
