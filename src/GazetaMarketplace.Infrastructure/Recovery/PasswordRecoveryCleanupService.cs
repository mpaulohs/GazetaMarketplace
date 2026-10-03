using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Infrastructure.Recovery;

/// <summary>
/// Apaga uma vez por dia as tentativas de redefinição com mais de 24 horas. Roda 1 minuto depois da partida e a cada 24 horas;
/// como o IIS recicla o processo, na prática roda a cada reinício também (apagar de novo é inofensivo).
/// </summary>
public sealed class PasswordRecoveryCleanupService(
    IServiceScopeFactory scopes,
    TimeProvider time,
    ILogger<PasswordRecoveryCleanupService> log) : BackgroundService
{
    private static readonly TimeSpan FirstRun = TimeSpan.FromMinutes(1);

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(FirstRun, time, stoppingToken);
            using PeriodicTimer timer = new(Interval, time);
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // desligamento do site
        }
    }

    /// <summary>Uma limpeza. Falha de banco vai para o log e a rotina tenta de novo no próximo dia.</summary>
    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = scopes.CreateScope();
            int removed = await PasswordRecoveryService.PurgeAsync(
                scope.ServiceProvider.GetRequiredService<AppDbContext>(), time, cancellationToken);
            log.LogInformation("Limpeza das tentativas de redefinição de senha: {Removed} apagadas", removed);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            log.LogError(error, "Falha na limpeza das tentativas de redefinição de senha");
        }
    }
}
