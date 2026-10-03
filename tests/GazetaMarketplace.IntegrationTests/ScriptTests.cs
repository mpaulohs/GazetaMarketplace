using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// O script idempotente (<c>db/scripts/gazeta-idempotente.sql</c>) é o que vai para o servidor de hospedagem (ADR-004):
/// precisa rodar duas vezes sem erro e dar o mesmo esquema que as migrations do EF. No SQLite dos testes de unidade nada disso é provado.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ScriptTests
#pragma warning restore CA1515
{
    [TestMethod]
    [TestCategory("Integration")]
    public async Task ScriptIdempotente_AplicadoDuasVezes_NaoFalhaENaoDuplicaNada()
    {
        string connectionString = await SqlServerFixture.CreateEmptyDatabaseAsync();

        await SqlServerFixture.ApplyScriptAsync(connectionString);
        List<string> afterFirst = await QueryAsync(connectionString, "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId");
        string[] schemaAfterFirst = [.. await DescribeSchemaAsync(connectionString)];

        await SqlServerFixture.ApplyScriptAsync(connectionString);

        CollectionAssert.AreEqual(afterFirst, await QueryAsync(connectionString, "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId"));
        CollectionAssert.AreEqual(schemaAfterFirst, (await DescribeSchemaAsync(connectionString)).ToArray());
        Assert.AreEqual(10, afterFirst.Count, "as dez migrations do projeto");
        Assert.AreEqual(2, (await QueryAsync(connectionString, "SELECT Name FROM AspNetRoles")).Count, "os dois papéis não são duplicados");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ScriptIdempotente_ComSessaoQuotedIdentifierOff_LigaPorContaPropria_ENaoFalhaNosIndicesFiltrados()
    {
        // O sqlcmd sem -I abre a sessão com QUOTED_IDENTIFIER OFF e os índices filtrados falham com o erro 1934
        string connectionString = await SqlServerFixture.CreateEmptyDatabaseAsync();

        await SqlServerFixture.ApplyScriptAsync(connectionString, quotedIdentifierOff: true);

        Assert.AreEqual(10, (await QueryAsync(connectionString, "SELECT MigrationId FROM __EFMigrationsHistory")).Count);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task MigrationsDoEf_EScript_DaoOMesmoEsquema_ENosMesmosDados()
    {
        string byMigrations = await SqlServerFixture.CreateMigratedDatabaseAsync();
        string byScript = await SqlServerFixture.CreateEmptyDatabaseAsync();
        await SqlServerFixture.ApplyScriptAsync(byScript);

        CollectionAssert.AreEqual((await DescribeSchemaAsync(byMigrations)).ToArray(), (await DescribeSchemaAsync(byScript)).ToArray());
        Assert.IsTrue((await DescribeSchemaAsync(byMigrations)).Count > 30, "o esquema comparado não está vazio");

        const string roles = "SELECT CAST(Id AS nvarchar(10)) + '|' + Name + '|' + NormalizedName FROM AspNetRoles ORDER BY Id";
        CollectionAssert.AreEqual(await QueryAsync(byMigrations, roles), await QueryAsync(byScript, roles));
        // A carga inicial de categorias: as mesmas 147 linhas, com os mesmos slugs, pelos dois caminhos
        const string categories = "SELECT CAST(Id AS nvarchar(10)) + '|' + COALESCE(CAST(ParentId AS nvarchar(10)), '-') + '|' + Name + '|' + Slug + '|' + CAST(DisplayOrder AS nvarchar(10)) + '|' + CAST(IsPostable AS nvarchar(1)) + '|' + CAST(IsSystem AS nvarchar(1)) + '|' + COALESCE(FieldGroup, '-') FROM Categories ORDER BY Id";
        List<string> byMigrationCategories = await QueryAsync(byMigrations, categories);
        Assert.HasCount(147, byMigrationCategories);
        CollectionAssert.AreEqual(byMigrationCategories, await QueryAsync(byScript, categories));
        const string history = "SELECT MigrationId + '|' + ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId";
        CollectionAssert.AreEqual(await QueryAsync(byMigrations, history), await QueryAsync(byScript, history));
    }

    /// <summary>Colunas, índices (com colunas, unicidade e filtro) e chaves estrangeiras, em texto ordenado para comparar.</summary>
    internal static async Task<List<string>> DescribeSchemaAsync(string connectionString)
    {
        List<string> lines = [];
        lines.AddRange((await QueryAsync(connectionString, """
            SELECT 'COL ' + CAST(TABLE_NAME AS nvarchar(200)) COLLATE DATABASE_DEFAULT + '.' + CAST(COLUMN_NAME AS nvarchar(200)) COLLATE DATABASE_DEFAULT + ' ' + CAST(DATA_TYPE AS nvarchar(100)) COLLATE DATABASE_DEFAULT
                 + COALESCE('(' + CAST(CHARACTER_MAXIMUM_LENGTH AS varchar(10)) + ')', '')
                 + ' ' + CAST(IS_NULLABLE AS nvarchar(10)) COLLATE DATABASE_DEFAULT + ' ' + COALESCE(CAST(COLUMN_DEFAULT AS nvarchar(max)) COLLATE DATABASE_DEFAULT, '-')
            FROM INFORMATION_SCHEMA.COLUMNS
            """)).Select(x => x));
        lines.AddRange(await QueryAsync(connectionString, """
            SELECT 'IDX ' + CAST(t.name AS nvarchar(400)) COLLATE DATABASE_DEFAULT + '.' + CAST(i.name AS nvarchar(400)) COLLATE DATABASE_DEFAULT + ' unique=' + CAST(i.is_unique AS varchar(1)) + ' pk=' + CAST(i.is_primary_key AS varchar(1))
                 + ' filter=' + COALESCE(CAST(i.filter_definition AS nvarchar(400)) COLLATE DATABASE_DEFAULT, '-') + ' cols=' + 
                   (SELECT STRING_AGG(CAST(c.name AS nvarchar(400)) COLLATE DATABASE_DEFAULT, ',') WITHIN GROUP (ORDER BY ic.key_ordinal)
                    FROM sys.index_columns ic JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                    WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id)
            FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
            WHERE i.name IS NOT NULL
            """));
        lines.AddRange(await QueryAsync(connectionString, """
            SELECT 'FK ' + CAST(fk.name AS nvarchar(400)) COLLATE DATABASE_DEFAULT + ' ' + CAST(OBJECT_NAME(fk.parent_object_id) AS nvarchar(400)) COLLATE DATABASE_DEFAULT + '->' + CAST(OBJECT_NAME(fk.referenced_object_id) AS nvarchar(400)) COLLATE DATABASE_DEFAULT
                 + ' delete=' + CAST(fk.delete_referential_action_desc AS nvarchar(400)) COLLATE DATABASE_DEFAULT
            FROM sys.foreign_keys fk
            """));
        lines.AddRange(await QueryAsync(connectionString, """
            SELECT 'CK ' + CAST(name AS nvarchar(400)) COLLATE DATABASE_DEFAULT + ' ' + CAST(OBJECT_NAME(parent_object_id) AS nvarchar(400)) COLLATE DATABASE_DEFAULT + ' ' + CAST(definition AS nvarchar(max)) COLLATE DATABASE_DEFAULT
            FROM sys.check_constraints
            """));
        lines.Sort(StringComparer.Ordinal);
        return lines;
    }

    internal static async Task<List<string>> QueryAsync(string connectionString, string sql)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        List<string> rows = [];
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(Convert.ToString(reader.GetValue(0), System.Globalization.CultureInfo.InvariantCulture));
        }

        return rows;
    }
}
