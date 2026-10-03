using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GazetaMarketplace.Infrastructure.Recovery;

/// <summary>
/// Fila em memória dos envios de redefinição (RC-13). A resposta de "Esqueci minha senha" só põe o pedido aqui e volta:
/// quem procura a conta, gera o token e fala com o SendGrid é o <see cref="PasswordRecoveryWorker"/>, depois da resposta.
/// Se o processo reiniciar com pedidos na fila, eles se perdem; a pessoa pede de novo.
/// </summary>
public sealed class PasswordRecoveryQueue
{
    private const int Capacity = 200;

    private readonly Channel<RecoveryJob> _channel = Channel.CreateBounded<RecoveryJob>(
        new BoundedChannelOptions(Capacity) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

    private int _pending;

    public ChannelReader<RecoveryJob> Reader => _channel.Reader;

    /// <summary>Coloca o envio na fila; <c>false</c> se a fila estiver cheia (o pedido é descartado, a resposta ao usuário não muda).</summary>
    public bool TryEnqueue(RecoveryJob job)
    {
        Interlocked.Increment(ref _pending);
        if (_channel.Writer.TryWrite(job))
        {
            return true;
        }

        Interlocked.Decrement(ref _pending);
        return false;
    }

    /// <summary>Chamado pelo worker quando termina um envio, com sucesso ou não.</summary>
    internal void Complete() => Interlocked.Decrement(ref _pending);

    /// <summary>Espera a fila esvaziar e o último envio terminar. Serve aos testes; o site não usa.</summary>
    public async Task WaitUntilIdleAsync(TimeSpan timeout)
    {
        DateTime limit = DateTime.UtcNow + timeout;
        while (Volatile.Read(ref _pending) > 0)
        {
            if (DateTime.UtcNow > limit)
            {
                throw new TimeoutException("A fila de redefinição de senha não esvaziou a tempo.");
            }

            await Task.Delay(10);
        }
    }
}
