using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace CitiesImport;

/// <summary>
/// A linha de comando, separada do <c>Main</c> para os testes rodarem a ferramenta sem processo novo.
/// Códigos de saída: 0 feito · 2 uso errado, arquivo ilegível ou variável de conexão ausente · 3 banco inacessível ou carga desfeita · 4 dados recusados
/// (arquivo com problemas) ou carga em ambiente proibido. Em qualquer falha nenhum arquivo é criado e nenhuma linha é gravada.
/// </summary>
internal static partial class Cli
{
    public const string TargetVariable = "CITIES_IMPORT_TARGET_CONNECTION";

    private const string Usage = """
        Uso:
          CitiesImport export --input <municipios.json> --source <rótulo> --out <arquivo.sql> [--sample]
          CitiesImport load   --input <municipios.json> --source <rótulo> --environment <Development|Testing>

        <municipios.json> é o arquivo oficial do IBGE: https://servicodados.ibge.gov.br/api/v1/localidades/municipios
        --sample marca o script como amostra de teste (não vai para produção).

        Variável de ambiente (só para 'load'):
          CITIES_IMPORT_TARGET_CONNECTION  banco de desenvolvimento ou de teste que recebe a carga
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
        string input = options.GetValueOrDefault("--input");
        string source = options.GetValueOrDefault("--source");
        string outFile = options.GetValueOrDefault("--out");
        string env = options.GetValueOrDefault("--environment");
        if (string.IsNullOrWhiteSpace(input) || source is null || !SourceLabel().IsMatch(source) || (load ? string.IsNullOrWhiteSpace(env) : string.IsNullOrWhiteSpace(outFile)))
        {
            await error.WriteLineAsync(Usage);
            return 2;
        }

        string target = environment(TargetVariable);
        if (load && string.IsNullOrWhiteSpace(target))
        {
            await error.WriteLineAsync($"Variável de conexão ausente ({TargetVariable}). Nada foi carregado.");
            return 2;
        }

        // Recusa a carga antes de ler qualquer coisa: um banco de produção não deve nem ver a conexão ser aberta
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

        IReadOnlyList<RawCity> raw;
        try
        {
            raw = IbgeReader.Read(await File.ReadAllTextAsync(input, cancellationToken));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            await error.WriteLineAsync($"Não foi possível ler o arquivo do IBGE: {ex.Message} Nada foi gerado.");
            return 2;
        }

        ValidationResult result = CityValidator.Validate(raw);
        if (!result.IsValid)
        {
            await error.WriteAsync(CityValidator.Report(result));
            return 4;
        }

        await output.WriteLineAsync(string.Create(CultureInfo.InvariantCulture, $"Municípios: {result.Cities.Count}"));
        if (load)
        {
            try
            {
                await BatchLoader.LoadAsync(target, env, result.Cities, cancellationToken);
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
            WriteAtomically(outFile, ScriptGenerator.Script(result.Cities, source, options.ContainsKey("--sample")));
            await output.WriteLineAsync($"Script gerado: {outFile}");
        }

        return 0;
    }

    [GeneratedRegex(@"^[A-Za-z0-9._-]{1,60}$")]
    private static partial Regex SourceLabel();

    private static Dictionary<string, string> Parse(string[] args)
    {
        Dictionary<string, string> options = new(StringComparer.Ordinal);
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--sample")
            {
                options["--sample"] = "true";
            }
            else if (i + 1 < args.Length)
            {
                options[args[i]] = args[++i];
            }
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
