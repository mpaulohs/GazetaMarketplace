using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>Atalhos dos testes de anúncios: usuário, anúncio pelo EF e SQL direto (para provar o que o banco recusa, sem passar pelo C#).</summary>
internal static class AdData
{
    private static int _users;

    public static async Task<int> AddUserAsync(string connectionString)
    {
        int n = System.Threading.Interlocked.Increment(ref _users);
        await using AppDbContext context = SqlServerFixture.NewContext(connectionString);
        AppUser user = new() { UserName = $"u{n}@exemplo.com.br", NormalizedUserName = $"U{n}@EXEMPLO.COM.BR", Email = $"u{n}@exemplo.com.br", FullName = $"Pessoa {n}" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>Grava um rascunho pelo EF (com os métodos do anúncio, como o código de produção) e devolve o id.</summary>
    public static async Task<int> AddDraftAsync(string connectionString, int authorId, int? categoryId = null, string attributesJson = null, string title = "Honda Civic 2018")
    {
        Ad ad = Ad.CreateDraft(title, authorId);
        ad.SetCategory(categoryId);
        if (attributesJson is not null)
        {
            Assert(AdAttributes.TryParse(attributesJson, out AdAttributes attributes), "JSON de teste inválido");
            ad.SetAttributes(attributes);
        }

        await using AppDbContext context = SqlServerFixture.NewContext(connectionString);
        context.Ads.Add(ad);
        await context.SaveChangesAsync();
        return ad.Id;
    }

    public static async Task<Ad> LoadAsync(string connectionString, int id)
    {
        await using AppDbContext context = SqlServerFixture.NewContext(connectionString);
        return await context.Ads.AsNoTracking().SingleAsync(a => a.Id == id);
    }

    /// <summary>Executa SQL e devolve o número do erro do SQL Server (0 se deu certo).</summary>
    public static async Task<int> TryExecuteAsync(string connectionString, string sql, params (string Name, object Value)[] parameters)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        foreach ((string name, object value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        try
        {
            await command.ExecuteNonQueryAsync();
            return 0;
        }
        catch (SqlException error)
        {
            return error.Number;
        }
    }

    public static async Task<List<string>> QueryAsync(string connectionString, string sql) => await ScriptTests.QueryAsync(connectionString, sql);

    /// <summary>INSERT mínimo de um anúncio, com as colunas dadas sobrescrevendo os valores válidos de partida.</summary>
    public static Task<int> InsertRawAsync(string connectionString, int authorId, params (string Column, object Value)[] overrides)
    {
        Dictionary<string, object> values = new()
        {
            ["Status"] = (byte)1,
            ["Title"] = "Título",
            ["Attributes"] = "{}",
            ["LocationManual"] = false,
            ["AuthorId"] = authorId,
            ["CreatedAt"] = DateTime.UtcNow
        };
        foreach ((string column, object value) in overrides)
        {
            values[column] = value;
        }

        string columns = string.Join(", ", values.Keys.Select(c => $"[{c}]"));
        string markers = string.Join(", ", values.Keys.Select((_, i) => $"@p{i}"));
        (string, object)[] parameters = [.. values.Values.Select((v, i) => ($"@p{i}", v))];
        return TryExecuteAsync(connectionString, $"INSERT INTO Ads ({columns}) VALUES ({markers})", parameters);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
