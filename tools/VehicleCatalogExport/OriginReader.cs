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
/// Esquema do GazetaOnline, como está nas entidades dele: <c>{P}Brands({P}BrandId, Name)</c>, <c>{P}Models({P}ModelId, {P}BrandId, Name)</c>,
/// <c>{P}YearModels({P}YearModelId, {P}ModelId, Year)</c> e <c>{P}Versions({P}VersionId, {P}YearModelId, Name)</c>, com <c>P</c> = <c>Car</c> ou <c>Motorcycle</c>.
/// O <c>Year</c> é texto (nvarchar); o que não for número vira nulo (<c>TRY_CAST</c>) e o validador relata.
/// <b>Não há filtro por <c>IsPublished</c>:</b> no GazetaOnline esse flag só diz que algum anúncio publicado usa o item (é recalculado a cada anúncio), não que
/// o item foi escolhido. Filtrar por ele traria só os carros que já têm anúncio. O catálogo inteiro é lido; <c>LegacyCode</c> também não é lido.
/// Se o esquema da origem mudar, é aqui (e só aqui) que se ajusta o nome de tabela ou de coluna.
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

        // Os nomes de tabela e de coluna vêm só deste arquivo (nunca de argumento nem de dado), então a concatenação não recebe texto de fora.
        IEnumerable<RawBrand> brands = await connection.QueryAsync<RawBrand>(new CommandDefinition(
            $"SELECT {p}BrandId AS Id, Name FROM {p}Brands", cancellationToken: cancellationToken));
        IEnumerable<RawModel> models = await connection.QueryAsync<RawModel>(new CommandDefinition(
            $"SELECT {p}ModelId AS Id, {p}BrandId AS BrandId, Name FROM {p}Models", cancellationToken: cancellationToken));
        IEnumerable<RawYear> years = await connection.QueryAsync<RawYear>(new CommandDefinition(
            $"SELECT {p}YearModelId AS Id, {p}ModelId AS ModelId, TRY_CAST(Year AS int) AS Year FROM {p}YearModels", cancellationToken: cancellationToken));

        // Junção com o ano do modelo: a versão não guarda o ano, guarda a linha de ano-modelo. LEFT JOIN mantém as versões sem ano
        // para entrarem no relatório em vez de sumirem em silêncio.
        IEnumerable<RawVersion> versions = await connection.QueryAsync<RawVersion>(new CommandDefinition(
            $"SELECT v.{p}VersionId AS Id, ym.{p}ModelId AS ModelId, TRY_CAST(ym.Year AS int) AS Year, v.Name FROM {p}Versions v LEFT JOIN {p}YearModels ym ON ym.{p}YearModelId = v.{p}YearModelId",
            cancellationToken: cancellationToken));

        return new RawCatalog(kind, [.. brands], [.. models], [.. years], [.. versions]);
    }
}
