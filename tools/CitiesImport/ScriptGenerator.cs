using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CitiesImport;

/// <summary>
/// Gera o script de carga dos municípios: um <c>MERGE</c> por lote de até 500 linhas, numa transação, idempotente. O texto é determinístico (ordem por
/// código, nada de data): o mesmo arquivo do IBGE gera sempre o mesmo script. O <c>MERGE</c> nunca apaga: município que o IBGE extinguir continua na
/// tabela até ser removido à mão.
/// </summary>
internal static class ScriptGenerator
{
    public const int BatchSize = 500;

    // BIN2: a comparação diferencia maiúscula e acento, para um nome corrigido pelo IBGE ser atualizado
    private const string Exact = "COLLATE Latin1_General_100_BIN2";

    public static string Script(IReadOnlyList<CityRow> cities, string source, bool sample)
    {
        ArgumentNullException.ThrowIfNull(cities);
        StringBuilder script = new();
        script.AppendLine("-- Municípios do IBGE (tarefa 3.2). GERADO por tools/CitiesImport: não edite à mão; gere de novo.");
        script.AppendLine(CultureInfo.InvariantCulture, $"-- Origem: {source}");
        if (sample)
        {
            script.AppendLine("-- AMOSTRA DE TESTE: poucos municípios, com códigos a conferir na carga real. NÃO aplique em produção.");
        }

        script.AppendLine(CultureInfo.InvariantCulture, $"-- Municípios: {cities.Count}");
        script.AppendLine("-- Idempotente: pode ser aplicado mais de uma vez. Aplique com 'sqlcmd -I' ou na ferramenta de SQL do provedor, depois das migrations.");
        script.AppendLine("SET QUOTED_IDENTIFIER ON;");
        script.AppendLine("GO");
        script.AppendLine("SET XACT_ABORT ON;");
        script.AppendLine("BEGIN TRANSACTION;");
        script.AppendLine();
        foreach (string statement in Statements(cities))
        {
            script.AppendLine(statement);
            script.AppendLine();
        }

        script.AppendLine("COMMIT TRANSACTION;");
        script.AppendLine("GO");
        return script.ToString();
    }

    public static IEnumerable<string> Statements(IReadOnlyList<CityRow> cities)
    {
        ArgumentNullException.ThrowIfNull(cities);
        foreach (CityRow[] batch in cities.OrderBy(c => c.Code).Chunk(BatchSize))
        {
            string rows = string.Join(",\n    ", batch.Select(c => string.Create(CultureInfo.InvariantCulture, $"({c.Code}, {Unicode(c.Name)}, '{c.Uf}', {Unicode(c.NameSearch)})")));
            yield return $"""
                MERGE [Cities] AS t
                USING (VALUES
                    {rows}
                ) AS s ([IbgeCode], [Name], [Uf], [NameSearch])
                ON t.[IbgeCode] = s.[IbgeCode]
                WHEN MATCHED AND (t.[Name] {Exact} <> s.[Name] {Exact} OR t.[Uf] <> s.[Uf] OR t.[NameSearch] {Exact} <> s.[NameSearch] {Exact}) THEN UPDATE SET [Name] = s.[Name], [Uf] = s.[Uf], [NameSearch] = s.[NameSearch]
                WHEN NOT MATCHED THEN INSERT ([IbgeCode], [Name], [Uf], [NameSearch]) VALUES (s.[IbgeCode], s.[Name], s.[Uf], s.[NameSearch]);
                """.Replace("\r\n", "\n", StringComparison.Ordinal);
        }
    }

    // Os valores vêm do arquivo: aspas duplicadas são a defesa que um literal precisa, e o nome nunca é usado como identificador
    private static string Unicode(string value) => "N'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
}
