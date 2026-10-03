using System;
using Microsoft.Data.SqlClient;

namespace VehicleCatalogExport;

/// <summary>Recusa a carga em lote contra um banco de produção. Em produção o caminho é o script, revisado e aplicado à mão (ADR-008).</summary>
internal static class ProductionGuard
{
    private static readonly string[] AllowedEnvironments = ["Development", "Testing"];

    /// <summary>Lança <see cref="InvalidOperationException"/> se o ambiente não for Development ou Testing, ou se a cadeia de conexão estiver marcada como produção.</summary>
    /// <param name="connectionString">Cadeia do banco que receberia a carga.</param>
    /// <param name="environment">Ambiente declarado por quem roda a ferramenta.</param>
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
