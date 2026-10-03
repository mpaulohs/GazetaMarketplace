using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.VehicleCatalog;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Catalog;

/// <summary>O cache de 10 minutos (relógio do site) e a regra de que a origem dos dados (<c>Source</c>) não muda nenhuma consulta.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CacheAndSourceTests
#pragma warning restore CA1515
{
    private static WebFactory NewFactory(string source = "teste") => new(withDatabase: true, seed: context => SmallCatalog.Seed(context, source));

    private static IVehicleCatalog Catalog(WebFactory factory) => factory.Services.GetRequiredService<IVehicleCatalog>();

    private static async Task BehindTheCacheAsync(WebFactory factory, Func<AppDbContext, Task> change)
    {
        // Direto no banco, sem passar pelo serviço: o cache não fica sabendo
        using IServiceScope scope = factory.Services.CreateScope();
        await change(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private static Task RenameBrandAsync(WebFactory factory, string name) => BehindTheCacheAsync(factory, async db =>
    {
        await db.VehicleBrands.Where(b => b.Id == 1 && b.Kind == "car").ExecuteUpdateAsync(s => s.SetProperty(b => b.Name, name));
    });

    private static async Task<string> HondaAsync(WebFactory factory) =>
        (await Catalog(factory).BrandsAsync("car", CancellationToken.None)).Single(b => b.Id == 1).Name;

    [TestMethod]
    public async Task Lista_FicaEmCacheAte10Minutos_ENaoUmSegundoAMais()
    {
        using WebFactory factory = NewFactory();
        Assert.AreEqual("Honda", await HondaAsync(factory));

        await RenameBrandAsync(factory, "Honda renomeada");

        factory.Clock.Now += TimeSpan.FromMinutes(9) + TimeSpan.FromSeconds(59);
        Assert.AreEqual("Honda", await HondaAsync(factory), "aos 9min59 ainda vale o cache");
        factory.Clock.Now += TimeSpan.FromSeconds(1);
        Assert.AreEqual("Honda renomeada", await HondaAsync(factory), "aos 10 minutos relê o banco");
    }

    [TestMethod]
    public async Task Invalidate_ForcaReleituraNaHora()
    {
        using WebFactory factory = NewFactory();
        _ = await HondaAsync(factory);
        await RenameBrandAsync(factory, "Honda renomeada");

        Catalog(factory).Invalidate();

        Assert.AreEqual("Honda renomeada", await HondaAsync(factory));
    }

    [TestMethod]
    public async Task CadaConsulta_TemOSeuCache_ETipoEIdFazemParteDaChave()
    {
        using WebFactory factory = NewFactory();
        IVehicleCatalog catalog = Catalog(factory);

        IReadOnlyList<CatalogItem> carModels = await catalog.ModelsAsync("car", 1, CancellationToken.None);
        IReadOnlyList<CatalogItem> motoModels = await catalog.ModelsAsync("moto", 1, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "City", "Civic", "Fit" }, carModels.Select(m => m.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "CG 160" }, motoModels.Select(m => m.Name).ToArray(), "o cache de carros não responde pelas motos");
    }

    [TestMethod]
    public async Task ConsultaQueNaoAchaNada_NaoFicaEmCache_ENaoEnvenenaARespostaSeguinte()
    {
        using WebFactory factory = NewFactory();
        IVehicleCatalog catalog = Catalog(factory);
        _ = await Assert.ThrowsExactlyAsync<GazetaMarketplace.Core.Exceptions.NotFoundException>(() => catalog.ModelsAsync("car", 7, CancellationToken.None));

        await BehindTheCacheAsync(factory, async db =>
        {
            db.VehicleBrands.Add(new VehicleBrand { Id = 7, Kind = "car", Name = "Nova", Source = "teste" });
            await db.SaveChangesAsync();
        });

        Assert.AreEqual(0, (await catalog.ModelsAsync("car", 7, CancellationToken.None)).Count, "a marca nova já existe, sem esperar os 10 minutos");
    }

    [TestMethod]
    public async Task TipoInvalido_NaoChegaAoBanco()
    {
        using WebFactory factory = NewFactory();

        _ = await Assert.ThrowsExactlyAsync<ArgumentException>(() => Catalog(factory).BrandsAsync("bike", CancellationToken.None));
    }

    [TestMethod]
    public async Task TrocarFonte_NaoMudaAConsulta()
    {
        using WebFactory factory = NewFactory("gazetaonline-2026-09");
        IVehicleCatalog catalog = Catalog(factory);
        string Snapshot(params string[] parts) => string.Join(";", parts);
        async Task<string> AllAsync() => Snapshot(
            JsonSerializer.Serialize(await catalog.BrandsAsync("car", CancellationToken.None)),
            JsonSerializer.Serialize(await catalog.ModelsAsync("car", 1, CancellationToken.None)),
            JsonSerializer.Serialize(await catalog.YearsAsync("car", 11, CancellationToken.None)),
            JsonSerializer.Serialize(await catalog.VersionsAsync("car", 11, 2019, CancellationToken.None)));
        string before = await AllAsync();

        await BehindTheCacheAsync(factory, async db =>
        {
            await db.VehicleBrands.ExecuteUpdateAsync(s => s.SetProperty(x => x.Source, "fipe-2027-01"));
            await db.VehicleModels.ExecuteUpdateAsync(s => s.SetProperty(x => x.Source, "fipe-2027-01"));
            await db.VehicleModelYears.ExecuteUpdateAsync(s => s.SetProperty(x => x.Source, "fipe-2027-01"));
            await db.VehicleVersions.ExecuteUpdateAsync(s => s.SetProperty(x => x.Source, "fipe-2027-01"));
        });
        catalog.Invalidate();

        Assert.AreEqual(before, await AllAsync(), "as listas só dependem de tipo, ids e nomes");
    }

    [TestMethod]
    public async Task ARespostaDoSite_NaoExpoeAOrigemDosDados()
    {
        using WebFactory factory = NewFactory("gazetaonline-2026-09");
        using HttpClient client = factory.CreateClient();

        string json = await client.GetStringAsync("/api/v1/vehicle-catalog/brands?kind=car");

        Assert.DoesNotContain("gazetaonline", json);
        Assert.DoesNotContain("source", json, StringComparison.OrdinalIgnoreCase);
    }
}
