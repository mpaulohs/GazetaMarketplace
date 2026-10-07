using System;
using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Infrastructure.Identity;

/// <summary>A espera do atraso progressivo (SC-03). Separada para os testes não esperarem de verdade.</summary>
public interface ILoginDelay
{
    Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken);
}

/// <summary>Espera de verdade.</summary>
public sealed class LoginDelay : ILoginDelay
{
    public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken) => Task.Delay(delay, cancellationToken);
}
