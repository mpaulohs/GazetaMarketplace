using System;
using System.Threading;
using System.Threading.Tasks;

namespace GazetaMarketplace.Infrastructure.Caching;

/// <summary>
/// Um valor em cache de memória que vence depois de <c>lifetime</c> (pelo <see cref="TimeProvider"/>) e que pode ser invalidado. Uma carga
/// por vez: leituras ao mesmo tempo num cache vazio esperam a mesma carga. Se alguém invalida <b>durante</b> a carga, o valor lido pode já ser
/// velho: é devolvido a quem pediu, mas não é guardado.
/// </summary>
internal sealed class ExpiringCache<T>(TimeProvider time, TimeSpan lifetime, Func<CancellationToken, Task<T>> load)
    where T : class
{
    private readonly SemaphoreSlim _loading = new(1, 1);
    private volatile Entry _cached;
    private int _version;

    public async Task<T> GetAsync(CancellationToken cancellationToken)
    {
        Entry current = _cached;
        if (current is not null && time.GetUtcNow() < current.ExpiresAt)
        {
            return current.Value;
        }

        await _loading.WaitAsync(cancellationToken);
        try
        {
            // Quem esperou na fila encontra o valor que outra requisição acabou de carregar
            current = _cached;
            if (current is not null && time.GetUtcNow() < current.ExpiresAt)
            {
                return current.Value;
            }

            int versionBeforeLoad = Volatile.Read(ref _version);
            T value = await load(cancellationToken);
            if (versionBeforeLoad == Volatile.Read(ref _version))
            {
                _cached = new Entry(value, time.GetUtcNow() + lifetime);
            }

            return value;
        }
        finally
        {
            _loading.Release();
        }
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref _version);
        _cached = null;
    }

    private sealed record Entry(T Value, DateTimeOffset ExpiresAt);
}
