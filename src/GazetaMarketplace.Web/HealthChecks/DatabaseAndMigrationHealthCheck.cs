using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.HealthChecks;

/// <summary>Pronto = banco acessível e última migration aplicada (ADR-010). Nada interno vai para a resposta.</summary>
public sealed class DatabaseAndMigrationHealthCheck(IProntidaoDoBanco prontidao, ILogger<DatabaseAndMigrationHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            ProntidaoDoBanco estado = await prontidao.VerificarAsync(cancellationToken);
            if (!estado.Pronto)
            {
                logger.LogWarning("Banco não pronto: acessível={Acessivel}, migration em dia={MigrationEmDia}",
                    estado.BancoAcessivel, estado.MigrationAplicada);
            }

            return estado.Pronto ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
        }
        catch (Exception ex)
        {
            // Só o tipo: a mensagem de uma exceção de banco pode conter a cadeia de conexão
            logger.LogWarning("Falha ao verificar o banco: {Tipo}", ex.GetType().Name);
            return HealthCheckResult.Unhealthy();
        }
    }
}
