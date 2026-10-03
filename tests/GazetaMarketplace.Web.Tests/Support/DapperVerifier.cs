using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>
/// Confere a regra do ADR-004: toda chamada Execute* do Dapper tem um comentário "// Dapper: motivo"
/// nas linhas imediatamente acima. Só olha arquivos que usam "using Dapper;".
/// </summary>
internal static partial class DapperVerifier
{
    public static IReadOnlyList<int> LinesWithoutJustification(string code)
    {
        List<int> problems = [];
        if (!code.Contains("using Dapper;", StringComparison.Ordinal))
        {
            return problems;
        }

        string[] lines = code.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (!Run().IsMatch(lines[i]))
            {
                continue;
            }

            bool justified = false;
            for (int previous = Math.Max(0, i - 4); previous <= i; previous++)
            {
                if (Justification().IsMatch(lines[previous]))
                {
                    justified = true;
                }
            }

            if (!justified)
            {
                problems.Add(i + 1);
            }
        }

        return problems;
    }

    [GeneratedRegex(@"\.(Execute|ExecuteAsync|ExecuteScalar|ExecuteScalarAsync|ExecuteReader|ExecuteReaderAsync)\s*\(")]
    private static partial Regex Run();

    [GeneratedRegex(@"//\s*Dapper:\s*\S.{7,}")]
    private static partial Regex Justification();
}
