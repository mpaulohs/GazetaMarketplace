using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.VehicleCatalog;
using GazetaMarketplace.Infrastructure.Ads;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Review;

/// <summary>A leitura das características com os nomes do catálogo de veículos (4.1): o que o catálogo não conhece não aparece, mas deixa um aviso no registro.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdSpecsReaderTests
#pragma warning restore CA1515
{
    private const int CarsCategory = 33;

    private static Ad CarAd() => Ad.CreateDraft("Civic", 1).WithCar();

    [TestMethod]
    public async Task ModeloQueOCatalogoNaoConhece_SomeDaLista_EDeixaUmAvisoComOAnuncioETipo()
    {
        ListLogger<AdSpecsReader> logger = new();
        AdSpecsReader reader = new(new MissingModelCatalog(), logger);

        IReadOnlyList<AdSpec> specs = await reader.ReadAsync(CarAd(), CarsCategory, FieldGroupRegistry.Get(FieldGroupKeys.Cars), CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "Marca: Honda", "Ano: 2019", "Quilometragem: 45.000 km" }, specs.Select(s => s.Label + ": " + s.Value).ToArray(), "o nome que faltou não vira o id");
        (LogLevel Level, string Message) warning = logger.Entries.Single();
        Assert.AreEqual(LogLevel.Warning, warning.Level);
        StringAssert.Contains(warning.Message, "anúncio 0");
        StringAssert.Contains(warning.Message, "tipo car");
    }

    [TestMethod]
    public async Task CatalogoCompleto_NaoGeraAviso()
    {
        ListLogger<AdSpecsReader> logger = new();
        AdSpecsReader reader = new(new MissingModelCatalog(modelsExist: true), logger);

        IReadOnlyList<AdSpec> specs = await reader.ReadAsync(CarAd(), CarsCategory, FieldGroupRegistry.Get(FieldGroupKeys.Cars), CancellationToken.None);

        CollectionAssert.Contains(specs.Select(s => s.Label + ": " + s.Value).ToArray(), "Modelo: Civic");
        Assert.IsEmpty(logger.Entries);
    }

    private sealed class MissingModelCatalog(bool modelsExist = false) : IVehicleCatalog
    {
        public Task<IReadOnlyList<CatalogItem>> BrandsAsync(string kind, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CatalogItem>>([new CatalogItem(1, "Honda")]);

        public Task<IReadOnlyList<CatalogItem>> ModelsAsync(string kind, int brandId, CancellationToken cancellationToken) =>
            modelsExist ? Task.FromResult<IReadOnlyList<CatalogItem>>([new CatalogItem(11, "Civic")]) : throw new NotFoundException("Marca", brandId);

        public Task<IReadOnlyList<int>> YearsAsync(string kind, int modelId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<int>>([]);

        public Task<IReadOnlyList<CatalogItem>> VersionsAsync(string kind, int modelId, int year, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CatalogItem>>([]);

        public void Invalidate()
        {
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}

internal static class CarAdExtensions
{
    internal static Ad WithCar(this Ad ad)
    {
        ad.SetCategory(33);
        ad.SetAttributes(new AdAttributes().Set("brandId", 1).Set("modelId", 11).Set("modelYear", 2019).Set("versionId", 100).Set("km", 45000));
        return ad;
    }
}
