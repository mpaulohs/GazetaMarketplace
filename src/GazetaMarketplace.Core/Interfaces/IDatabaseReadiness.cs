using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Core.Interfaces;

/// <summary>Estado do banco para o <c>/health/ready</c>: acessível e com a última migration aplicada.</summary>
public sealed record DatabaseReadiness(bool DatabaseReachable, bool MigrationApplied)
{
    public bool IsReady => DatabaseReachable && MigrationApplied;
}

/// <summary>Consulta a prontidão do banco. A implementação real (EF Core) chega com a tarefa 0.6.</summary>
public interface IDatabaseReadiness
{
    Task<DatabaseReadiness> CheckAsync(CancellationToken cancellationToken);
}
