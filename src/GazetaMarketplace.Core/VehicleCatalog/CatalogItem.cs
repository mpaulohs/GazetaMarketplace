namespace GazetaMarketplace.Core.VehicleCatalog;

/// <summary>Um item de lista do catálogo (marca, modelo ou versão), como o formulário e os filtros o recebem.</summary>
public sealed record CatalogItem(int Id, string Name);
