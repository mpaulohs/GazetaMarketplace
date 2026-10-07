using System;
using System.Collections.Generic;

namespace GazetaMarketplace.Infrastructure.Identity;

/// <summary>O que aconteceu ao registrar uma falha de entrada: se ela fechou um bloqueio e quantos bloqueios seguidos a conta já teve.</summary>
public readonly record struct LockoutOutcome(bool BlockStarted, int ConsecutiveBlocks);

/// <summary>
/// Bloqueio de entrada por <b>conta e origem</b> (SC-03): 5 falhas em 15 minutos de um mesmo IP contra uma mesma conta bloqueiam
/// <i>aquele IP naquela conta</i> até a falha mais antiga sair da janela. Quem erra a senha do Administrador de outra rede não o
/// impede de entrar da própria: o bloqueio antigo (por conta, para qualquer IP) deixava qualquer pessoa travar a conta de outra a
/// cada 15 minutos com 5 pedidos. Para quem insiste de redes diferentes: depois de 3 bloqueios seguidos da mesma conta cada
/// tentativa espera 1, 2, 4, 8 e 16 segundos (teto), e a partir do 2.º bloqueio o dono da conta é avisado por e-mail.
/// Fica em memória, como o <c>LoginFailureCounter</c>: reciclar o site zera os contadores, o que só afrouxa por alguns minutos.
/// </summary>
public sealed class AccountOriginLockout(TimeProvider time)
{
    public const int Limit = 5;

    /// <summary>Quantos bloqueios seguidos da mesma conta fazem as tentativas começarem a esperar.</summary>
    public const int DelayStartsAtBlock = 3;

    /// <summary>A partir de quantos bloqueios seguidos o dono da conta é avisado por e-mail.</summary>
    public const int NoticeAtBlock = 2;

    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    /// <summary>Sem novo bloqueio por esse tempo, os bloqueios deixam de ser "seguidos" e a contagem recomeça.</summary>
    public static readonly TimeSpan BlockMemory = TimeSpan.FromHours(1);

    public static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(16);

    // Acima disto, cada nova falha também varre o que já esfriou
    private const int SweepThreshold = 1024;

    private readonly object _lock = new();
    private readonly Dictionary<(int User, string Origin), List<DateTimeOffset>> _failures = [];
    private readonly Dictionary<int, (int Blocks, DateTimeOffset Last)> _blocks = [];

    public bool IsBlocked(int userId, string origin)
    {
        lock (_lock)
        {
            return CountInWindow((userId, origin)) >= Limit;
        }
    }

    /// <summary>Tempo até o bloqueio daquele IP naquela conta terminar; zero se não está bloqueado.</summary>
    public TimeSpan RemainingWait(int userId, string origin)
    {
        lock (_lock)
        {
            if (CountInWindow((userId, origin)) < Limit)
            {
                return TimeSpan.Zero;
            }

            return _failures[(userId, origin)][0] + Window - time.GetUtcNow();
        }
    }

    /// <summary>Registra uma falha de senha de uma conta que existe. Falha numa origem já bloqueada não conta de novo.</summary>
    public LockoutOutcome RecordFailure(int userId, string origin)
    {
        lock (_lock)
        {
            Sweep();
            (int, string) key = (userId, origin);
            int before = CountInWindow(key);
            if (before >= Limit)
            {
                return new LockoutOutcome(false, ConsecutiveBlocks(userId));
            }

            if (!_failures.TryGetValue(key, out List<DateTimeOffset> list))
            {
                list = [];
                _failures[key] = list;
            }

            list.Add(time.GetUtcNow());
            if (before + 1 < Limit)
            {
                return new LockoutOutcome(false, ConsecutiveBlocks(userId));
            }

            int blocks = ConsecutiveBlocks(userId) + 1;
            _blocks[userId] = (blocks, time.GetUtcNow());
            return new LockoutOutcome(true, blocks);
        }
    }

    /// <summary>Quanto cada tentativa contra a conta espera, de qualquer origem: zero até o 2.º bloqueio seguido, depois 1, 2, 4, 8 e 16 segundos.</summary>
    public TimeSpan DelayFor(int userId)
    {
        int blocks;
        lock (_lock)
        {
            blocks = ConsecutiveBlocks(userId);
        }

        if (blocks < DelayStartsAtBlock)
        {
            return TimeSpan.Zero;
        }

        double seconds = Math.Pow(2, Math.Min(blocks - DelayStartsAtBlock, 4));
        TimeSpan delay = TimeSpan.FromSeconds(seconds);
        return delay > MaxDelay ? MaxDelay : delay;
    }

    /// <summary>Entrada bem-sucedida: zera as falhas daquele IP naquela conta e a sequência de bloqueios da conta.</summary>
    public void Reset(int userId, string origin)
    {
        lock (_lock)
        {
            _failures.Remove((userId, origin));
            _blocks.Remove(userId);
        }
    }

    /// <summary>Senha redefinida pelo link ou pelo Administrador, ou conta reativada (RC-12): a conta volta a zero, de qualquer origem.</summary>
    public void ClearAccount(int userId)
    {
        lock (_lock)
        {
            foreach ((int User, string Origin) key in new List<(int User, string Origin)>(_failures.Keys))
            {
                if (key.User == userId)
                {
                    _failures.Remove(key);
                }
            }

            _blocks.Remove(userId);
        }
    }

    // Os bloqueios seguidos esfriam: sem novo bloqueio por BlockMemory a conta recomeça
    private int ConsecutiveBlocks(int userId)
    {
        if (!_blocks.TryGetValue(userId, out (int Blocks, DateTimeOffset Last) state))
        {
            return 0;
        }

        if (time.GetUtcNow() - state.Last > BlockMemory)
        {
            _blocks.Remove(userId);
            return 0;
        }

        return state.Blocks;
    }

    private void Sweep()
    {
        if (_failures.Count <= SweepThreshold)
        {
            return;
        }

        foreach ((int User, string Origin) key in new List<(int User, string Origin)>(_failures.Keys))
        {
            CountInWindow(key);
        }
    }

    private int CountInWindow((int User, string Origin) key)
    {
        if (!_failures.TryGetValue(key, out List<DateTimeOffset> list))
        {
            return 0;
        }

        DateTimeOffset cutoff = time.GetUtcNow() - Window;
        list.RemoveAll(moment => moment <= cutoff);
        if (list.Count == 0)
        {
            _failures.Remove(key);
        }

        return list.Count;
    }
}
