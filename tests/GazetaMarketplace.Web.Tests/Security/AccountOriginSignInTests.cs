using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Infrastructure.Recovery;
using GazetaMarketplace.Web.Areas.Panel.Controllers;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

/// <summary>SC-03 pela rota real: 5 senhas erradas de qualquer IP não travam a conta; atraso progressivo e aviso por e-mail ao dono.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AccountOriginSignInTests
#pragma warning restore CA1515
{
    private const string Admin = "adm@exemplo.com.br";
    private const string Password = "Senha@Forte1";
    private const string Wrong = "Senha@Errada9";

    private sealed class RecordingDelay : ILoginDelay
    {
        public List<TimeSpan> Waits { get; } = [];

        public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            lock (Waits)
            {
                Waits.Add(delay);
            }

            return Task.CompletedTask;
        }
    }

    private static async Task<(WebFactory Factory, SpyEmailSender Mail, RecordingDelay Delay)> NewAsync()
    {
        SpyEmailSender mail = new();
        RecordingDelay delay = new();
        WebFactory factory = new(
            withDatabase: true,
            configuration: new Dictionary<string, string> { ["RateLimiting:LoginFailuresPerOrigin"] = "500" }, // o contador por IP não entra na conta
            services: services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(mail);
                services.RemoveAll<ILoginDelay>();
                services.AddSingleton<ILoginDelay>(delay);
            });
        await factory.CreateUserAsync(Admin, "Administrador", Password, RoleNames.Administrator);
        return (factory, mail, delay);
    }

    private static async Task Fail(HttpClient client, string ip, int times = 5)
    {
        for (int i = 0; i < times; i++)
        {
            await client.SignInAsync(Admin, Wrong, ip: ip);
        }
    }

    private static async Task SendingDoneAsync(WebFactory factory) =>
        await factory.Services.GetRequiredService<PasswordRecoveryQueue>().WaitUntilIdleAsync(TimeSpan.FromSeconds(15));

    [TestMethod]
    public async Task CincoSenhasErradasDeUmIp_NaoImpedemOAdministradorDeEntrarDeOutroIp() // SC-03
    {
        (WebFactory factory, _, _) = await NewAsync();
        using (factory)
        {
            using HttpClient attacker = TeamClient.Create(factory);
            await Fail(attacker, "198.51.100.1");

            // Do IP do atacante nem a senha certa entra, e a resposta é a de qualquer falha (não revela o bloqueio)
            HttpResponseMessage fromAttackerIp = await attacker.SignInAsync(Admin, Password, ip: "198.51.100.1");
            Assert.AreEqual(HttpStatusCode.OK, fromAttackerIp.StatusCode);
            StringAssert.Contains(await fromAttackerIp.TextAsync(), AccountController.InvalidCredentialsMessage);

            // Da rede do Administrador entra normalmente
            using HttpClient owner = TeamClient.Create(factory);
            HttpResponseMessage fromOwnerIp = await owner.SignInAsync(Admin, Password, ip: "203.0.113.7");
            Assert.AreEqual(HttpStatusCode.Redirect, fromOwnerIp.StatusCode);
            Assert.AreNotEqual("/painel/entrar", fromOwnerIp.Destination(), "foi para a página inicial do papel, não voltou à entrada");

            // Passada a janela, o IP do atacante volta a poder tentar
            await owner.SignOutAsync();
            factory.Clock.Now += TimeSpan.FromMinutes(16);
            Assert.AreEqual(HttpStatusCode.Redirect, (await attacker.SignInAsync(Admin, Password, ip: "198.51.100.1")).StatusCode);
        }
    }

    [TestMethod]
    public async Task OrigemBloqueadaNaConta_ContinuaTentandoOutraConta() // o limite por IP é do LoginFailureCounter, não deste bloqueio
    {
        (WebFactory factory, _, _) = await NewAsync();
        using (factory)
        {
            await factory.CreateUserAsync("outra@exemplo.com.br", "Outra", Password, RoleNames.Writer);
            using HttpClient client = TeamClient.Create(factory);
            await Fail(client, "198.51.100.1");

            HttpResponseMessage other = await client.SignInAsync("outra@exemplo.com.br", Password, ip: "198.51.100.1");

            Assert.AreEqual(HttpStatusCode.Redirect, other.StatusCode);
        }
    }

    [TestMethod]
    public async Task AposTresBloqueiosSeguidos_CadaTentativaEsperaAtrasoProgressivo() // SC-03: 1s, 2s, 4s...
    {
        (WebFactory factory, _, RecordingDelay delay) = await NewAsync();
        using (factory)
        {
            using HttpClient client = TeamClient.Create(factory);
            await Fail(client, "198.51.100.1");
            await Fail(client, "198.51.100.2");
            Assert.IsEmpty(delay.Waits, "até o 2.º bloqueio ninguém espera");

            await Fail(client, "198.51.100.3"); // 3.º bloqueio
            await client.SignInAsync(Admin, Wrong, ip: "198.51.100.4");
            CollectionAssert.AreEqual(new[] { TimeSpan.FromSeconds(1) }, delay.Waits, "a tentativa depois do 3.º bloqueio espera 1 s");

            await Fail(client, "198.51.100.5"); // cada tentativa espera 1 s; a 5.ª fecha o 4.º bloqueio
            await client.SignInAsync(Admin, Wrong, ip: "198.51.100.6");
            Assert.AreEqual(TimeSpan.FromSeconds(2), delay.Waits[^1], "depois do 4.º bloqueio a espera sobe para 2 s");

            // Entrar com sucesso zera a sequência
            using HttpClient owner = TeamClient.Create(factory);
            await owner.SignInAsync(Admin, Password, ip: "203.0.113.7");
            int before = delay.Waits.Count;
            await client.SignInAsync(Admin, Wrong, ip: "198.51.100.9");
            Assert.AreEqual(before, delay.Waits.Count, "a sequência recomeçou");
        }
    }

    [TestMethod]
    public async Task SegundoBloqueioSeguido_AvisaODonoPorEmail_UmaVezPorHora() // SC-03
    {
        (WebFactory factory, SpyEmailSender mail, _) = await NewAsync();
        using (factory)
        {
            using HttpClient client = TeamClient.Create(factory);
            await Fail(client, "198.51.100.1");
            await SendingDoneAsync(factory);
            Assert.IsEmpty(mail.Sent, "o 1.º bloqueio não avisa");

            await Fail(client, "198.51.100.2");
            await SendingDoneAsync(factory);
            Assert.HasCount(1, mail.Sent, "o 2.º bloqueio avisa o dono");
            EmailMessage notice = mail.Sent[0];
            Assert.AreEqual(Admin, notice.To);
            Assert.AreEqual(PasswordRecoveryMessages.LockoutNoticeSubject, notice.Subject);
            Assert.DoesNotContain("code=", notice.TextBody, "sem link com credencial");
            StringAssert.Contains(notice.TextBody, "/painel/esqueci-minha-senha");

            await Fail(client, "198.51.100.3");
            await SendingDoneAsync(factory);
            Assert.HasCount(1, mail.Sent, "no máximo um aviso por hora");

            factory.Clock.Now += TimeSpan.FromMinutes(61);
            await Fail(client, "198.51.100.4");
            await Fail(client, "198.51.100.5");
            await SendingDoneAsync(factory);
            Assert.IsGreaterThanOrEqualTo(1, mail.Sent.Count);
        }
    }

    [TestMethod]
    public async Task ContaDesativada_NaoAcumulaBloqueio() // a falha de conta desativada não conta
    {
        (WebFactory factory, _, _) = await NewAsync();
        using (factory)
        {
            await factory.CreateUserAsync("inativo@exemplo.com.br", "Inativo", Password, RoleNames.Writer, active: false);
            using HttpClient client = TeamClient.Create(factory);
            for (int i = 0; i < 8; i++)
            {
                await client.SignInAsync("inativo@exemplo.com.br", Password, ip: "198.51.100.1");
            }

            AccountOriginLockout lockout = factory.Services.GetRequiredService<AccountOriginLockout>();
            Assert.IsFalse(lockout.IsBlocked(1, "198.51.100.1"));
            Assert.IsTrue(factory.Logs.Events.All(e => !CollectorSink.Text(e).Contains("bloqueada por tentativas", StringComparison.Ordinal)));
        }
    }
}
