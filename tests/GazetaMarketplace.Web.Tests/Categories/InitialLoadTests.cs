using System;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Categories;

/// <summary>A carga inicial e o esquema de <c>Categories</c> (ARCHITECTURE §6.2), em SQLite; o SQL Server real é provado nos testes de integração.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class InitialLoadTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task Carga_Cria124CategoriasPostaveis_ComIdsReais()
    {
        using CategoryDb db = new();

        Category[] all = await db.WithContextAsync(c => c.Categories.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());

        Assert.HasCount(147, all);
        Assert.AreEqual(124, all.Count(c => c.IsPostable));
        Assert.AreEqual(22, all.Count(c => c.ParentId is null));
        Assert.AreEqual(1, all.Single(c => c.Id == 1).Id);
        Assert.AreEqual("Imóveis", all.Single(c => c.Id == 1).Name);
        Assert.AreEqual("Carros, vans e utilitários", all.Single(c => c.Id == 33).Name);
        Assert.AreEqual("Papelaria", all.Single(c => c.Id == 155).Name);
        Assert.IsEmpty(all.Where(c => c.Id is 24 or 25 or 32 or 79 or 80 or 82 or 83 or 91), "buracos do arquivo e animais vivos continuam ausentes");
        Assert.IsTrue(all.All(c => c.IsSystem));
        Assert.IsTrue(all.All(c => c.FieldGroup is null));
        Assert.IsTrue(all.All(c => c.CreatedAt == new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc)));
    }

    [TestMethod]
    public async Task Autopecas_EstaNoTerceiroNivel()
    {
        using CategoryDb db = new();

        CategoryTreeSnapshot tree = await db.Tree.GetAsync(default);

        CategoryNode autoparts = tree.Find(3);
        Assert.AreEqual("Autopeças", autoparts.Name);
        Assert.AreEqual(2, autoparts.Depth);
        Assert.IsFalse(autoparts.IsPostable);
        CategoryNode[] children = [.. tree.ChildrenOf(3)];
        Assert.HasCount(5, children);
        Assert.IsTrue(children.All(c => c.Depth == 3 && c.IsPostable));
        Assert.AreEqual(3, tree.All.Max(n => n.Depth), "a árvore tem 3 níveis, nunca mais");
    }

    [TestMethod]
    public async Task ProximaCategoriaNova_ContinuaDepoisDoMaiorId()
    {
        using CategoryDb db = new();

        int id = await db.WithContextAsync(async context =>
        {
            Category created = new() { ParentId = 1, Name = "Galpões", Slug = "galpoes", DisplayOrder = 7, IsPostable = true };
            context.Categories.Add(created);
            await context.SaveChangesAsync();
            return created.Id;
        });

        Assert.AreEqual(156, id);
    }

    [TestMethod]
    public async Task EsquemaRecusa_NomeRepetidoEntreIrmas_MasAceitaEmPaisDiferentes()
    {
        using CategoryDb db = new();

        // "Serviços" já existe sob o pai 7 (id 66), mas também é o nome do pai 7: pais diferentes, tudo bem
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => db.WithContextAsync(async context =>
        {
            context.Categories.Add(new Category { ParentId = 1, Name = "Casas", Slug = "casas-nova", DisplayOrder = 9, IsPostable = true });
            await context.SaveChangesAsync();
        }));
        await db.WithContextAsync(async context =>
        {
            context.Categories.Add(new Category { ParentId = 2, Name = "Casas", Slug = "casas-no-auto", DisplayOrder = 9, IsPostable = true });
            await context.SaveChangesAsync();
        });
    }

    [TestMethod]
    public async Task EsquemaRecusa_NomeRepetidoEntreCategoriasDePrimeiroNivel()
    {
        using CategoryDb db = new();

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => db.WithContextAsync(async context =>
        {
            context.Categories.Add(new Category { ParentId = null, Name = "Imóveis", Slug = "imoveis-2", DisplayOrder = 30 });
            await context.SaveChangesAsync();
        }));
    }

    [TestMethod]
    public async Task EsquemaRecusa_SlugRepetido()
    {
        using CategoryDb db = new();

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => db.WithContextAsync(async context =>
        {
            context.Categories.Add(new Category { ParentId = 2, Name = "Trailers", Slug = "cars", DisplayOrder = 9, IsPostable = true });
            await context.SaveChangesAsync();
        }));
    }

    [TestMethod]
    public async Task EsquemaRecusa_ApagarPaiQueTemFilhas_ECategoriaPaiDeSiMesma()
    {
        using CategoryDb db = new();

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => db.WithContextAsync(async context =>
        {
            context.Categories.Remove(await context.Categories.SingleAsync(c => c.Id == 3));
            await context.SaveChangesAsync();
        }));
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => db.WithContextAsync(async context =>
        {
            Category self = await context.Categories.SingleAsync(c => c.Id == 66);
            self.ParentId = self.Id;
            await context.SaveChangesAsync();
        }));
    }

    [TestMethod]
    public async Task CategoriaNova_GravaAuditoria_ERowVersionNaoEhNulo()
    {
        using CategoryDb db = new();

        Category created = await db.WithContextAsync(async context =>
        {
            Category category = new() { ParentId = 1, Name = "Galpões", Slug = "galpoes", DisplayOrder = 7, IsPostable = true };
            context.Categories.Add(category);
            await context.SaveChangesAsync();
            return category;
        });

        Assert.AreEqual(db.Clock.Now.UtcDateTime, created.CreatedAt);
    }
}
