using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;

namespace GazetaMarketplace.Core.Location;

/// <summary>A lista oficial de municípios (IBGE). Vazia para uma UF enquanto a carga real não foi feita: então vale a regra de padronização.</summary>
public interface ICityDirectory
{
    /// <summary>As cidades de uma UF em ordem alfabética sem considerar acento; lista vazia se a UF não tem carga.</summary>
    /// <exception cref="ValidationException">A sigla não é de uma das 27 UFs.</exception>
    Task<IReadOnlyList<CityItem>> ByUfAsync(string uf, CancellationToken cancellationToken);

    /// <summary>O município pelo código do IBGE, ou nulo se não está na lista.</summary>
    Task<City> FindByCodeAsync(int ibgeCode, CancellationToken cancellationToken);
}
