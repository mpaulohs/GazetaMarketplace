using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Ads;

/// <summary>A contagem real de anúncios por categoria (substitui a implementação provisória que devolvia zero).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdsCategoryUsageTests
#pragma warning restore CA1515
{
    private const int Ciclismo = 58;
    private const int Carros = 33;

    private static Task<IReadOnlyDictionary<int, int>> CountAsync(AdDb db, params int[] ids) =>
        db.WithScopeAsync(provider => provider.GetRequiredService<ICategoryUsage>().CountAdsAsync(ids, CancellationToken.None));

    [TestMethod]
    public async Task ContaAnunciosEmQualquerSituacao()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        foreach (byte status in AdStatus.All)
        {
            await db.AddAdAsync(author, status, categoryId: Ciclismo);
        }

        IReadOnlyDictionary<int, int> counts = await CountAsync(db, Ciclismo);

        Assert.AreEqual(5, counts[Ciclismo], "rascunho, em revisão, publicado, rejeitado e arquivado contam");
    }

    [TestMethod]
    public async Task TodoIdPedidoApareceNoResultado_ComZeroSeNaoHaAnuncios()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        await db.AddAdAsync(author, categoryId: Ciclismo);
        await db.AddAdAsync(author, categoryId: Ciclismo);
        await db.AddAdAsync(author, categoryId: Carros);

        IReadOnlyDictionary<int, int> counts = await CountAsync(db, Ciclismo, Carros, 1, Ciclismo);

        Assert.HasCount(3, counts, "ids repetidos viram uma entrada");
        Assert.AreEqual(2, counts[Ciclismo]);
        Assert.AreEqual(1, counts[Carros]);
        Assert.AreEqual(0, counts[1]);
    }

    [TestMethod]
    public async Task RascunhoSemCategoria_NaoContaEmCategoriaNenhuma()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        await db.AddAdAsync(author);

        IReadOnlyDictionary<int, int> counts = await CountAsync(db, Ciclismo, Carros);

        Assert.AreEqual(0, counts[Ciclismo]);
        Assert.AreEqual(0, counts[Carros]);
    }

    [TestMethod]
    public async Task SemIds_DevolveVazio()
    {
        using AdDb db = new();

        Assert.IsEmpty(await CountAsync(db));
    }

    [TestMethod]
    public async Task OutraCategoria_NaoEntraNaContagem()
    {
        using AdDb db = new();
        int author = await db.AddUserAsync();
        await db.AddAdAsync(author, categoryId: Carros);

        Assert.AreEqual(0, (await CountAsync(db, Ciclismo))[Ciclismo]);
    }
}
