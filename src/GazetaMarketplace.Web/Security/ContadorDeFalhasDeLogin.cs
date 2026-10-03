using System;
using System.Collections.Generic;

namespace GazetaMarketplace.Web.Security;

/// <summary>
/// Bloqueio de entrada por origem (NFR-06, S06): 5 <b>falhas</b> em 15 minutos bloqueiam novas tentativas
/// daquele IP até a mais antiga sair da janela. Entrada bem-sucedida zera a contagem. Conta falhas, não
/// requisições, para que uma redação inteira atrás do mesmo IP possa entrar de manhã. Fica em memória:
/// uma reciclagem do site zera os contadores, o que só afrouxa o bloqueio por alguns minutos.
/// </summary>
public sealed class ContadorDeFalhasDeLogin(TimeProvider time)
{
    public const int Limite = 5;

    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    // Acima disto, cada nova falha também varre as origens que já esfriaram
    private const int LimiteParaVarrer = 1024;

    private readonly object _trava = new();
    private readonly Dictionary<string, List<DateTimeOffset>> _falhas = [];

    public bool EstaBloqueado(string source)
    {
        lock (_trava)
        {
            return ContarNaJanela(source) >= Limite;
        }
    }

    /// <summary>Tempo até o bloqueio terminar; zero se a origem não está bloqueada.</summary>
    public TimeSpan EsperaRestante(string source)
    {
        lock (_trava)
        {
            if (ContarNaJanela(source) < Limite)
            {
                return TimeSpan.Zero;
            }

            return _falhas[source][0] + Window - time.GetUtcNow();
        }
    }

    /// <summary>Registra uma falha; devolve verdadeiro se foi esta que fechou o bloqueio.</summary>
    public bool RegistrarFalha(string source)
    {
        lock (_trava)
        {
            if (_falhas.Count > LimiteParaVarrer)
            {
                foreach (string key in new List<string>(_falhas.Keys))
                {
                    ContarNaJanela(key);
                }
            }

            int antes = ContarNaJanela(source);
            if (antes >= Limite)
            {
                return false;
            }

            if (!_falhas.TryGetValue(source, out List<DateTimeOffset> list))
            {
                list = [];
                _falhas[source] = list;
            }

            list.Add(time.GetUtcNow());
            return antes + 1 >= Limite;
        }
    }

    public void Zerar(string source)
    {
        lock (_trava)
        {
            _falhas.Remove(source);
        }
    }

    // Descarta o que saiu da janela e devolve quantas falhas restam; remove a origem se zerou
    private int ContarNaJanela(string source)
    {
        if (!_falhas.TryGetValue(source, out List<DateTimeOffset> list))
        {
            return 0;
        }

        DateTimeOffset corte = time.GetUtcNow() - Window;
        list.RemoveAll(momento => momento <= corte);
        if (list.Count == 0)
        {
            _falhas.Remove(source);
        }

        return list.Count;
    }
}
