using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

namespace VehicleCatalogExport;

/// <summary>
/// Lê o catálogo do banco do GazetaOnline. <b>Só lê:</b> nenhum comando de escrita existe aqui (um teste confere o texto deste arquivo) e a conexão
/// declara intenção de leitura. A conta usada deve ser somente leitura; ela vem de uma variável de ambiente, nunca do repositório.
/// </summary>
/// <remarks>
/// O esquema da origem foi presumido a partir da descoberta (<c>specs/discovery/gazetaonline-fields.md</c>); o banco nunca foi lido. Presunção:
/// <c>{P}Brands(Id, Name)</c>, <c>{P}Models(Id, BrandId, Name)</c>, <c>{P}YearModels(Id, ModelId, Year)</c> e <c>{P}Versions(Id, YearModelId, Name)</c>,
/// com <c>P</c> = <c>Car</c> ou <c>Motorcycle</c>. Quando houver acesso, é aqui (e só aqui) que se ajusta o nome de tabela ou de coluna.
/// </remarks>
internal static class OriginReader
{
    private static readonly string[] Kinds = [Kind.Car, Kind.Moto];

    public static async Task<IReadOnlyList<RawCatalog>> ReadAsync(string connectionString, CancellationToken cancellationToken)
    {
        SqlConnectionStringBuilder builder = new(connectionString)
        {
            ApplicationIntent = ApplicationIntent.ReadOnly,
            ApplicationName = "VehicleCatalogExport"
        };

        await using SqlConnection connection = new(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        List<RawCatalog> catalogs = [];
        foreach (string kind in Kinds)
        {
            catalogs.Add(await ReadKindAsync(connection, kind, cancellationToken));
        }

        return catalogs;
    }

    internal static string TablePrefix(string kind) => kind == Kind.Car ? "Car" : "Motorcycle";

    private static async Task<RawCatalog> ReadKindAsync(DbConnection connection, string kind, CancellationToken cancellationToken)
    {
        string p = TablePrefix(kind);

        // Os nomes de tabela vêm só deste arquivo (nunca de argumento nem de dado), então a concatenação não recebe texto de fora.
        IEnumerable<RawBrand> brands = await connection.QueryAsync<RawBrand>(new CommandDefinition(
            $"SELECT Id, Name FROM {p}Brands", cancellationToken: cancellationToken));
        IEnumerable<RawModel> models = await connection.QueryAsync<RawModel>(new CommandDefinition(
            $"SELECT Id, BrandId, Name FROM {p}Models", cancellationToken: cancellationToken));
        IEnumerable<RawYear> years = await connection.QueryAsync<RawYear>(new CommandDefinition(
            $"SELECT Id, ModelId, Year FROM {p}YearModels", cancellationToken: cancellationToken));

        // Junção com o ano do modelo: a versão não guarda o ano, guarda a linha de ano-modelo. LEFT JOIN mantém as versões sem ano (órfãs)
        // para entrarem no relatório em vez de sumirem em silêncio.
        IEnumerable<RawVersion> versions = await connection.QueryAsync<RawVersion>(new CommandDefinition(
            $"SELECT v.Id, ym.ModelId, ym.Year, v.Name FROM {p}Versions v LEFT JOIN {p}YearModels ym ON ym.Id = v.YearModelId",
            cancellationToken: cancellationToken));

        return new RawCatalog(kind, [.. brands], [.. models], [.. years], [.. versions]);
    }
}
