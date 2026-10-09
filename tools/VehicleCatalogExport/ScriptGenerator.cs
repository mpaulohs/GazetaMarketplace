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
        script.AppendLine("-- Idempotente: pode ser aplicado mais de uma vez. Aplique depois de um backup, com:");
        script.AppendLine("--   sqlcmd -b -I -f 65001 -S <servidor> -d <banco> -U <usuario> -i <este arquivo>");
        script.AppendLine("-- O -f 65001 é obrigatório: sem ele o sqlcmd do Windows lê o arquivo em outra página de código e grava os acentos errados.");
        script.AppendLine("-- Se isso acontecer, a guarda no fim do script aborta e desfaz a carga inteira.");
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

        script.AppendLine(AccentGuard(source));
        script.AppendLine();
        script.AppendLine("COMMIT TRANSACTION;");
        script.AppendLine("GO");
        return script.ToString();
    }

    /// <summary>
    /// Rede de segurança contra o erro que já aconteceu uma vez no site (ImÃ³veis em vez de Imóveis): o <c>sqlcmd</c> do Windows, sem <c>-f 65001</c>, lê o arquivo UTF-8
    /// em outra página de código e grava os bytes de cada acento como letras soltas. Depois da carga, procura "Ã" ou "Â" seguido de um desses restos nas linhas desta origem;
    /// achando, lança erro dentro da transação e <c>XACT_ABORT</c> desfaz tudo. É escrita só em ASCII (<c>NCHAR(n)</c>) para a leitura errada não corromper a própria guarda.
    /// </summary>
    private static string AccentGuard(string source)
    {
        string src = Literal(source);
        string[] tables = ["VehicleBrands", "VehicleModels", "VehicleVersions"];
        string selects = string.Join("\n    UNION ALL ", tables.Select(t => $"SELECT 1 AS Found FROM [{t}] WHERE [Source] = {src} AND [Name] {Exact} LIKE @Corrupted"));

        StringBuilder guard = new();
        guard.AppendLine("-- Guarda de acentos corrompidos (ASCII de proposito): aborta e desfaz tudo se o arquivo foi lido sem '-f 65001'.");
        // Faixa U+0080-U+00BF (Latin-1) e os simbolos que o Windows-1252 poe em 0x80-0x9F, depois de A com til (195) ou com circunflexo (194)
        guard.AppendLine("DECLARE @Corrupted nvarchar(100) = N'%[' + NCHAR(194) + NCHAR(195) + N'][' + NCHAR(128) + N'-' + NCHAR(191)");
        guard.AppendLine("    + NCHAR(338) + NCHAR(339) + NCHAR(352) + NCHAR(353) + NCHAR(376) + NCHAR(381) + NCHAR(382) + NCHAR(402) + NCHAR(710) + NCHAR(732)");
        guard.AppendLine("    + NCHAR(8211) + N'-' + NCHAR(8250) + NCHAR(8364) + NCHAR(8482) + N']%';");
        guard.AppendLine("IF EXISTS (");
        guard.AppendLine("    " + selects);
        guard.AppendLine(")");
        guard.Append("    THROW 50001, N'Acentos corrompidos: o arquivo foi lido sem UTF-8. Aplique com sqlcmd -f 65001. Nada foi gravado.', 1;");
        return guard.ToString();
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
