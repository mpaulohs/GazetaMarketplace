using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.VehicleCatalog;

/// <summary>
/// As consultas encadeadas marca → modelo → ano → versão de carros e motos (ADR-008). As listas ficam em cache de 10 minutos e a ordem é sempre a
/// mesma: marcas, modelos e versões em ordem alfabética, anos do mais novo ao mais antigo. O catálogo só muda por uma nova carga, feita fora do site.
/// </summary>
/// <remarks>
/// Um item que não existe (marca, modelo ou ano) lança <see cref="Exceptions.NotFoundException"/>; um item que existe e não tem filhos devolve lista
/// vazia. O tipo (<see cref="VehicleKinds"/>) acompanha todas as consultas porque os ids de carros e motos podem coincidir.
/// </remarks>
public interface IVehicleCatalog
{
    Task<IReadOnlyList<CatalogItem>> BrandsAsync(string kind, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogItem>> ModelsAsync(string kind, int brandId, CancellationToken cancellationToken);

    Task<IReadOnlyList<int>> YearsAsync(string kind, int modelId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CatalogItem>> VersionsAsync(string kind, int modelId, int year, CancellationToken cancellationToken);

    /// <summary>Esvazia o cache (usado depois de uma carga feita com o site no ar e nos testes).</summary>
    void Invalidate();
}
