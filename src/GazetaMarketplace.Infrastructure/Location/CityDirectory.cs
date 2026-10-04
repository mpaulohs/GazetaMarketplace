using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.Infrastructure.Location;

/// <inheritdoc cref="ICityDirectory"/>
public sealed class CityDirectory(AppDbContext context) : ICityDirectory
{
    public async Task<IReadOnlyList<CityItem>> ByUfAsync(string uf, CancellationToken cancellationToken)
    {
        State state = BrazilianStates.Find(uf)
            ?? throw new ValidationException(new Dictionary<string, string[]> { ["uf"] = ["Informe uma UF válida"] });

        // NameSearch é o nome sem acento e em minúsculas: a lista fica em ordem alfabética como as pessoas esperam ("Água Boa" antes de "Alta Floresta")
        return await context.Cities.AsNoTracking()
            .Where(c => c.Uf == state.Uf)
            .OrderBy(c => c.NameSearch)
            .Select(c => new CityItem(c.IbgeCode, c.Name))
            .ToListAsync(cancellationToken);
    }

    public Task<City> FindByCodeAsync(int ibgeCode, CancellationToken cancellationToken) =>
        context.Cities.AsNoTracking().SingleOrDefaultAsync(c => c.IbgeCode == ibgeCode, cancellationToken);
}
