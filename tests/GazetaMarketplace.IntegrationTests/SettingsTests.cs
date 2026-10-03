using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// Configurações do site no SQL Server real (US-015): a tabela e o índice único da chave, o fluxo completo pela tela (PRG + cache + auditoria na
/// mesma transação) e duas pessoas criando a primeira configuração ao mesmo tempo.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SettingsTests
#pragma warning restore CA1515
{
    private const string AdminEmail = "marcos@exemplo.com.br";
    private const string Password = "Senha@Forte1";

    private static async Task<HttpClient> SignedInAdminAsync(IntegrationWebFactory factory)
    {
        await factory.CreateUserAsync(AdminEmail, "Marcos Silva", Password, RoleNames.Administrator);
        HttpClient client = factory.CreateBrowser();
        string page = await client.GetStringAsync("/painel/entrar");
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        using FormUrlEncodedContent form = new(new Dictionary<string, string> { ["Email"] = AdminEmail, ["Password"] = Password, ["__RequestVerificationToken"] = token });
        HttpResponseMessage entry = await client.PostAsync("/painel/entrar", form);
        Assert.AreEqual(HttpStatusCode.Redirect, entry.StatusCode, "a conta de teste precisa entrar");
        return client;
    }

    private static async Task<HttpResponseMessage> SaveAsync(HttpClient client, string phone)
    {
        string page = await client.GetStringAsync("/painel/configuracoes");
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        using FormUrlEncodedContent form = new(new Dictionary<string, string> { ["Phone"] = phone, ["__RequestVerificationToken"] = token });
        return await client.PostAsync("/painel/configuracoes", form);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Tabela_TemChaveUnica_ColunasDoTamanhoCerto_ENasceVazia()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        Assert.AreEqual(0, await context.SiteSettings.CountAsync(), "o telefone nasce vazio");

        context.SiteSettings.Add(new SiteSetting { Key = SiteSettingKeys.Phone, Value = "11912345678" });
        await context.SaveChangesAsync();
        context.SiteSettings.Add(new SiteSetting { Key = SiteSettingKeys.Phone, Value = "21987654321" });

        DbUpdateException error = await Assert.ThrowsExactlyAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.IsInstanceOfType<SqlException>(error.InnerException);
        Assert.AreEqual(2601, ((SqlException)error.InnerException).Number, "o índice único UQ_SiteSettings_Key recusa a segunda linha da mesma chave");

        await using SqlConnection raw = new(connection);
        await raw.OpenAsync();
        await using SqlCommand command = raw.CreateCommand();
        command.CommandText = "SELECT COLUMN_NAME + ':' + DATA_TYPE + ':' + CAST(CHARACTER_MAXIMUM_LENGTH AS varchar(10)) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SiteSettings' AND COLUMN_NAME IN ('Key','Value')";
        List<string> columns = [];
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }

        CollectionAssert.AreEquivalent(new[] { "Key:varchar:100", "Value:nvarchar:500" }, columns);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task FluxoCompleto_SalvarTrocarEAudit_NoSqlServer()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());
        using HttpClient admin = await SignedInAdminAsync(factory);
        ISiteSettings settings = factory.Services.GetRequiredService<ISiteSettings>();

        Assert.AreEqual(HttpStatusCode.Redirect, (await SaveAsync(admin, "(11) 91234-5678")).StatusCode);
        Assert.AreEqual("11912345678", await settings.GetPhoneAsync(CancellationToken.None));
        Assert.AreEqual(HttpStatusCode.Redirect, (await SaveAsync(admin, "+55 21 98765-4321")).StatusCode);
        Assert.AreEqual("21987654321", await settings.GetPhoneAsync(CancellationToken.None), "o cache foi invalidado ao salvar");
        Assert.AreEqual(HttpStatusCode.OK, (await SaveAsync(admin, "abc123")).StatusCode);
        Assert.AreEqual("21987654321", await settings.GetPhoneAsync(CancellationToken.None));

        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.AreEqual(1, await context.SiteSettings.CountAsync(), "uma linha só para a chave, atualizada");
        List<AuditEntry> audit = await context.AuditEntries.AsNoTracking().Where(e => e.TargetType == "SiteSetting").OrderBy(e => e.Id).ToListAsync();
        Assert.HasCount(2, audit);
        Assert.IsNull(audit[0].PreviousValue);
        Assert.AreEqual("11912345678", audit[1].PreviousValue);
        Assert.AreEqual("21987654321", audit[1].NewValue);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DuasPessoasCriandoAPrimeiraConfiguracaoJuntas_NuncaDuplicaALinha_ENemPerdeAuditoria()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());

        for (int round = 1; round <= 8; round++)
        {
            using (IServiceScope clean = factory.Services.CreateScope())
            {
                AppDbContext context = clean.ServiceProvider.GetRequiredService<AppDbContext>();
                await context.SiteSettings.ExecuteDeleteAsync();
                await context.Database.ExecuteSqlRawAsync("DELETE FROM AuditEntries WHERE TargetType = 'SiteSetting'");
                clean.ServiceProvider.GetRequiredService<ISiteSettings>().Invalidate();
            }

            using Barrier start = new(2);
            SettingsSaveResult[] results = await Task.WhenAll(
                Task.Run(() => SaveFromScopeAsync(factory, start, "(11) 91234-5678")),
                Task.Run(() => SaveFromScopeAsync(factory, start, "(21) 98765-4321")));

            // Dois resultados são corretos: uma salva e a outra troca depois (as duas têm sucesso, em sequência), ou as duas leem "vazio",
            // as duas tentam criar e o índice único recusa uma, que é avisada do conflito. Nunca duas linhas, nunca um erro solto.
            Assert.IsGreaterThanOrEqualTo(1, results.Count(r => r.Succeeded), $"rodada {round}: ao menos uma salva");
            Assert.IsTrue(results.Where(r => !r.Succeeded).All(r => r.Error == SiteSettingsMessages.Conflict), $"rodada {round}: quem não salva é avisado do conflito");

            using IServiceScope check = factory.Services.CreateScope();
            AppDbContext db = check.ServiceProvider.GetRequiredService<AppDbContext>();
            SiteSetting[] rows = await db.SiteSettings.AsNoTracking().ToArrayAsync();
            Assert.HasCount(1, rows, $"rodada {round}: sobra uma linha");
            AuditEntry[] audit = await db.AuditEntries.AsNoTracking().Where(e => e.TargetType == "SiteSetting").OrderBy(e => e.Id).ToArrayAsync();
            Assert.HasCount(results.Count(r => r.Succeeded), audit, $"rodada {round}: uma entrada de auditoria por gravação que deu certo");
            Assert.AreEqual(rows[0].Value, audit[^1].NewValue, $"rodada {round}: a última auditoria é o valor guardado");
        }
    }

    private static async Task<SettingsSaveResult> SaveFromScopeAsync(IntegrationWebFactory factory, Barrier start, string phone)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        ISiteSettingsManagement management = scope.ServiceProvider.GetRequiredService<ISiteSettingsManagement>();
        start.SignalAndWait();
        return await management.SavePhoneAsync(phone, CancellationToken.None);
    }
}
