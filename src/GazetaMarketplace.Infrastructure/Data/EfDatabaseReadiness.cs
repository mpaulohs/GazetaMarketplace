using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.Infrastructure.Data;

/// <summary>Banco acessível e nenhuma migration pendente (ADR-004, risco "esquecer o script").</summary>
public sealed class EfDatabaseReadiness(AppDbContext context) : IDatabaseReadiness
{
    public async Task<DatabaseReadiness> CheckAsync(CancellationToken cancellationToken)
    {
        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            return new DatabaseReadiness(false, false);
        }

        var pending = await context.Database.GetPendingMigrationsAsync(cancellationToken);
        return new DatabaseReadiness(true, !pending.Any());
    }
}
