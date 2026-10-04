using System;
using Microsoft.Data.SqlClient;

namespace CitiesImport;

/// <summary>Recusa a carga em lote contra um banco de produção. Em produção o caminho é o script, revisado e aplicado à mão.</summary>
internal static class ProductionGuard
{
    private static readonly string[] AllowedEnvironments = ["Development", "Testing"];

    public static void EnsureNotProduction(string connectionString, string environment)
    {
        if (Array.IndexOf(AllowedEnvironments, environment) < 0)
        {
            throw new InvalidOperationException($"A carga em lote só roda em Development ou Testing (recebido: '{environment}'). Em produção, aplique o script gerado.");
        }

        SqlConnectionStringBuilder builder = new(connectionString);
        foreach (string part in new[] { builder.DataSource, builder.InitialCatalog, builder.ApplicationName })
        {
            if (part.Contains("prod", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("A cadeia de conexão está marcada como produção ('prod' no servidor, no banco ou no nome da aplicação): a carga em lote foi recusada.");
            }
        }
    }
}
