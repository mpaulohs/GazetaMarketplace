using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CitiesImport;

/// <summary>Carrega os municípios direto num banco de desenvolvimento ou de teste, com os mesmos <c>MERGE</c> do script gerado, numa transação.</summary>
internal static class BatchLoader
{
    public static async Task LoadAsync(string connectionString, string environment, IReadOnlyList<CityRow> cities, CancellationToken cancellationToken)
    {
        ProductionGuard.EnsureNotProduction(connectionString, environment);

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (string statement in ScriptGenerator.Statements(cities))
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
