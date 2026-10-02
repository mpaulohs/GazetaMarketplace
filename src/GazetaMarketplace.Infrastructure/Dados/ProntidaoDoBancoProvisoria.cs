using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;

namespace GazetaMarketplace.Infrastructure.Dados;

/// <summary>
/// Provisória até a tarefa 0.6 (AppDbContext): nunca declara o banco pronto, para o
/// <c>/health/ready</c> não afirmar o que ainda não é verificado.
/// </summary>
internal sealed class ProntidaoDoBancoProvisoria : IProntidaoDoBanco
{
    public Task<ProntidaoDoBanco> VerificarAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new ProntidaoDoBanco(false, false));
}
