using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entidades;

namespace GazetaMarketplace.Core.Interfaces;

/// <summary>O que registrar. Ator, data e correlação vêm do contexto, não de quem chama.</summary>
public sealed record EntradaDeAuditoria(
    string Acao,
    string TipoDoAlvo,
    string IdDoAlvo,
    ResultadoDaAuditoria Resultado = ResultadoDaAuditoria.Sucesso,
    string ValorAnterior = null,
    string ValorNovo = null);

/// <summary>
/// Serviço único de auditoria de ações (RC-16). Só acrescenta: não há operação de editar nem apagar.
/// Persiste no mesmo DbContext do escopo, então mudanças pendentes nele vão na mesma transação:
/// chame antes de concluir a unidade de trabalho.
/// </summary>
public interface IAuditLog
{
    Task RegistrarAsync(EntradaDeAuditoria entrada, CancellationToken cancellationToken);
}
