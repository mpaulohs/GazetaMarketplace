using System;
using System.Collections.Generic;

namespace GazetaMarketplace.Web.Seguranca;

/// <summary>
/// Bloqueio de entrada por origem (NFR-06, S06): 5 <b>falhas</b> em 15 minutos bloqueiam novas tentativas
/// daquele IP até a mais antiga sair da janela. Entrada bem-sucedida zera a contagem. Conta falhas, não
/// requisições, para que uma redação inteira atrás do mesmo IP possa entrar de manhã. Fica em memória:
/// uma reciclagem do site zera os contadores, o que só afrouxa o bloqueio por alguns minutos.
/// </summary>
public sealed class ContadorDeFalhasDeLogin(TimeProvider tempo)
{
    public const int Limite = 5;

    public static readonly TimeSpan Janela = TimeSpan.FromMinutes(15);

    // Acima disto, cada nova falha também varre as origens que já esfriaram
    private const int LimiteParaVarrer = 1024;

    private readonly object _trava = new();
    private readonly Dictionary<string, List<DateTimeOffset>> _falhas = [];

    public bool EstaBloqueado(string origem)
    {
        lock (_trava)
        {
            return ContarNaJanela(origem) >= Limite;
        }
    }

    /// <summary>Tempo até o bloqueio terminar; zero se a origem não está bloqueada.</summary>
    public TimeSpan EsperaRestante(string origem)
    {
        lock (_trava)
        {
            if (ContarNaJanela(origem) < Limite)
            {
                return TimeSpan.Zero;
            }

            return _falhas[origem][0] + Janela - tempo.GetUtcNow();
        }
    }

    /// <summary>Registra uma falha; devolve verdadeiro se foi esta que fechou o bloqueio.</summary>
    public bool RegistrarFalha(string origem)
    {
        lock (_trava)
        {
            if (_falhas.Count > LimiteParaVarrer)
            {
                foreach (string chave in new List<string>(_falhas.Keys))
                {
                    ContarNaJanela(chave);
                }
            }

            int antes = ContarNaJanela(origem);
            if (antes >= Limite)
            {
                return false;
            }

            if (!_falhas.TryGetValue(origem, out List<DateTimeOffset> lista))
            {
                lista = [];
                _falhas[origem] = lista;
            }

            lista.Add(tempo.GetUtcNow());
            return antes + 1 >= Limite;
        }
    }

    public void Zerar(string origem)
    {
        lock (_trava)
        {
            _falhas.Remove(origem);
        }
    }

    // Descarta o que saiu da janela e devolve quantas falhas restam; remove a origem se zerou
    private int ContarNaJanela(string origem)
    {
        if (!_falhas.TryGetValue(origem, out List<DateTimeOffset> lista))
        {
            return 0;
        }

        DateTimeOffset corte = tempo.GetUtcNow() - Janela;
        lista.RemoveAll(momento => momento <= corte);
        if (lista.Count == 0)
        {
            _falhas.Remove(origem);
        }

        return lista.Count;
    }
}
