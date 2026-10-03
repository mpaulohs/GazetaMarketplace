using System;
using System.IO;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Localiza arquivos do repositório (wwwroot) para os testes que leem o código-fonte estático.</summary>
internal static class RepositoryRoot
{
    public static string FullPath(params string[] parts)
    {
        DirectoryInfo folder = new(AppContext.BaseDirectory);
        while (folder != null && !File.Exists(Path.Combine(folder.FullName, "GazetaMarketplace.slnx")))
        {
            folder = folder.Parent;
        }

        if (folder == null)
        {
            throw new InvalidOperationException("GazetaMarketplace.slnx não encontrado acima de " + AppContext.BaseDirectory);
        }

        return Path.Combine([folder.FullName, .. parts]);
    }

    public static string Wwwroot(params string[] parts) =>
        Path.Combine([FullPath("src", "GazetaMarketplace.Web", "wwwroot"), .. parts]);
}
