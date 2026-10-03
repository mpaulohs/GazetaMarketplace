using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.HealthChecks;

/// <summary>Pronto = banco acessível e última migration aplicada (ADR-010). Nada interno vai para a resposta.</summary>
public sealed class DatabaseAndMigrationHealthCheck(IDatabaseReadiness readiness, ILogger<DatabaseAndMigrationHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            DatabaseReadiness state = await readiness.CheckAsync(cancellationToken);
            if (!state.IsReady)
            {
                logger.LogWarning("Banco não pronto: acessível={Reachable}, migration em dia={MigrationUpToDate}",
                    state.DatabaseReachable, state.MigrationApplied);
            }

            return state.IsReady ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
        }
        catch (Exception ex)
        {
            // Só o tipo: a mensagem de uma exceção de banco pode conter a cadeia de conexão
            logger.LogWarning("Falha ao verificar o banco: {Type}", ex.GetType().Name);
            return HealthCheckResult.Unhealthy();
        }
    }
}
