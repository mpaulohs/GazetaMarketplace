using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.Infrastructure.Data;

/// <summary>Banco acessível e nenhuma migration pendente (ADR-004, risco "esquecer o script").</summary>
public sealed class ProntidaoDoBancoEf(AppDbContext contexto) : IProntidaoDoBanco
{
    public async Task<ProntidaoDoBanco> VerificarAsync(CancellationToken cancellationToken)
    {
        if (!await contexto.Database.CanConnectAsync(cancellationToken))
        {
            return new ProntidaoDoBanco(false, false);
        }

        var pendentes = await contexto.Database.GetPendingMigrationsAsync(cancellationToken);
        return new ProntidaoDoBanco(true, !pendentes.Any());
    }
}
