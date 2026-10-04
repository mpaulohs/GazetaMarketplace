using System;
using System.IO;
using System.Linq;

namespace CitiesImport.Tests;

internal static class Repo
{
    public static string Path(params string[] parts)
    {
        DirectoryInfo folder = new(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(System.IO.Path.Combine(folder.FullName, "GazetaMarketplace.slnx")))
        {
            folder = folder.Parent;
        }

        return folder is null
            ? throw new InvalidOperationException("GazetaMarketplace.slnx não encontrado acima de " + AppContext.BaseDirectory)
            : System.IO.Path.Combine([folder.FullName, .. parts]);
    }
}

/// <summary>Monta o JSON do IBGE à mão, nos dois formatos de UF que o arquivo oficial tem.</summary>
internal static class Ibge
{
    public static string City(object id, string name, string uf, bool immediateRegion = false)
    {
        string head = "{\"id\":" + Json(id) + ",\"nome\":" + Quote(name) + ",";
        string state = "\"UF\":{\"sigla\":" + Quote(uf) + "}";
        return immediateRegion
            ? head + "\"microrregiao\":null,\"regiao-imediata\":{\"regiao-intermediaria\":{" + state + "}}}"
            : head + "\"microrregiao\":{\"mesorregiao\":{" + state + "}}}";
    }

    public static string List(params string[] cities) => "[" + string.Join(",", cities) + "]";

    private static string Json(object id) => id is string text ? Quote(text) : Convert.ToString(id, System.Globalization.CultureInfo.InvariantCulture);

    private static string Quote(string text) => System.Text.Json.JsonSerializer.Serialize(text);

    public static string TempFile(string name) => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ci-" + Guid.NewGuid().ToString("N"), name);
}
