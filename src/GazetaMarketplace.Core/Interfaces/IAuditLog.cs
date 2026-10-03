using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entities;

namespace GazetaMarketplace.Core.Interfaces;

/// <summary>O que registrar. Ator, data e correlação vêm do contexto, não de quem chama.</summary>
public sealed record AuditRecord(
    string Action,
    string TargetType,
    string TargetId,
    AuditResult Result = AuditResult.Success,
    string PreviousValue = null,
    string NewValue = null);

/// <summary>
/// Serviço único de auditoria de ações (RC-16). Só acrescenta: não há operação de editar nem apagar.
/// Persiste no mesmo DbContext do escopo, então mudanças pendentes nele vão na mesma transação:
/// chame antes de concluir a unidade de trabalho.
/// </summary>
public interface IAuditLog
{
    Task RecordAsync(AuditRecord entry, CancellationToken cancellationToken);
}
