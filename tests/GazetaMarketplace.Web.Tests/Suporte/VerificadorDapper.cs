using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GazetaMarketplace.Web.Tests.Suporte;

/// <summary>
/// Confere a regra do ADR-004: toda chamada Execute* do Dapper tem um comentário "// Dapper: motivo"
/// nas linhas imediatamente acima. Só olha arquivos que usam "using Dapper;".
/// </summary>
internal static partial class VerificadorDapper
{
    public static IReadOnlyList<int> LinhasSemJustificativa(string codigo)
    {
        List<int> problemas = [];
        if (!codigo.Contains("using Dapper;", StringComparison.Ordinal))
        {
            return problemas;
        }

        string[] linhas = codigo.Split('\n');
        for (int i = 0; i < linhas.Length; i++)
        {
            if (!Execucao().IsMatch(linhas[i]))
            {
                continue;
            }

            bool justificada = false;
            for (int anterior = Math.Max(0, i - 4); anterior <= i; anterior++)
            {
                if (Justificativa().IsMatch(linhas[anterior]))
                {
                    justificada = true;
                }
            }

            if (!justificada)
            {
                problemas.Add(i + 1);
            }
        }

        return problemas;
    }

    [GeneratedRegex(@"\.(Execute|ExecuteAsync|ExecuteScalar|ExecuteScalarAsync|ExecuteReader|ExecuteReaderAsync)\s*\(")]
    private static partial Regex Execucao();

    [GeneratedRegex(@"//\s*Dapper:\s*\S.{7,}")]
    private static partial Regex Justificativa();
}
