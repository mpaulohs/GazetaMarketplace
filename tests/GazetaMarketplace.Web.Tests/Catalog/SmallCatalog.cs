using GazetaMarketplace.Core.VehicleCatalog;
using GazetaMarketplace.Infrastructure.Data;

namespace GazetaMarketplace.Web.Tests.Catalog;

/// <summary>
/// Catálogo pequeno e feito à mão para os testes do site. A Honda de carros e a Honda de motos são a marca 1, o modelo 10 existe nos dois tipos
/// e a versão 100 também: tudo que a chave composta precisa separar. Os nomes começam sempre em maiúscula para a ordem não depender da collation.
/// </summary>
internal static class SmallCatalog
{
    public static void Seed(AppDbContext context, string source = "teste")
    {
        context.VehicleBrands.AddRange(
            Brand(1, "car", "Honda", source), Brand(2, "car", "Toyota", source), Brand(3, "car", "Audi", source),
            Brand(1, "moto", "Honda", source), Brand(2, "moto", "BMW", source));
        context.VehicleModels.AddRange(
            Model(10, "car", 1, "Fit", source), Model(11, "car", 1, "Civic", source), Model(12, "car", 1, "City", source),
            Model(10, "moto", 1, "CG 160", source));
        context.VehicleModelYears.AddRange(
            Year(11, 2018, "car", source), Year(11, 2020, "car", source), Year(11, 2019, "car", source), Year(11, 2021, "car", source),
            Year(10, 2022, "car", source), Year(10, 2022, "moto", source));
        context.VehicleVersions.AddRange(
            Version(100, "car", 11, 2019, "LX", source), Version(101, "car", 11, 2019, "Advance", source), Version(102, "car", 11, 2019, "EX 2.0", source),
            Version(100, "moto", 10, 2022, "Standard", source));
        context.SaveChanges();
    }

    private static VehicleBrand Brand(int id, string kind, string name, string source) => new() { Id = id, Kind = kind, Name = name, Source = source };

    private static VehicleModel Model(int id, string kind, int brandId, string name, string source) => new() { Id = id, Kind = kind, BrandId = brandId, Name = name, Source = source };

    private static VehicleModelYear Year(int modelId, int year, string kind, string source) => new() { ModelId = modelId, Year = year, Kind = kind, Source = source };

    private static VehicleVersion Version(int id, string kind, int modelId, int year, string name, string source) =>
        new() { Id = id, Kind = kind, ModelId = modelId, Year = year, Name = name, Source = source };
}
