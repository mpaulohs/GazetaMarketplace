using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace VehicleCatalogExport;

/// <summary>
/// Gera o script de carga do catálogo (ADR-008): um <c>MERGE</c> por tabela e por lote de até 500 linhas, na ordem marcas → modelos → anos → versões,
/// tudo numa transação. Rodar duas vezes deixa o banco igual (idempotente). O texto é determinístico (nada de data nem de ordem do dicionário):
/// o mesmo catálogo gera sempre o mesmo arquivo, e o diff no git mostra só o que mudou de verdade.
/// </summary>
internal static class ScriptGenerator
{
    public const int BatchSize = 500;

    // BIN2: a comparação diferencia maiúscula e acento, para "honda" virar "Honda" numa nova carga
    private const string Exact = "COLLATE Latin1_General_100_BIN2";

    public static string Script(CatalogData data, string source)
    {
        ArgumentNullException.ThrowIfNull(data);
        StringBuilder script = new();
        script.AppendLine("-- Catálogo de veículos (ADR-008). GERADO por tools/VehicleCatalogExport: não edite à mão; gere de novo.");
        script.AppendLine(CultureInfo.InvariantCulture, $"-- Origem (Source): {source}");
        script.AppendLine(CultureInfo.InvariantCulture, $"-- Marcas: {data.Brands.Count} | Modelos: {data.Models.Count} | Anos: {data.Years.Count} | Versões: {data.Versions.Count}");
        script.AppendLine("-- Idempotente: pode ser aplicado mais de uma vez. Aplique com 'sqlcmd -I' ou na ferramenta de SQL do provedor.");
        script.AppendLine("SET QUOTED_IDENTIFIER ON;");
        script.AppendLine("GO");
        script.AppendLine("SET XACT_ABORT ON;");
        script.AppendLine("BEGIN TRANSACTION;");
        script.AppendLine();
        foreach (string statement in Statements(data, source))
        {
            script.AppendLine(statement);
            script.AppendLine();
        }

        script.AppendLine("COMMIT TRANSACTION;");
        script.AppendLine("GO");
        return script.ToString();
    }

    /// <summary>Os comandos <c>MERGE</c>, um por lote, na ordem em que precisam rodar (cada nível depende do anterior).</summary>
    public static IEnumerable<string> Statements(CatalogData data, string source)
    {
        ArgumentNullException.ThrowIfNull(data);
        string src = Literal(source);

        foreach (BrandRow[] batch in data.Brands.OrderBy(b => b.Kind, StringComparer.Ordinal).ThenBy(b => b.Id).Chunk(BatchSize))
        {
            yield return Merge(
                "VehicleBrands", ["Id", "Kind", "Name"], ["Id", "Kind"], batch.Select(b => $"({b.Id}, {Literal(b.Kind)}, {Unicode(b.Name)})"), src,
                changed: $"t.[Name] {Exact} <> s.[Name] {Exact}");
        }

        foreach (ModelRow[] batch in data.Models.OrderBy(m => m.Kind, StringComparer.Ordinal).ThenBy(m => m.Id).Chunk(BatchSize))
        {
            yield return Merge(
                "VehicleModels", ["Id", "Kind", "BrandId", "Name"], ["Id", "Kind"], batch.Select(m => $"({m.Id}, {Literal(m.Kind)}, {m.BrandId}, {Unicode(m.Name)})"), src,
                changed: $"t.[BrandId] <> s.[BrandId] OR t.[Name] {Exact} <> s.[Name] {Exact}");
        }

        foreach (YearRow[] batch in data.Years.OrderBy(y => y.Kind, StringComparer.Ordinal).ThenBy(y => y.ModelId).ThenBy(y => y.Year).Chunk(BatchSize))
        {
            yield return Merge(
                "VehicleModelYears", ["ModelId", "Year", "Kind"], ["ModelId", "Year", "Kind"], batch.Select(y => $"({y.ModelId}, {y.Year}, {Literal(y.Kind)})"), src,
                changed: null);
        }

        foreach (VersionRow[] batch in data.Versions.OrderBy(v => v.Kind, StringComparer.Ordinal).ThenBy(v => v.Id).Chunk(BatchSize))
        {
            yield return Merge(
                "VehicleVersions", ["Id", "Kind", "ModelId", "Year", "Name"], ["Id", "Kind"],
                batch.Select(v => $"({v.Id}, {Literal(v.Kind)}, {v.ModelId}, {v.Year}, {Unicode(v.Name)})"), src,
                changed: $"t.[ModelId] <> s.[ModelId] OR t.[Year] <> s.[Year] OR t.[Name] {Exact} <> s.[Name] {Exact}");
        }
    }

    private static string Merge(string table, string[] columns, string[] keys, IEnumerable<string> rows, string source, string changed)
    {
        string list = string.Join(", ", columns.Select(c => $"[{c}]"));
        string on = string.Join(" AND ", keys.Select(k => $"t.[{k}] = s.[{k}]"));
        string insertValues = string.Join(", ", columns.Select(c => $"s.[{c}]"));
        string update = $"t.[Source] <> {source}" + (changed is null ? string.Empty : $" OR {changed}");
        string assign = string.Join(", ", columns.Except(keys).Select(c => $"[{c}] = s.[{c}]").Append($"[Source] = {source}"));

        StringBuilder sql = new();
        sql.AppendLine(CultureInfo.InvariantCulture, $"MERGE [{table}] AS t");
        sql.AppendLine("USING (VALUES");
        sql.AppendLine("    " + string.Join(",\n    ", rows));
        sql.AppendLine(CultureInfo.InvariantCulture, $") AS s ({list})");
        sql.AppendLine(CultureInfo.InvariantCulture, $"ON {on}");
        sql.AppendLine(CultureInfo.InvariantCulture, $"WHEN MATCHED AND ({update}) THEN UPDATE SET {assign}");
        sql.Append(CultureInfo.InvariantCulture, $"WHEN NOT MATCHED THEN INSERT ({list}, [Source]) VALUES ({insertValues}, {source});");
        return sql.ToString();
    }

    // Os valores vêm da origem: aspas duplicadas são a única defesa que um literal precisa, e o nome nunca é usado como identificador.
    private static string Literal(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    private static string Unicode(string value) => "N" + Literal(value);
}
