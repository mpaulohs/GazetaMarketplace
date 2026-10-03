using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

namespace VehicleCatalogExport;

/// <summary>
/// Carrega o catálogo direto num banco de desenvolvimento ou de teste. Roda exatamente os mesmos <c>MERGE</c> do script gerado, numa transação:
/// não há uma segunda implementação da regra, então a contagem final é a do script por construção.
/// </summary>
internal static class BatchLoader
{
    public static async Task LoadAsync(string connectionString, string environment, CatalogData data, string source, CancellationToken cancellationToken)
    {
        ProductionGuard.EnsureNotProduction(connectionString, environment);

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (string statement in ScriptGenerator.Statements(data, source))
            {
                // Dapper: carga em lote; o EF Core geraria um INSERT por linha
                await connection.ExecuteAsync(new CommandDefinition(statement, transaction: transaction, cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
