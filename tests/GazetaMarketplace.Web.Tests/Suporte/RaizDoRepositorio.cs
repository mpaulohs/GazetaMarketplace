using System;
using System.IO;

namespace GazetaMarketplace.Web.Tests.Suporte;

/// <summary>Localiza arquivos do repositório (wwwroot) para os testes que leem o código-fonte estático.</summary>
internal static class RaizDoRepositorio
{
    public static string Caminho(params string[] partes)
    {
        DirectoryInfo pasta = new(AppContext.BaseDirectory);
        while (pasta != null && !File.Exists(Path.Combine(pasta.FullName, "GazetaMarketplace.slnx")))
        {
            pasta = pasta.Parent;
        }

        if (pasta == null)
        {
            throw new InvalidOperationException("GazetaMarketplace.slnx não encontrado acima de " + AppContext.BaseDirectory);
        }

        return Path.Combine([pasta.FullName, .. partes]);
    }

    public static string Wwwroot(params string[] partes) =>
        Path.Combine([Caminho("src", "GazetaMarketplace.Web", "wwwroot"), .. partes]);
}
