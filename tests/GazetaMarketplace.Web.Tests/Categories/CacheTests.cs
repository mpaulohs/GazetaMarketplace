using System;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Categories;

/// <summary>Cache de 10 minutos da árvore (relógio do site) e a invalidação automática ao gravar uma categoria.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CacheTests
#pragma warning restore CA1515
{
    private static Task RenameAsync(CategoryDb db, int id, string name) => db.WithContextAsync(async context =>
    {
        Category category = await context.Categories.SingleAsync(c => c.Id == id);
        category.Name = name;
        await context.SaveChangesAsync();
    });

    [TestMethod]
    public async Task ArvoreInteira_CarregaComUmaUnicaConsulta_ASegundaLeituraNaoVaiAoBanco()
    {
        using CategoryDb db = new();

        CategoryTreeSnapshot first = await db.Tree.GetAsync(default);
        _ = first.DescendantsOf(2);
        _ = first.PathTo(38);
        CategoryTreeSnapshot second = await db.Tree.GetAsync(default);

        Assert.AreEqual(1, db.Counter.Count, "uma consulta para 147 categorias, todos os níveis, e nenhuma na segunda leitura");
        Assert.AreSame(first, second);
    }

    [TestMethod]
    public async Task EditarCategoria_InvalidaOCache()
    {
        using CategoryDb db = new();
        Assert.AreEqual("Casas", (await db.Tree.GetAsync(default)).Find(27).Name);

        await RenameAsync(db, 27, "Casas e sobrados");

        Assert.AreEqual("Casas e sobrados", (await db.Tree.GetAsync(default)).Find(27).Name, "a leitura seguinte já vê o novo nome");
    }

    [TestMethod]
    public async Task CriarEExcluirCategoria_TambemInvalidam()
    {
        using CategoryDb db = new();
        Assert.IsNull((await db.Tree.GetAsync(default)).FindBySlug("galpoes"));

        await db.WithContextAsync(async context =>
        {
            context.Categories.Add(new Category { ParentId = 1, Name = "Galpões", Slug = "galpoes", DisplayOrder = 7, IsPostable = true });
            await context.SaveChangesAsync();
        });
        Assert.IsNotNull((await db.Tree.GetAsync(default)).FindBySlug("galpoes"), "criada");

        await db.WithContextAsync(async context =>
        {
            context.Categories.Remove(await context.Categories.SingleAsync(c => c.Slug == "galpoes"));
            await context.SaveChangesAsync();
        });
        Assert.IsNull((await db.Tree.GetAsync(default)).FindBySlug("galpoes"), "excluída");
    }

    [TestMethod]
    public async Task AlterarOutraEntidade_NaoInvalidaOCache()
    {
        using CategoryDb db = new();
        CategoryTreeSnapshot before = await db.Tree.GetAsync(default);

        await db.WithContextAsync(async context =>
        {
            context.PasswordRecoveryAttempts.Add(new PasswordRecoveryAttempt { Email = "a@b.com", Ip = "1.1.1.1", RequestedAt = db.Clock.Now.UtcDateTime });
            await context.SaveChangesAsync();
        });

        Assert.AreSame(before, await db.Tree.GetAsync(default));
    }

    [TestMethod]
    public async Task GravacaoQueFalha_NaoInvalidaOCache()
    {
        using CategoryDb db = new();
        CategoryTreeSnapshot before = await db.Tree.GetAsync(default);

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => db.WithContextAsync(async context =>
        {
            context.Categories.Add(new Category { ParentId = 1, Name = "Casas", Slug = "outra", DisplayOrder = 9, IsPostable = true });
            await context.SaveChangesAsync();
        }));

        Assert.AreSame(before, await db.Tree.GetAsync(default), "nada mudou no banco, então o cache vale");
    }

    [TestMethod]
    public async Task Cache_Expira_Em10Minutos()
    {
        using CategoryDb db = new();
        CategoryTreeSnapshot first = await db.Tree.GetAsync(default);

        db.Clock.Now = db.Clock.Now.AddMinutes(9).AddSeconds(59);
        Assert.AreSame(first, await db.Tree.GetAsync(default), "aos 9min59 ainda vale");

        db.Clock.Now = db.Clock.Now.AddSeconds(2);
        CategoryTreeSnapshot reloaded = await db.Tree.GetAsync(default);

        Assert.AreNotSame(first, reloaded, "aos 10min01 recarrega");
        Assert.AreEqual(2, db.Counter.Count);
    }

    [TestMethod]
    public async Task LeiturasAoMesmoTempo_NumCacheVazio_CarregamUmaVezSo()
    {
        using CategoryDb db = new();

        CategoryTreeSnapshot[] results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => db.Tree.GetAsync(default))));

        Assert.AreEqual(1, db.Counter.Count);
        Assert.IsTrue(results.All(r => ReferenceEquals(r, results[0])));
    }

    [TestMethod]
    public async Task InvalidacaoNoMeioDaCarga_NaoDeixaAArvoreVelhaNoCache()
    {
        using CategoryDb db = new();
        bool invalidated = false;
        db.Counter.AfterRead = () =>
        {
            if (!invalidated)
            {
                invalidated = true;
                db.Tree.Invalidate(); // alguém gravou uma categoria enquanto esta leitura estava a caminho
            }
        };

        await db.Tree.GetAsync(default);
        await db.Tree.GetAsync(default);

        Assert.AreEqual(2, db.Counter.Count, "a primeira leitura foi devolvida mas não guardada; a segunda foi ao banco");
    }
}
