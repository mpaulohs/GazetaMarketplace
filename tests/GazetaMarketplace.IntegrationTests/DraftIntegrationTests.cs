using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Core.VehicleCatalog;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// O serviço do rascunho no SQL Server real: o que o formulário grava alimenta as colunas calculadas do catálogo e da área, duas telas salvando o
/// mesmo anúncio ao mesmo tempo não se atropelam (RowVersion) e uma categoria apagada no meio do salvamento vira recusa, não erro de banco.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class DraftIntegrationTests
#pragma warning restore CA1515
{
    private const int Cars = 33;
    private const int Land = 30;

    private sealed class FixedCep : ICepLookup
    {
        public Task<CepLookupResult> LookupAsync(string cep, CancellationToken cancellationToken) =>
            Task.FromResult(new CepLookupResult("Campinas", "SP", 3509502));
    }

    private static IntegrationWebFactory Site(string connection) => new(connection, services =>
    {
        services.RemoveAll<ICepLookup>();
        services.AddSingleton<ICepLookup>(new FixedCep());
        services.RemoveAll<ICurrentUser>();
        services.AddScoped<ICurrentUser, FakeCurrentUser>();
    });

    private static async Task<T> AsUserAsync<T>(IntegrationWebFactory factory, int userId, Func<IServiceProvider, Task<T>> work, Barrier start = null)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        FakeCurrentUser user = (FakeCurrentUser)scope.ServiceProvider.GetRequiredService<ICurrentUser>();
        user.UserId = userId;
        user.IsAdministrator = false;
        start?.SignalAndWait();
        return await work(scope.ServiceProvider);
    }

    private static Task<AdDraftResult> CreateAsync(IntegrationWebFactory factory, int userId, AdDraftInput input) =>
        AsUserAsync(factory, userId, sp => sp.GetRequiredService<IAdDraftService>().CreateAsync(input, CancellationToken.None));

    private static AdDraftInput Input(string title, int? category = null, string price = null, params (string Key, string Value)[] fields) =>
        new(title, null, category, price, null, null, null, null, false, fields.ToDictionary(f => f.Key, f => new[] { f.Value }));

    private static async Task SeedCatalogAsync(string connection)
    {
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        context.VehicleBrands.Add(new VehicleBrand { Id = 1, Kind = "car", Name = "Honda", Source = "teste" });
        context.VehicleModels.Add(new VehicleModel { Id = 11, Kind = "car", BrandId = 1, Name = "Civic", Source = "teste" });
        context.VehicleModelYears.Add(new VehicleModelYear { ModelId = 11, Year = 2018, Kind = "car", Source = "teste" });
        context.VehicleVersions.Add(new VehicleVersion { Id = 100, Kind = "car", ModelId = 11, Year = 2018, Name = "LX", Source = "teste" });
        await context.SaveChangesAsync();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task OQueOFormularioGrava_AlimentaAsColunasCalculadas_ComOMesmoValorDoCatalogoEDaArea()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        await SeedCatalogAsync(connection);
        int author = await AdData.AddUserAsync(connection);
        using IntegrationWebFactory factory = Site(connection);

        AdDraftResult car = await CreateAsync(factory, author, Input("Honda Civic 2018", Cars, "62.000,00",
            ("brandId", "1"), ("modelId", "11"), ("modelYear", "2018"), ("versionId", "100"), ("km", "45.000")));
        AdDraftResult land = await CreateAsync(factory, author, Input("Terreno", Land, null, ("areaM2", "1.450,75")));

        Ad carRow = await AdData.LoadAsync(connection, car.Id);
        Assert.AreEqual(1, carRow.VehicleBrandId);
        Assert.AreEqual(11, carRow.VehicleModelId);
        Assert.AreEqual(2018, carRow.ModelYear);
        Assert.AreEqual(45000, carRow.Km, "'45.000' digitado vira o número 45000 no JSON, e o banco o lê como número");
        Assert.AreEqual(6_200_000L, carRow.PriceCents);
        Assert.AreEqual(1450.75m, (await AdData.LoadAsync(connection, land.Id)).AreaM2);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task TextoEmCampoNumerico_NuncaChegaAoBanco_EOAnuncioNaoEhCriado()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        using IntegrationWebFactory factory = Site(connection);

        // O SQL Server converteria "12" entre aspas em número e o C# o leria como ausente; por isso o formulário recusa em vez de gravar texto
        await Assert.ThrowsExactlyAsync<ValidationException>(() => CreateAsync(factory, author, Input("Honda", Cars, null, ("km", " 12 muito"))));
        await Assert.ThrowsExactlyAsync<ValidationException>(() => CreateAsync(factory, author, Input("Honda", Cars, null, ("km", "1e3"))));

        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        Assert.AreEqual(0, await context.Ads.CountAsync());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DuasTelasSalvandoOMesmoAnuncio_SoUmaGrava_AOutraRecebeConflito_ESemRastroNaAuditoria()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        using IntegrationWebFactory factory = Site(connection);

        for (int round = 1; round <= 6; round++)
        {
            AdDraftResult created = await CreateAsync(factory, author, Input("Corrida " + round, Cars));
            using Barrier start = new(2);
            Task<Exception> first = Task.Run(() => SaveOrErrorAsync(factory, author, created, start, "Tela A " + round));
            Task<Exception> second = Task.Run(() => SaveOrErrorAsync(factory, author, created, start, "Tela B " + round));
            Exception[] errors = await Task.WhenAll(first, second);

            Assert.AreEqual(1, errors.Count(e => e is null), $"rodada {round}: exatamente uma tela grava");
            Assert.IsInstanceOfType<ConflictException>(errors.Single(e => e is not null), $"rodada {round}: a outra recebe Conflito, não erro genérico");
            Ad stored = await AdData.LoadAsync(connection, created.Id);
            Assert.IsTrue(stored.Title.StartsWith("Tela ", StringComparison.Ordinal));
        }

        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        List<AuditEntry> audit = await context.AuditEntries.AsNoTracking().Where(e => e.TargetType == "Ad").ToListAsync();
        Assert.AreEqual(6, audit.Count(e => e.Action == "ad.create"));
        Assert.AreEqual(6, audit.Count(e => e.Action == "ad.update"), "uma por rodada: quem gravou; a tela recusada não deixa rastro");
        Assert.IsTrue(audit.Where(e => e.Action == "ad.update").All(e => e.NewValue is null && e.PreviousValue is null), "só o título mudou, e título não é auditado");
    }

    private static async Task<Exception> SaveOrErrorAsync(IntegrationWebFactory factory, int userId, AdDraftResult opened, Barrier start, string title)
    {
        try
        {
            await AsUserAsync(factory, userId,
                sp => sp.GetRequiredService<IAdDraftService>().UpdateAsync(opened.Id, opened.RowVersion, Input(title, Cars), CancellationToken.None), start);
            return null;
        }
        catch (Exception error) when (error is ConflictException or ValidationException)
        {
            return error;
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task FormularioAbertoAntesDeOutroSalvar_ComVersaoVelha_EhRecusado_EAVersaoNovaPassa()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        using IntegrationWebFactory factory = Site(connection);
        AdDraftResult opened = await CreateAsync(factory, author, Input("Original", Cars));

        AdDraftResult saved = await AsUserAsync(factory, author, sp => sp.GetRequiredService<IAdDraftService>().UpdateAsync(opened.Id, opened.RowVersion, Input("Primeira edição", Cars), CancellationToken.None));
        await Assert.ThrowsExactlyAsync<ConflictException>(() => AsUserAsync(factory, author,
            sp => sp.GetRequiredService<IAdDraftService>().UpdateAsync(opened.Id, opened.RowVersion, Input("Edição com versão velha", Cars), CancellationToken.None)));
        AdDraftResult next = await AsUserAsync(factory, author, sp => sp.GetRequiredService<IAdDraftService>().UpdateAsync(opened.Id, saved.RowVersion, Input("Segunda edição", Cars), CancellationToken.None));

        Assert.AreEqual("Segunda edição", (await AdData.LoadAsync(connection, opened.Id)).Title);
        CollectionAssert.AreNotEqual(opened.RowVersion, saved.RowVersion);
        CollectionAssert.AreNotEqual(saved.RowVersion, next.RowVersion);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CategoriaApagadaNoMeioDoSalvamento_ViraRecusaDeCategoria_NaoErroDeBanco()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        using IntegrationWebFactory factory = Site(connection);

        // Cria uma categoria nova (sem anúncios) e deixa a árvore em cache com ela
        CategoryResult made = await AsUserAsync(factory, author, sp => sp.GetRequiredService<ICategoryManagement>().CreateAsync(1, "Galpões", CancellationToken.None));
        Assert.IsTrue(made.Succeeded);
        await AsUserAsync(factory, author, sp => sp.GetRequiredService<ICategoryTree>().GetAsync(CancellationToken.None));

        // Outra pessoa a apaga direto no banco: o cache de 10 minutos ainda a mostra
        int error = await AdData.TryExecuteAsync(connection, "DELETE FROM Categories WHERE Id = @id", ("@id", made.CategoryId));
        Assert.AreEqual(0, error);

        ValidationException refused = await Assert.ThrowsExactlyAsync<ValidationException>(() => CreateAsync(factory, author, Input("Galpão", made.CategoryId)));

        StringAssert.Contains(refused.Errors["CategoryId"][0], "não existe mais");
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        Assert.AreEqual(0, await context.Ads.CountAsync(), "nada ficou gravado");
        Assert.AreEqual(0, await context.AuditEntries.CountAsync(e => e.Action == "ad.create"));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task AuditoriaEAnuncio_SaoGravadosJuntos_NaoSobraAnuncioSemAuditoria()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        using IntegrationWebFactory factory = Site(connection);

        for (int i = 0; i < 5; i++)
        {
            await CreateAsync(factory, author, Input("Rascunho " + i, Cars, "10,00"));
        }

        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        Assert.AreEqual(5, await context.Ads.CountAsync());
        Assert.AreEqual(5, await context.AuditEntries.CountAsync(e => e.Action == "ad.create"));
        Assert.IsFalse(await context.AuditEntries.AnyAsync(e => e.TargetType == "Ad" && e.TargetId == "0"));
    }
}
