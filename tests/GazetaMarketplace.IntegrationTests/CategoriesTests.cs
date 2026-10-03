using System;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// A tabela <c>Categories</c> no SQL Server real: a carga inicial, a identidade que continua depois do maior id, os índices únicos
/// (inclusive o filtrado para o primeiro nível, onde o NULL não repete), a restrição de apagar o pai e o cache da árvore que o site usa.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CategoriesTests
#pragma warning restore CA1515
{
    private static async Task<AppDbContext> OpenAsync(string connectionString) => await Task.FromResult(SqlServerFixture.NewContext(connectionString));

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Carga_Tem147Linhas_124Postaveis_ComIdsReais_ESemOsAnimaisVivos()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        await using AppDbContext context = await OpenAsync(connection);

        Category[] all = await context.Categories.AsNoTracking().ToArrayAsync();

        Assert.HasCount(147, all);
        Assert.AreEqual(124, all.Count(c => c.IsPostable));
        Assert.AreEqual(22, all.Count(c => c.ParentId is null));
        Assert.AreEqual("Autopeças", all.Single(c => c.Id == 3).Name);
        Assert.AreEqual(2, all.Single(c => c.Id == 3).ParentId);
        Assert.AreEqual("cars", all.Single(c => c.Id == 33).Slug);
        Assert.AreEqual("servicos", all.Single(c => c.Id == 66).Slug);
        Assert.AreEqual("servicos-grupo", all.Single(c => c.Id == 7).Slug);
        Assert.IsFalse(all.Any(c => c.Id is 24 or 25 or 32 or 79 or 80 or 82 or 83 or 91));
        Assert.IsTrue(all.All(c => c.IsSystem && c.FieldGroup is null));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ProximaCategoriaNova_RecebeOId156_DepoisDaCargaComIdentityInsert()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        await using AppDbContext context = await OpenAsync(connection);

        Category created = new() { ParentId = 1, Name = "Galpões", Slug = "galpoes", DisplayOrder = 7, IsPostable = true };
        context.Categories.Add(created);
        await context.SaveChangesAsync();

        Assert.AreEqual(156, created.Id, "a identidade continua depois do maior id da carga (155)");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ProximaCategoriaNova_TambemRecebe156_NoBancoCriadoPeloScript()
    {
        string connection = await SqlServerFixture.CreateEmptyDatabaseAsync();
        await SqlServerFixture.ApplyScriptAsync(connection);
        await using AppDbContext context = await OpenAsync(connection);

        Category created = new() { ParentId = 1, Name = "Galpões", Slug = "galpoes", DisplayOrder = 7, IsPostable = true };
        context.Categories.Add(created);
        await context.SaveChangesAsync();

        Assert.AreEqual(156, created.Id);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Indices_RecusamNomeRepetidoEntreIrmas_NomeRepetidoNoPrimeiroNivel_ESlugRepetido()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();

        // Irmãs: o banco compara sem diferenciar maiúscula ("casas" = "Casas")
        await AssertRejectedAsync(connection, new Category { ParentId = 1, Name = "casas", Slug = "x1", DisplayOrder = 9 });
        // Primeiro nível: o NULL do pai não deixa passar dois nomes iguais
        await AssertRejectedAsync(connection, new Category { ParentId = null, Name = "IMÓVEIS", Slug = "x2", DisplayOrder = 99 });
        // Slug único no site inteiro, mesmo em pais diferentes
        await AssertRejectedAsync(connection, new Category { ParentId = 2, Name = "Trailers", Slug = "cars", DisplayOrder = 9 });

        // Mesmo nome em pais diferentes passa; sem diferenciar acento o banco deixa passar, por isso a regra do serviço (CategoryRules) também compara sem acento
        await using AppDbContext context = await OpenAsync(connection);
        context.Categories.Add(new Category { ParentId = 2, Name = "Casas", Slug = "casas-auto", DisplayOrder = 9 });
        context.Categories.Add(new Category { ParentId = 1, Name = "Cásas", Slug = "casas-acentuada", DisplayOrder = 10 });
        await context.SaveChangesAsync();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Restricoes_ApagarPaiComFilhas_EPaiDeSiMesma_SaoRecusadas()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();

        await using (AppDbContext context = await OpenAsync(connection))
        {
            context.Categories.Remove(await context.Categories.SingleAsync(c => c.Id == 3));
            DbUpdateException error = await Assert.ThrowsExactlyAsync<DbUpdateException>(() => context.SaveChangesAsync());
            Assert.IsInstanceOfType<SqlException>(error.InnerException);
            StringAssert.Contains(error.InnerException!.Message, "FK_Categories_Categories_ParentId");
        }

        await using (AppDbContext context = await OpenAsync(connection))
        {
            Category own = await context.Categories.SingleAsync(c => c.Id == 66);
            own.ParentId = own.Id;
            DbUpdateException error = await Assert.ThrowsExactlyAsync<DbUpdateException>(() => context.SaveChangesAsync());
            StringAssert.Contains(error.InnerException!.Message, "CK_Categories_NotOwnParent");
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ArvoreDoSite_CarregaDoSqlServer_VeAEdicaoNaHora_ERecusaOQuartoNivel()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());
        ICategoryTree tree = factory.Services.GetRequiredService<ICategoryTree>();

        CategoryTreeSnapshot first = await tree.GetAsync(default);
        Assert.AreEqual(147, first.Count);
        Assert.HasCount(11, first.DescendantsOf(2));
        Assert.AreEqual(CategoryViolation.TooDeep, CategoryRules.Check(first, parentId: 38, "Faróis"));

        // A edição pelo contexto do site esvazia o cache; a leitura seguinte já vê o nome novo
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Category houses = await context.Categories.SingleAsync(c => c.Id == 27);
            houses.Name = "Casas e sobrados";
            await context.SaveChangesAsync();
        }

        CategoryTreeSnapshot second = await tree.GetAsync(default);
        Assert.AreNotSame(first, second);
        Assert.AreEqual("Casas e sobrados", second.Find(27).Name);
        Assert.AreEqual("Casas", first.Find(27).Name, "o retrato antigo continua intacto para quem ainda o usa");
    }

    private static async Task AssertRejectedAsync(string connection, Category category)
    {
        await using AppDbContext context = await OpenAsync(connection);
        context.Categories.Add(category);
        DbUpdateException error = await Assert.ThrowsExactlyAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.IsInstanceOfType<SqlException>(error.InnerException);
        Assert.AreEqual(2601, ((SqlException)error.InnerException!).Number, "violação de índice único");
    }
}
