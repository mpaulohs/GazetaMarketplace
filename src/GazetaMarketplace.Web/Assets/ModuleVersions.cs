using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.FileProviders;

namespace GazetaMarketplace.Web.Assets;

/// <summary>A versão (<c>?v=</c>) de um módulo JavaScript de página.</summary>
public interface IModuleVersions
{
    /// <summary>A versão do módulo e de tudo que ele importa, direta ou indiretamente. Muda quando qualquer um dos arquivos muda.</summary>
    /// <param name="path">O endereço do arquivo no site, a partir da raiz (<c>/js/pages/search.js</c>).</param>
    string For(string path);
}

/// <summary>
/// A versão de um módulo de página é o hash do <b>conjunto</b> formado por ele e pelos módulos que ele importa (os <c>import</c> relativos, seguidos até o fim). O <c>asp-append-version</c> olha só o arquivo da página:
/// com ele, mudar só um módulo compartilhado (<c>api.js</c>, <c>favorites-ui.js</c>) não mudava o endereço da página, e o navegador, com cache de um ano no módulo da página, juntava a página antiga com o módulo novo.
/// Os módulos importados saem sem <c>?v=</c> (o <c>import</c> não leva versão) e por isso continuam revalidando a cada visita; o endereço da página é o que muda e carrega tudo de novo.
/// </summary>
public sealed partial class ModuleVersions(IFileProvider files, bool cache) : IModuleVersions
{
    // import ... from "./x.js"  |  export ... from "../x.js"  |  import "./x.js"  |  import("./x.js")
    [GeneratedRegex("""(?:\b(?:import|export)\s*(?:[^'"();]*?\sfrom\s*)?|\bimport\s*\(\s*)["'](?<path>\.{1,2}/[^"'?#]+)""")]
    private static partial Regex ImportPattern();

    private readonly ConcurrentDictionary<string, string> _versions = new(StringComparer.Ordinal);

    public string For(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        string normalized = Normalize(path);
        if (!cache)
        {
            return Compute(normalized);
        }

        return _versions.GetOrAdd(normalized, Compute);
    }

    private string Compute(string path)
    {
        SortedDictionary<string, byte[]> closure = new(StringComparer.Ordinal);
        Collect(path, closure);

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach ((string file, byte[] content) in closure)
        {
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(file + "\n"));
            hash.AppendData(content);
            hash.AppendData([0]);
        }

        return WebEncoders.Base64UrlEncode(hash.GetHashAndReset(), 0, 12);
    }

    private void Collect(string path, SortedDictionary<string, byte[]> closure)
    {
        if (closure.ContainsKey(path))
        {
            return;
        }

        IFileInfo file = files.GetFileInfo(path);
        if (!file.Exists || file.IsDirectory)
        {
            // Importação para um arquivo que não existe: o navegador vai falhar de qualquer jeito; o hash só registra que ele não está lá
            closure[path] = [];
            return;
        }

        using Stream stream = file.CreateReadStream();
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        byte[] content = buffer.ToArray();
        closure[path] = content;

        string text = System.Text.Encoding.UTF8.GetString(content);
        foreach (Match match in ImportPattern().Matches(text))
        {
            Collect(Resolve(path, match.Groups["path"].Value), closure);
        }
    }

    private static string Normalize(string path)
    {
        string trimmed = path.StartsWith("~/", StringComparison.Ordinal) ? path[1..] : path;
        return trimmed.StartsWith('/') ? trimmed : "/" + trimmed;
    }

    private static string Resolve(string from, string relative)
    {
        List<string> parts = [.. from.Split('/', StringSplitOptions.RemoveEmptyEntries)];
        parts.RemoveAt(parts.Count - 1);
        foreach (string segment in relative.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == "..")
            {
                if (parts.Count > 0)
                {
                    parts.RemoveAt(parts.Count - 1);
                }
            }
            else if (segment != ".")
            {
                parts.Add(segment);
            }
        }

        return "/" + string.Join('/', parts);
    }
}
