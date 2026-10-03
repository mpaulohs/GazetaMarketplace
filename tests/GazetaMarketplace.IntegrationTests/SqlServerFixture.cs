using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testcontainers.MsSql;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// Um SQL Server 2022 real (Testcontainers, porta sorteada) para a suíte inteira: subir o contêiner leva dezenas de segundos,
/// então ele é compartilhado e cada teste cria o próprio banco, com nome único. Nenhum teste toca em infraestrutura existente
/// (rules/testing.md, contrato de isolamento).
/// </summary>
[TestClass]
public static class SqlServerFixture
{
    private static MsSqlContainer _container;

    [AssemblyInitialize]
    public static async Task StartAsync(TestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await _container.StartAsync(context.CancellationToken);
    }

    [AssemblyCleanup]
    public static async Task StopAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>Cria um banco vazio com nome único e devolve a cadeia de conexão para ele.</summary>
    public static async Task<string> CreateEmptyDatabaseAsync()
    {
        string name = "gazeta_" + Guid.NewGuid().ToString("N");
        await using SqlConnection master = new(_container.GetConnectionString());
        await master.OpenAsync();
        await using SqlCommand create = master.CreateCommand();
        create.CommandText = $"CREATE DATABASE [{name}]";
        await create.ExecuteNonQueryAsync();

        return new SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = name }.ConnectionString;
    }

    /// <summary>Banco criado pelas migrations do EF, como faz quem aplica <c>dotnet ef database update</c>.</summary>
    public static async Task<string> CreateMigratedDatabaseAsync()
    {
        string connectionString = await CreateEmptyDatabaseAsync();
        await using AppDbContext context = NewContext(connectionString);
        await context.Database.MigrateAsync();
        return connectionString;
    }

    public static AppDbContext NewContext(string connectionString) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options, new FakeCurrentUser(), TimeProvider.System);

    /// <summary>Aplica o script idempotente do repositório (<c>db/scripts/gazeta-idempotente.sql</c>) lote a lote, como o sqlcmd.</summary>
    /// <param name="connectionString">Banco que recebe o script.</param>
    /// <param name="quotedIdentifierOff">Começa a sessão com <c>QUOTED_IDENTIFIER OFF</c>, como o sqlcmd sem <c>-I</c>: o script tem de ligar por conta própria.</param>
    public static async Task ApplyScriptAsync(string connectionString, bool quotedIdentifierOff = false)
    {
        string script = await File.ReadAllTextAsync(RepositoryPath("db", "scripts", "gazeta-idempotente.sql"));
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        if (quotedIdentifierOff)
        {
            await using SqlCommand off = connection.CreateCommand();
            off.CommandText = "SET QUOTED_IDENTIFIER OFF";
            await off.ExecuteNonQueryAsync();
        }

        foreach (string batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline).Where(b => !string.IsNullOrWhiteSpace(b)))
        {
            await using SqlCommand command = connection.CreateCommand();
            command.CommandText = batch;
            await command.ExecuteNonQueryAsync();
        }
    }

    public static string RepositoryPath(params string[] parts)
    {
        DirectoryInfo folder = new(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "GazetaMarketplace.slnx")))
        {
            folder = folder.Parent;
        }

        return folder is null
            ? throw new InvalidOperationException("GazetaMarketplace.slnx não encontrado acima de " + AppContext.BaseDirectory)
            : Path.Combine([folder.FullName, .. parts]);
    }
}
