using System;
using System.IO;

namespace GazetaMarketplace.Web.Tests.Support;

internal static class RepositoryHelper
{
    public static string Root()
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

    public static string Project(string relative) => Path.Combine(Root(), relative.Replace('/', Path.DirectorySeparatorChar));
}
