using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Email;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Infrastructure.Recovery;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Account;

/// <summary>Limites do "Esqueci minha senha" (RC-11, RC-12, RC-13): por e-mail, por IP, total diário, resposta indistinguível e limpeza.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PasswordRecoveryLimitsTests
#pragma warning restore CA1515
{
    private const string Email = "ana@exemplo.com.br";
    private const string Password = "Senha@Forte1";

    private static async Task<RecoveryHarness> NewHarnessAsync()
    {
        RecoveryHarness harness = new();
        await harness.Factory.CreateUserAsync(Email, "Ana Souza", Password, RoleNames.Writer);
        return harness;
    }

    [TestMethod]
    public async Task QuartoPedidoNaMesmaHora_NaoEnviaEmail_MasRespondeIgual()
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        string[] bodies = new string[4];

        // IPs diferentes: só o limite por e-mail está em jogo
        for (int i = 0; i < 4; i++)
        {
            HttpResponseMessage response = await harness.RequestAsync(Email, ip: "10.0.1." + (i + 1));
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            bodies[i] = RecoveryHarness.Normalize(await response.TextAsync());
            await harness.WaitForSendingAsync();
        }

        Assert.HasCount(3, harness.Mail.Sent, "o 4º pedido não envia nada");
        Assert.AreEqual(bodies[0], bodies[3], "a resposta do pedido barrado é idêntica à do aceito");
        Assert.AreEqual(4, harness.AttemptCount(), "mas o pedido barrado também é registrado");
        Assert.IsTrue(harness.Factory.Logs.Events.Any(e => e.Level == LogEventLevel.Warning && CollectorSink.Text(e).Contains("acima do limite", StringComparison.Ordinal)),
            "o log registra o pedido barrado");
    }

    [TestMethod]
    public async Task DepoisDeUmaHora_OLimitePorEmailLibera()
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        for (int i = 0; i < 4; i++)
        {
            await harness.RequestAsync(Email, ip: "10.0.2." + (i + 1));
            await harness.WaitForSendingAsync();
        }

        Assert.HasCount(3, harness.Mail.Sent);

        harness.Factory.Clock.Now = harness.Factory.Clock.Now.AddMinutes(61);
        await harness.RequestAsync(Email, ip: "10.0.2.9");
        await harness.WaitForSendingAsync();

        Assert.HasCount(4, harness.Mail.Sent, "passada a hora, o pedido volta a enviar");
    }

    [TestMethod]
    public async Task OLimiteDeUmEmail_NaoAfetaOutroEmail()
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        await harness.Factory.CreateUserAsync("bia@exemplo.com.br", "Bia Lima", Password, RoleNames.Writer);
        for (int i = 0; i < 4; i++)
        {
            await harness.RequestAsync(Email, ip: "10.0.3." + (i + 1));
        }

        await harness.RequestAsync("bia@exemplo.com.br", ip: "10.0.3.9");
        await harness.WaitForSendingAsync();

        Assert.AreEqual(1, harness.Mail.Sent.Count(m => m.To == "bia@exemplo.com.br"));
    }

    [TestMethod]
    public async Task DecimoPrimeiroPedidoDoMesmoIp_NaoEnvia_ComEmailsDiferentes()
    {
        using RecoveryHarness harness = new();
        for (int i = 0; i < 11; i++)
        {
            await harness.Factory.CreateUserAsync($"pessoa{i}@exemplo.com.br", "Pessoa " + i, Password, RoleNames.Writer);
        }

        for (int i = 0; i < 11; i++)
        {
            HttpResponseMessage response = await harness.RequestAsync($"pessoa{i}@exemplo.com.br", ip: "192.168.0.50");
            StringAssert.Contains(await response.TextAsync(), RecoveryHarness.Neutral);
            await harness.WaitForSendingAsync();
        }

        Assert.HasCount(10, harness.Mail.Sent, "10 por hora por IP; o 11º é barrado");
        Assert.IsFalse(harness.Mail.Sent.Any(m => m.To == "pessoa10@exemplo.com.br"));
    }

    [TestMethod]
    public async Task PedidosDeEmailInexistente_ContamNoLimiteDoIp()
    {
        // Decisão C: quem tenta e-mails que não existem gasta o mesmo limite, então a resposta não revela nada
        using RecoveryHarness harness = await NewHarnessAsync();
        for (int i = 0; i < 10; i++)
        {
            await harness.RequestAsync($"fantasma{i}@exemplo.com.br", ip: "192.168.0.60");
        }

        await harness.RequestAsync(Email, ip: "192.168.0.60");
        await harness.WaitForSendingAsync();

        Assert.IsEmpty(harness.Mail.Sent, "o IP já estourou o limite com e-mails que não existem");
    }

    [TestMethod]
    public async Task TotalDiarioChegaA80_RegistraWarning()
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        DateTime now = harness.Factory.Clock.Now.UtcDateTime;

        // 78 anteriores + este pedido = 79: ainda sem aviso
        harness.SeedAttempts(78, now.AddHours(-3));
        await harness.RequestAsync(Email, ip: "10.0.4.1");
        Assert.IsFalse(DailyWarnings(harness).Any(), "79 pedidos em 24 horas ainda não avisam");

        // o próximo é o 80º
        await harness.RequestAsync(Email, ip: "10.0.4.2");
        LogEvent warning = DailyWarnings(harness).Single();
        Assert.AreEqual(LogEventLevel.Warning, warning.Level);
        StringAssert.Contains(CollectorSink.Text(warning), "80");
    }

    [TestMethod]
    public async Task PedidosComMaisDe24Horas_NaoContamNoTotalDiario()
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        harness.SeedAttempts(100, harness.Factory.Clock.Now.UtcDateTime.AddHours(-25));

        await harness.RequestAsync(Email, ip: "10.0.4.3");

        Assert.IsFalse(DailyWarnings(harness).Any());
    }

    [TestMethod]
    public async Task LimpezaDiaria_ApagaOQueTemMaisDe24Horas_ESoIsso()
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        DateTime now = harness.Factory.Clock.Now.UtcDateTime;
        harness.SeedAttempts(3, now.AddHours(-25), "velha");
        harness.SeedAttempts(2, now.AddHours(-23), "recente");

        await harness.Factory.Services.GetRequiredService<PasswordRecoveryCleanupService>().RunOnceAsync(default);

        using IServiceScope scope = harness.Factory.Services.CreateScope();
        string[] left = [.. scope.ServiceProvider.GetRequiredService<AppDbContext>().PasswordRecoveryAttempts.Select(a => a.Email).OrderBy(e => e)];
        CollectionAssertAreEqual(["recente0@exemplo.com.br", "recente1@exemplo.com.br"], left);
    }

    [TestMethod]
    public async Task ContaExistenteEInexistente_TemMesmaRespostaESemEsperarOEnvio()
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Mail.Gate = gate; // o "SendGrid" fica preso: se a resposta esperasse o envio, este pedido travaria

        HttpResponseMessage existing = await harness.RequestAsync(Email, ip: "10.0.5.1").WaitAsync(TimeSpan.FromSeconds(10));
        HttpResponseMessage missing = await harness.RequestAsync("naoexiste@exemplo.com.br", ip: "10.0.5.2").WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreEqual(existing.StatusCode, missing.StatusCode);
        Assert.AreEqual(RecoveryHarness.Normalize(await existing.TextAsync()), RecoveryHarness.Normalize(await missing.TextAsync()));
        CollectionAssertSameHeaders(existing, missing);
        Assert.IsEmpty(harness.Mail.Sent, "o envio ainda está preso quando a resposta já voltou");

        gate.SetResult();
        await harness.WaitForSendingAsync();
        Assert.HasCount(1, harness.Mail.Sent);
    }

    [TestMethod]
    public async Task FalhaNoEnvio_NaoMudaARespostaNemDerrubaOSite_ELogaComTraceId()
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        harness.Mail.FailWith = new EmailDeliveryException("O SendGrid recusou o envio (HTTP 500).");

        HttpResponseMessage failing = await harness.RequestAsync(Email, ip: "10.0.6.1");
        await harness.WaitForSendingAsync();

        StringAssert.Contains(await failing.TextAsync(), RecoveryHarness.Neutral);
        LogEvent error = harness.Factory.Logs.Events.Single(e => e.Level == LogEventLevel.Error);
        StringAssert.Contains(CollectorSink.AllAsText(error), failing.Headers.GetValues("X-Correlation-ID").Single());

        // O worker continua vivo: o pedido seguinte, sem a falha, envia
        harness.Mail.FailWith = null;
        await harness.RequestAsync(Email, ip: "10.0.6.2");
        await harness.WaitForSendingAsync();
        Assert.HasCount(1, harness.Mail.Sent);
    }

    [TestMethod]
    public async Task RedefinirComSucesso_LimpaOBloqueio() // RC-12
    {
        using RecoveryHarness harness = await NewHarnessAsync();

        // Bloqueia a conta com 5 senhas erradas
        for (int i = 1; i <= 5; i++)
        {
            using HttpClient attacker = TeamClient.Create(harness.Factory);
            await attacker.SignInAsync(Email, "Errada@123", ip: "10.9.0." + i);
        }

        using (IServiceScope scope = harness.Factory.Services.CreateScope())
        {
            UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            AppUser locked = await users.FindByEmailAsync(Email);
            Assert.IsTrue(await users.IsLockedOutAsync(locked), "a conta está bloqueada");
        }

        await harness.RequestAsync(Email, ip: "10.0.7.1");
        await harness.WaitForSendingAsync();
        HttpResponseMessage save = await harness.SetNewPasswordAsync(harness.LastLink(), "Nova@Senha2");
        Assert.AreEqual(HttpStatusCode.Redirect, save.StatusCode);

        using (IServiceScope scope = harness.Factory.Services.CreateScope())
        {
            UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            AppUser user = await users.FindByEmailAsync(Email);
            Assert.IsFalse(await users.IsLockedOutAsync(user), "o bloqueio foi desfeito");
            Assert.AreEqual(0, user.AccessFailedCount, "o contador de falhas zerou");
        }

        using HttpClient owner = TeamClient.Create(harness.Factory);
        Assert.AreEqual("/painel/anuncios", (await owner.SignInAsync(Email, "Nova@Senha2", ip: "10.9.1.1")).Destination());
    }

    [TestMethod]
    public async Task SenhaProvisoria_RedefinidaPeloLink_DeixaDeExigirATroca()
    {
        using RecoveryHarness harness = new();
        await harness.Factory.CreateUserAsync(Email, "Ana Souza", Password, RoleNames.Writer, mustChangePassword: true);

        await harness.RequestAsync(Email);
        await harness.WaitForSendingAsync();
        await harness.SetNewPasswordAsync(harness.LastLink(), "Nova@Senha2");

        Assert.IsFalse((await harness.Factory.ListUsersAsync()).Single().MustChangePassword);
    }

    private static System.Collections.Generic.IEnumerable<LogEvent> DailyWarnings(RecoveryHarness harness) =>
        harness.Factory.Logs.Events.Where(e => e.Level == LogEventLevel.Warning && CollectorSink.Text(e).Contains("nas últimas 24 horas", StringComparison.Ordinal));

    private static void CollectionAssertAreEqual(string[] expected, string[] actual) =>
        Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert.AreEqual(expected, actual);

    private static void CollectionAssertSameHeaders(HttpResponseMessage a, HttpResponseMessage b)
    {
        string[] Names(HttpResponseMessage r) => [.. r.Headers.Select(h => h.Key).Concat(r.Content.Headers.Select(h => h.Key)).Where(n => n is not ("Date" or "X-Correlation-ID" or "Content-Length")).OrderBy(n => n)];
        Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert.AreEqual(Names(a), Names(b), "mesmos cabeçalhos");
    }
}
