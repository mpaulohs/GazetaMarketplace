using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;

namespace GazetaMarketplace.Web.Tests.Suporte;

/// <summary>Fake escrita à mão de <see cref="IProntidaoDoBanco"/>; conta as chamadas.</summary>
internal sealed class FakeProntidaoDoBanco : IProntidaoDoBanco
{
    private int _chamadas;

    public ProntidaoDoBanco Resultado { get; set; } = new(true, true);

    public Exception Falha { get; set; }

    public int Chamadas => _chamadas;

    public Task<ProntidaoDoBanco> VerificarAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _chamadas);
        if (Falha is not null)
        {
            throw Falha;
        }

        return Task.FromResult(Resultado);
    }
}
