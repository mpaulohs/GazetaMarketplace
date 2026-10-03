using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>Gerenciar categorias (US-013) contra anúncios reais no SQL Server: a contagem de S08 e a regra de subcategoria dentro de uma folha com anúncios.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdsCategoryUsageTests
#pragma warning restore CA1515
{
    private const int Ciclismo = 58;
    private const int Motos = 36;

    private static async Task<T> InScopeAsync<T>(IntegrationWebFactory factory, Func<IServiceProvider, Task<T>> work)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        return await work(scope.ServiceProvider);
    }

    private static async Task AddAdsAsync(string connection, int author, int category, params byte[] statuses)
    {
        foreach (byte status in statuses)
        {
            Ad ad = Ad.CreateDraft("Anúncio " + status, author);
            ad.SetCategory(category);
            if (status != AdStatus.Draft)
            {
                ad.ApplyTransition(AdStatus.InReview, author, DateTime.UtcNow, null);
                if (status == AdStatus.Published)
                {
                    ad.ApplyTransition(AdStatus.Published, author, DateTime.UtcNow, null);
                }
                else if (status == AdStatus.Rejected)
                {
                    ad.ApplyTransition(AdStatus.Rejected, author, DateTime.UtcNow, "Fotos escuras");
                }
                else if (status == AdStatus.Archived)
                {
                    ad.ApplyTransition(AdStatus.Archived, author, DateTime.UtcNow, null);
                }
            }

            await using AppDbContext context = SqlServerFixture.NewContext(connection);
            context.Ads.Add(ad);
            await context.SaveChangesAsync();
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US013S08_ExcluirCategoriaComAnuncios_Recusa_ComAContagemReal()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        using IntegrationWebFactory factory = new(connection);
        await AddAdsAsync(connection, author, Ciclismo, [.. AdStatus.All]);

        CategoryResult refused = await InScopeAsync(factory, sp => sp.GetRequiredService<ICategoryManagement>().DeleteAsync(Ciclismo, CancellationToken.None));
        IReadOnlyDictionary<int, int> counts = await InScopeAsync(factory, sp => sp.GetRequiredService<ICategoryUsage>().CountAdsAsync([Ciclismo, 59], CancellationToken.None));

        Assert.IsFalse(refused.Succeeded);
        Assert.AreEqual(CategoryMessages.HasAds(5), refused.Message, "rascunho, em revisão, publicado, rejeitado e arquivado contam");
        Assert.AreEqual(5, counts[Ciclismo]);
        Assert.AreEqual(0, counts[59]);
        Assert.IsNotNull(await InScopeAsync(factory, sp => sp.GetRequiredService<AppDbContext>().Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == Ciclismo)));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task SubcategoriaDentroDeUmaFolhaComAnuncios_Recusa_ESemAnuncios_Cria()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        using IntegrationWebFactory factory = new(connection);
        await AddAdsAsync(connection, author, Motos, AdStatus.Draft);

        CategoryResult refused = await InScopeAsync(factory, sp => sp.GetRequiredService<ICategoryManagement>().CreateAsync(Motos, "Scooters", CancellationToken.None));
        CategoryResult created = await InScopeAsync(factory, sp => sp.GetRequiredService<ICategoryManagement>().CreateAsync(Ciclismo, "BMX", CancellationToken.None));

        Assert.IsFalse(refused.Succeeded);
        Assert.AreEqual(CategoryMessages.ParentHasAds, refused.Message);
        Assert.IsTrue(created.Succeeded, "folha sem anúncios aceita a subcategoria");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CategoriaSemAnuncios_ExcluiNormalmente_ERascunhoSemCategoriaNaoImpedeNada()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        using IntegrationWebFactory factory = new(connection);
        await AdData.AddDraftAsync(connection, author, title: "Só o título");
        CategoryResult created = await InScopeAsync(factory, sp => sp.GetRequiredService<ICategoryManagement>().CreateAsync(null, "Colecionáveis", CancellationToken.None));

        CategoryResult deleted = await InScopeAsync(factory, sp => sp.GetRequiredService<ICategoryManagement>().DeleteAsync(created.CategoryId!.Value, CancellationToken.None));

        Assert.IsTrue(deleted.Succeeded);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ExcluirCategoriaEnquantoOutraPessoaCriaAnuncioNela_NuncaDeixaAnuncioSemCategoria()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        using IntegrationWebFactory factory = new(connection);

        for (int round = 1; round <= 6; round++)
        {
            CategoryResult created = await InScopeAsync(factory, sp => sp.GetRequiredService<ICategoryManagement>().CreateAsync(Ciclismo, "Subcategoria " + round, CancellationToken.None));
            int category = created.CategoryId!.Value;
            using Barrier start = new(2);

            Task<CategoryResult> delete = Task.Run(() => InScopeAsync(factory, sp =>
            {
                start.SignalAndWait();
                return sp.GetRequiredService<ICategoryManagement>().DeleteAsync(category, CancellationToken.None);
            }));
            Task<bool> post = Task.Run(async () =>
            {
                start.SignalAndWait();
                try
                {
                    await AddAdsAsync(connection, author, category, AdStatus.Draft);
                    return true;
                }
                catch (DbUpdateException)
                {
                    return false; // a categoria saiu antes: a chave estrangeira recusa
                }
            });

            CategoryResult deleteResult = await delete;
            bool posted = await post;

            int ads = (await AdData.QueryAsync(connection, $"SELECT COUNT(*) FROM Ads WHERE CategoryId = {category}")).Select(int.Parse).Single();
            bool exists = (await AdData.QueryAsync(connection, $"SELECT COUNT(*) FROM Categories WHERE Id = {category}")).Select(int.Parse).Single() == 1;
            Assert.IsTrue(!(posted && deleteResult.Succeeded), $"rodada {round}: ou o anúncio entrou ou a categoria saiu, nunca os dois");
            Assert.AreEqual(posted ? 1 : 0, ads, $"rodada {round}");
            Assert.AreEqual(!deleteResult.Succeeded, exists, $"rodada {round}: a categoria só some se a exclusão deu certo");
        }
    }
}
