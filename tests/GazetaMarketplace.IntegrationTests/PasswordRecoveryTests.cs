using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Recovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// A recuperação de senha (US-007) pelo site inteiro contra o SQL Server real: contagem por janela de tempo nos índices,
/// limpeza em lote e o link de uso único com o carimbo de segurança gravado de verdade.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed partial class PasswordRecoveryTests
#pragma warning restore CA1515
{
    private const string Email = "ana@exemplo.com.br";
    private const string OldPassword = "Senha@Forte1";
    private const string NewPassword = "Nova@Senha2";

    [GeneratedRegex(@"/painel/redefinir-senha\?id=\d+&code=[A-Za-z0-9_\-]+")]
    private static partial Regex LinkPattern();

    private static async Task<string> RequestAsync(IntegrationWebFactory factory, HttpClient browser, string email, RecordingEmailSender mail)
    {
        string page = await browser.GetStringAsync("/painel/esqueci-minha-senha");
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        using FormUrlEncodedContent form = new(new Dictionary<string, string> { ["Email"] = email, ["__RequestVerificationToken"] = token });
        HttpResponseMessage response = await browser.PostAsync("/painel/esqueci-minha-senha", form);
        await factory.Services.GetRequiredService<PasswordRecoveryQueue>().WaitUntilIdleAsync(TimeSpan.FromSeconds(30));
        _ = mail;
        return await response.TextAsync();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US007_PedirUsarOLinkUmaVez_ESegundoUsoRecusado_NoSqlServerReal()
    {
        RecordingEmailSender mail = new();
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync(), s => s.AddSingleton<IEmailSender>(mail));
        await factory.CreateUserAsync(Email, "Ana Souza", OldPassword, RoleNames.Writer);
        using HttpClient browser = factory.CreateBrowser();

        StringAssert.Contains(await RequestAsync(factory, browser, Email, mail), "Se o e-mail estiver cadastrado, enviaremos as instruções");
        string link = LinkPattern().Match(mail.Sent.Single().TextBody).Value;
        Assert.IsFalse(string.IsNullOrEmpty(link), "o e-mail traz o link");

        // O link abre a tela; a nova senha é aceita e leva à entrada com o aviso
        StringAssert.Contains(await (await browser.GetAsync(link)).TextAsync(), "Definir nova senha");
        Uri uri = new("https://localhost" + link);
        System.Collections.Specialized.NameValueCollection query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        HttpResponseMessage save = await browser.PostFormAsync("/painel/esqueci-minha-senha", "/painel/redefinir-senha", new Dictionary<string, string>
        {
            ["Id"] = query["id"],
            ["Code"] = query["code"],
            ["NewPassword"] = NewPassword,
            ["ConfirmPassword"] = NewPassword
        });
        Assert.AreEqual("/painel/entrar?alterada=1", save.Destination());

        // O carimbo de segurança mudou no banco: o mesmo link já foi usado, e só a senha nova entra
        StringAssert.Contains(await (await browser.GetAsync(link)).TextAsync(), "Este link já foi usado");
        using HttpClient owner = factory.CreateBrowser();
        Assert.AreEqual("/painel/anuncios", (await owner.SignInAsync(Email, NewPassword)).Destination());
        using HttpClient old = factory.CreateBrowser();
        StringAssert.Contains(await (await old.SignInAsync(Email, OldPassword)).TextAsync(), "E-mail ou senha inválidos");

        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.AreEqual(1, await db.PasswordRecoveryAttempts.CountAsync());
        Assert.AreEqual(1, await db.AuditEntries.CountAsync(a => a.Action == "user.recover_password" && a.Result == AuditResult.Success));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task LimitePorEmail_ContaPorJanelaDeTempoNoSqlServer()
    {
        RecordingEmailSender mail = new();
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync(), s => s.AddSingleton<IEmailSender>(mail));
        await factory.CreateUserAsync(Email, "Ana Souza", OldPassword, RoleNames.Writer);
        using HttpClient browser = factory.CreateBrowser();

        // Um pedido antigo (fora da janela de 1 hora) não conta; os de agora, sim
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PasswordRecoveryAttempts.Add(new PasswordRecoveryAttempt { Email = Email, Ip = "10.9.9.9", RequestedAt = DateTime.UtcNow.AddMinutes(-90) });
            await db.SaveChangesAsync();
        }

        for (int i = 0; i < 4; i++)
        {
            await RequestAsync(factory, browser, Email, mail);
        }

        Assert.HasCount(3, mail.Sent, "3 por hora; o 4º, não");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task LimpezaDiaria_ApagaEmLoteSoOQueTemMaisDe24Horas_NoSqlServer()
    {
        using IntegrationWebFactory factory = new(await SqlServerFixture.CreateMigratedDatabaseAsync());
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DateTime now = DateTime.UtcNow;
            db.PasswordRecoveryAttempts.AddRange(
                new PasswordRecoveryAttempt { Email = "velha1@x.com", Ip = "1.1.1.1", RequestedAt = now.AddHours(-30) },
                new PasswordRecoveryAttempt { Email = "velha2@x.com", Ip = "1.1.1.1", RequestedAt = now.AddHours(-24).AddMinutes(-1) },
                new PasswordRecoveryAttempt { Email = "recente@x.com", Ip = "1.1.1.1", RequestedAt = now.AddHours(-23) });
            await db.SaveChangesAsync();
        }

        await factory.Services.GetRequiredService<PasswordRecoveryCleanupService>().RunOnceAsync(default);

        using IServiceScope check = factory.Services.CreateScope();
        string[] left = [.. await check.ServiceProvider.GetRequiredService<AppDbContext>().PasswordRecoveryAttempts.Select(a => a.Email).ToListAsync()];
        CollectionAssert.AreEqual(new[] { "recente@x.com" }, left);
    }
}
