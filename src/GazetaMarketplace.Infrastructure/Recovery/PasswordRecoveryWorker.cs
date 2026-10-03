using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Infrastructure.Recovery;

/// <summary>Esvazia a <see cref="PasswordRecoveryQueue"/> um envio por vez. Falha de um envio vai para o log e nunca para a pessoa (RC-13).</summary>
public sealed class PasswordRecoveryWorker(
    PasswordRecoveryQueue queue,
    IServiceScopeFactory scopes,
    ILogger<PasswordRecoveryWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (RecoveryJob job in queue.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessAsync(job, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // desligamento do site
        }
    }

    private async Task ProcessAsync(RecoveryJob job, CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<PasswordRecoveryMailer>().SendAsync(job, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            // O destinatário aparece mascarado pelo MaskingEnricher; o traceId liga esta linha ao pedido
            log.LogError(error, "Falha ao enviar o e-mail de redefinição de senha para {Recipient} (traceId {TraceId})", job.Email, job.TraceId);
        }
        finally
        {
            queue.Complete();
        }
    }
}
