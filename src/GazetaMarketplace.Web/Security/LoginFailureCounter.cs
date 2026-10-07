using System;
using System.Collections.Generic;

namespace GazetaMarketplace.Web.Security;

/// <summary>
/// Bloqueio de entrada por origem (NFR-06, S06): 5 <b>falhas</b> em 15 minutos bloqueiam novas tentativas
/// daquele IP até a mais antiga sair da janela. Entrada bem-sucedida zera a contagem. Conta falhas, não
/// requisições, para que uma redação inteira atrás do mesmo IP possa entrar de manhã. Fica em memória:
/// uma reciclagem do site zera os contadores, o que só afrouxa o bloqueio por alguns minutos.
/// </summary>
public sealed class LoginFailureCounter(TimeProvider time, int limit = AuthLimits.Default)
{
    /// <summary>Falhas em <see cref="Window"/> que bloqueiam a origem (5 com proxies conhecidos, 20 enquanto a lista não vem do provedor: <see cref="AuthLimits"/>).</summary>
    public int Limit { get; } = limit;

    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    // Acima disto, cada nova falha também varre as origens que já esfriaram
    private const int SweepThreshold = 1024;

    private readonly object _lock = new();
    private readonly Dictionary<string, List<DateTimeOffset>> _failures = [];

    public bool IsBlocked(string source)
    {
        lock (_lock)
        {
            return CountInWindow(source) >= Limit;
        }
    }

    /// <summary>Tempo até o bloqueio terminar; zero se a origem não está bloqueada.</summary>
    public TimeSpan RemainingWait(string source)
    {
        lock (_lock)
        {
            if (CountInWindow(source) < Limit)
            {
                return TimeSpan.Zero;
            }

            return _failures[source][0] + Window - time.GetUtcNow();
        }
    }

    /// <summary>Registra uma falha; devolve verdadeiro se foi esta que fechou o bloqueio.</summary>
    public bool RecordFailure(string source)
    {
        lock (_lock)
        {
            if (_failures.Count > SweepThreshold)
            {
                foreach (string key in new List<string>(_failures.Keys))
                {
                    CountInWindow(key);
                }
            }

            int before = CountInWindow(source);
            if (before >= Limit)
            {
                return false;
            }

            if (!_failures.TryGetValue(source, out List<DateTimeOffset> list))
            {
                list = [];
                _failures[source] = list;
            }

            list.Add(time.GetUtcNow());
            return before + 1 >= Limit;
        }
    }

    public void Reset(string source)
    {
        lock (_lock)
        {
            _failures.Remove(source);
        }
    }

    // Descarta o que saiu da janela e devolve quantas falhas restam; remove a origem se zerou
    private int CountInWindow(string source)
    {
        if (!_failures.TryGetValue(source, out List<DateTimeOffset> list))
        {
            return 0;
        }

        DateTimeOffset cutoff = time.GetUtcNow() - Window;
        list.RemoveAll(moment => moment <= cutoff);
        if (list.Count == 0)
        {
            _failures.Remove(source);
        }

        return list.Count;
    }
}
