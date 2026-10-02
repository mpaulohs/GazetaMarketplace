using System;
using System.IO;

namespace GazetaMarketplace.Web.Tests.Suporte;

internal static class RepositorioHelper
{
    public static string Raiz()
    {
        DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "GazetaMarketplace.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("GazetaMarketplace.slnx não encontrado acima de " + AppContext.BaseDirectory);
        }

        return dir.FullName;
    }

    public static string Projeto(string relativo) => Path.Combine(Raiz(), relativo.Replace('/', Path.DirectorySeparatorChar));
}
