using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace VehicleCatalogExport;

/// <summary>
/// A linha de comando, separada do <c>Main</c> para os testes rodarem a ferramenta sem processo novo.
/// Códigos de saída: 0 feito · 2 uso errado ou variável de conexão ausente · 3 origem inacessível · 4 carga recusada.
/// Em qualquer falha nenhum arquivo é criado e nenhuma linha é gravada.
/// </summary>
internal static partial class Cli
{
    public const string OriginVariable = "VEHICLE_CATALOG_ORIGIN_CONNECTION";
    public const string TargetVariable = "VEHICLE_CATALOG_TARGET_CONNECTION";

    private const string Usage = """
        Uso:
          VehicleCatalogExport export --source <rótulo> --out <arquivo.sql> [--report <arquivo.txt>]
          VehicleCatalogExport load   --source <rótulo> --environment <Development|Testing> [--report <arquivo.txt>]

        Variáveis de ambiente:
          VEHICLE_CATALOG_ORIGIN_CONNECTION  banco do GazetaOnline (conta SOMENTE LEITURA)
          VEHICLE_CATALOG_TARGET_CONNECTION  só para 'load': banco de desenvolvimento ou de teste que recebe a carga
        """;

    public static async Task<int> RunAsync(string[] args, Func<string, string> environment, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (args.Length == 0 || args[0] is not ("export" or "load"))
        {
            await error.WriteLineAsync(Usage);
            return 2;
        }

        bool load = args[0] == "load";
        Dictionary<string, string> options = Parse(args.Skip(1).ToArray());
        string source = options.GetValueOrDefault("--source");
        string outFile = options.GetValueOrDefault("--out");
        string env = options.GetValueOrDefault("--environment");
        // O rótulo vai para a coluna Source (varchar(60)): só caracteres simples, sem aspas
        if (source is null || !SourceLabel().IsMatch(source) || (load ? string.IsNullOrWhiteSpace(env) : string.IsNullOrWhiteSpace(outFile)))
        {
            await error.WriteLineAsync(Usage);
            return 2;
        }

        string origin = environment(OriginVariable);
        string target = environment(TargetVariable);
        if (string.IsNullOrWhiteSpace(origin) || (load && string.IsNullOrWhiteSpace(target)))
        {
            await error.WriteLineAsync($"Variável de conexão ausente ({OriginVariable}{(load ? " e " + TargetVariable : string.Empty)}). Nada foi gerado.");
            return 2;
        }

        // Recusa a carga antes de tocar na origem: um banco de produção não deve nem ver a conexão ser aberta
        if (load)
        {
            try
            {
                ProductionGuard.EnsureNotProduction(target, env);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                await error.WriteLineAsync(ex.Message);
                return 4;
            }
        }

        IReadOnlyList<RawCatalog> raw;
        try
        {
            raw = await OriginReader.ReadAsync(origin, cancellationToken);
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException or ArgumentException or TimeoutException)
        {
            await error.WriteLineAsync($"Não foi possível ler a origem ({ex.GetType().Name}). Nada foi gerado.");
            return 3;
        }

        ValidationResult result = CatalogValidator.Validate(raw);
        string report = CatalogValidator.Report(result);
        await output.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
            $"Marcas: {result.Data.Brands.Count} | Modelos: {result.Data.Models.Count} | Anos: {result.Data.Years.Count} | Versões: {result.Data.Versions.Count} | Descartados: {result.Orphans.Count}"));
        if (options.GetValueOrDefault("--report") is { Length: > 0 } reportFile)
        {
            WriteAtomically(reportFile, report);
        }
        else if (result.Orphans.Count > 0)
        {
            await output.WriteAsync(report);
        }

        if (load)
        {
            try
            {
                await BatchLoader.LoadAsync(target, env, result.Data, source, cancellationToken);
            }
            catch (SqlException ex)
            {
                await error.WriteLineAsync($"A carga falhou e foi desfeita ({ex.Message}).");
                return 3;
            }

            await output.WriteLineAsync("Carga concluída.");
        }
        else
        {
            WriteAtomically(outFile, ScriptGenerator.Script(result.Data, source));
            await output.WriteLineAsync($"Script gerado: {outFile}");
        }

        return 0;
    }

    [GeneratedRegex(@"^[A-Za-z0-9._-]{1,60}$")]
    private static partial Regex SourceLabel();

    private static Dictionary<string, string> Parse(string[] args)
    {
        Dictionary<string, string> options = new(StringComparer.Ordinal);
        for (int i = 0; i + 1 < args.Length; i += 2)
        {
            options[args[i]] = args[i + 1];
        }

        return options;
    }

    // Escreve num arquivo temporário e troca no fim: uma falha no meio nunca deixa um script pela metade
    private static void WriteAtomically(string path, string content)
    {
        string full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        string temp = full + ".tmp";
        File.WriteAllText(temp, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        File.Move(temp, full, overwrite: true);
    }
}
