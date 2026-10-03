using System;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Fake escrita à mão de <see cref="IDatabaseReadiness"/>; conta as chamadas.</summary>
internal sealed class FakeDatabaseReadiness : IDatabaseReadiness
{
    private int _calls;

    public DatabaseReadiness Result { get; set; } = new(true, true);

    public Exception Failure { get; set; }

    public int Calls => _calls;

    public Task<DatabaseReadiness> CheckAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult(Result);
    }
}
