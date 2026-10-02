using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entidades;
using GazetaMarketplace.Core.Interfaces;

namespace GazetaMarketplace.Infrastructure.Data;

/// <inheritdoc cref="IAuditLog"/>
public sealed class AuditLog(AppDbContext contexto, IUsuarioAtual usuarioAtual, TimeProvider tempo) : IAuditLog
{
    public async Task RegistrarAsync(EntradaDeAuditoria entrada, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        ArgumentException.ThrowIfNullOrWhiteSpace(entrada.Acao);
        ArgumentException.ThrowIfNullOrWhiteSpace(entrada.TipoDoAlvo);
        ArgumentException.ThrowIfNullOrWhiteSpace(entrada.IdDoAlvo);

        contexto.AuditEntries.Add(new AuditEntry
        {
            OccurredAt = tempo.GetUtcNow().UtcDateTime,
            ActorId = usuarioAtual.UsuarioId,
            Action = entrada.Acao,
            TargetType = entrada.TipoDoAlvo,
            TargetId = entrada.IdDoAlvo,
            PreviousValue = entrada.ValorAnterior,
            NewValue = entrada.ValorNovo,
            Result = entrada.Resultado,
            CorrelationId = usuarioAtual.CorrelationId
        });

        await contexto.SaveChangesAsync(cancellationToken);
    }
}
