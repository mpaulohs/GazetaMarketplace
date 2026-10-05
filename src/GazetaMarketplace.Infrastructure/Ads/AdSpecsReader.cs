using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Core.VehicleCatalog;

namespace GazetaMarketplace.Infrastructure.Ads;

/// <summary>Resolve no catálogo de veículos os nomes de marca, modelo e versão do anúncio e entrega as características prontas (<see cref="AdSpecs"/>).</summary>
public sealed class AdSpecsReader(IVehicleCatalog catalog) : IAdSpecsReader
{
    public async Task<IReadOnlyList<AdSpec>> ReadAsync(Ad ad, int categoryId, FieldGroup group, CancellationToken cancellationToken)
    {
        if (!AdAttributes.TryParse(ad.Attributes, out AdAttributes attributes))
        {
            return [];
        }

        Dictionary<string, string> labels = [];
        FieldDefinition[] chain = [.. group.FieldsFor(categoryId).Where(f => f.Type == FieldType.CatalogItem)];
        if (chain.Length > 0)
        {
            string kind = chain[0].Catalog.Kind == CatalogKind.Motorcycle ? VehicleKinds.Moto : VehicleKinds.Car;
            await ResolveAsync(attributes, kind, labels, cancellationToken);
        }

        return AdSpecs.Build(attributes, group, categoryId, labels);
    }

    // Um nome que o catálogo não conhece (carga trocada, id antigo) simplesmente não aparece
    private async Task ResolveAsync(AdAttributes attributes, string kind, Dictionary<string, string> labels, CancellationToken cancellationToken)
    {
        // O ano do catálogo é o próprio número
        if (attributes.TryGetInt("modelYear", out int modelYear))
        {
            labels["modelYear"] = modelYear.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        try
        {
            if (attributes.TryGetInt("brandId", out int brandId))
            {
                labels["brandId"] = (await catalog.BrandsAsync(kind, cancellationToken)).FirstOrDefault(b => b.Id == brandId)?.Name;
                if (attributes.TryGetInt("modelId", out int modelId))
                {
                    labels["modelId"] = (await catalog.ModelsAsync(kind, brandId, cancellationToken)).FirstOrDefault(m => m.Id == modelId)?.Name;
                    if (attributes.TryGetInt("modelYear", out int year) && attributes.TryGetInt("versionId", out int versionId))
                    {
                        labels["versionId"] = (await catalog.VersionsAsync(kind, modelId, year, cancellationToken)).FirstOrDefault(v => v.Id == versionId)?.Name;
                    }
                }
            }
        }
        catch (NotFoundException)
        {
        }
    }
}
