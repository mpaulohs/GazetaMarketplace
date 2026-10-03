using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Web.Areas.Panel.Controllers;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Account;

/// <summary>Regras da entrada que não são um cenário do SPEC: log (RC-16), retorno seguro (RC-18) e não revelar contas.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AccountTests
#pragma warning restore CA1515
{
    private const string Email = "ana@exemplo.com.br";
    private const string Password = "Senha@Forte1";

    private static async Task<WebFactory> NewFactoryAsync()
    {
        WebFactory factory = new(withDatabase: true);
        await factory.CreateUserAsync(Email, "Ana Souza", Password, RoleNames.Writer);
        await factory.CreateUserAsync("marcos@exemplo.com.br", "Marcos Silva", Password, RoleNames.Administrator);
        return factory;
    }

    private static string Alert(string html) =>
        Regex.Match(html, @"<div class=""alert alert-danger"" role=""alert"">([^<]*)</div>").Groups[1].Value;

    private static bool CreatedSession(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> cookies)
        && cookies.Any(c => c.StartsWith("Gazeta.Team=", StringComparison.Ordinal) && !c.Contains("expires=Thu, 01 Jan 1970", StringComparison.Ordinal));

    [TestMethod]
    public async Task FalhaEBloqueioDeLogin_SaoRegistradosNoLog() // RC-16
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        for (int i = 1; i <= 5; i++)
        {
            await client.SignInAsync(Email, "Senha@Errada9", ip: "10.0.0.7");
        }

        IReadOnlyList<LogEvent> events = factory.Logs.Events;
        string all = string.Join("\n", events.Select(CollectorSink.AllAsText));

        LogEvent failure = events.First(e => CollectorSink.Text(e).Contains("Falha de entrada", StringComparison.Ordinal));
        Assert.AreEqual(LogEventLevel.Warning, failure.Level);
        StringAssert.Contains(CollectorSink.Text(failure), "senha incorreta");
        StringAssert.Contains(all, "a***@exemplo.com.br", "o e-mail aparece mascarado");
        Assert.IsFalse(all.Contains(Email, StringComparison.Ordinal), "o e-mail completo não pode ir para o log");
        Assert.IsFalse(all.Contains("Senha@Errada9", StringComparison.Ordinal), "a senha nunca vai para o log");

        Assert.IsTrue(events.Any(e => CollectorSink.Text(e).Contains("Conta do usuário", StringComparison.Ordinal) && CollectorSink.Text(e).Contains("bloqueada", StringComparison.Ordinal)), "bloqueio da conta registrado");
        Assert.IsTrue(events.Any(e => CollectorSink.Text(e).Contains("Origem", StringComparison.Ordinal) && CollectorSink.Text(e).Contains("bloqueada por 5 falhas", StringComparison.Ordinal)), "bloqueio da origem registrado");
    }

    [TestMethod]
    public async Task EntradaESaida_SaoRegistradasNoLog_SemSenha() // RC-16
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        await client.SignInAsync(Email, Password);
        await client.SignOutAsync();

        string[] texts = factory.Logs.Events.Select(CollectorSink.Text).ToArray();
        Assert.IsTrue(texts.Any(t => t.StartsWith("Entrada do usuário", StringComparison.Ordinal) && t.Contains("Redator", StringComparison.Ordinal)));
        Assert.IsTrue(texts.Any(t => t.StartsWith("Saída do usuário", StringComparison.Ordinal)));
        Assert.IsFalse(string.Join("\n", factory.Logs.Events.Select(CollectorSink.AllAsText)).Contains(Password, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task SenhaDigitadaNoCampoDeEmail_NaoVaiParaOLog()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        await client.SignInAsync("Minha$enhaSecreta9", "qualquer");

        string all = string.Join("\n", factory.Logs.Events.Select(CollectorSink.AllAsText));
        Assert.IsFalse(all.Contains("Minha$enhaSecreta9", StringComparison.Ordinal));
        StringAssert.Contains(all, "(formato inválido)");
    }

    [TestMethod]
    [DataRow("https://evil.example/painel/anuncios")]
    [DataRow("//evil.example/painel")]
    [DataRow("/\\evil.example")]
    [DataRow("https://localhost/painel/anuncios")]
    [DataRow("javascript:alert(1)")]
    [DataRow("/painel/entrar?ReturnUrl=/painel/entrar")]
    public async Task ReturnUrlExterno_E_Ignorado(string returnUrl) // RC-18
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        HttpResponseMessage entry = await client.SignInAsync(Email, Password, returnUrl);

        Assert.AreEqual(HttpStatusCode.Redirect, entry.StatusCode);
        Assert.AreEqual("/painel/anuncios", entry.Destination(), "vai para a página inicial do papel, não para " + returnUrl);
    }

    [TestMethod]
    public async Task ReturnUrlLocal_E_Aceito_ParaOAdministradorTambem()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        HttpResponseMessage entry = await client.SignInAsync("marcos@exemplo.com.br", Password, "/painel/anuncios?pagina=2");
        Assert.AreEqual("/painel/anuncios?pagina=2", entry.Destination());
    }

    [TestMethod]
    public async Task ContaInexistente_SenhaErrada_Desativada_E_Bloqueada_RespondemIgual()
    {
        using WebFactory factory = await NewFactoryAsync();
        await factory.CreateUserAsync("inativo@exemplo.com.br", "Inativo", Password, RoleNames.Writer, active: false);
        await factory.CreateUserAsync("travada@exemplo.com.br", "Travada", Password, RoleNames.Writer);
        using HttpClient client = TeamClient.Create(factory);
        for (int i = 1; i <= 5; i++)
        {
            await client.SignInAsync("travada@exemplo.com.br", "Senha@Errada9", ip: $"10.9.0.{i}");
        }

        (string Email, string Password, string Ip)[] cases =
        [
            ("nao-existe@exemplo.com.br", Password, "10.8.0.1"),
            (Email, "Senha@Errada9", "10.8.0.2"),
            ("inativo@exemplo.com.br", Password, "10.8.0.3"),
            ("travada@exemplo.com.br", Password, "10.8.0.4")
        ];

        foreach ((string email, string password, string ip) in cases)
        {
            HttpResponseMessage response = await client.SignInAsync(email, password, ip: ip);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, email);
            Assert.AreEqual(AccountController.InvalidCredentialsMessage, Alert(await response.TextAsync()), email + ": a mensagem não pode diferenciar os casos");
            Assert.IsFalse(CreatedSession(response), email + " não entra");
        }
    }

    [TestMethod]
    public async Task ContaBloqueadaPeloIdentity_BloqueiaDeVerdade_MesmoComSenhaCerta()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);
        for (int i = 1; i <= 5; i++)
        {
            await client.SignInAsync(Email, "Senha@Errada9", ip: $"10.5.0.{i}");
        }

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            Assert.IsTrue(await users.IsLockedOutAsync(await users.FindByEmailAsync(Email)), "o bloqueio da conta é real");
        }

        HttpResponseMessage response = await client.SignInAsync(Email, Password, ip: "10.5.0.99");
        Assert.AreEqual(AccountController.InvalidCredentialsMessage, Alert(await response.TextAsync()), "a mensagem não revela o bloqueio da conta");
        Assert.IsFalse(CreatedSession(response));
    }

    [TestMethod]
    public async Task Formulario_SemTokenAntiforgery_E_Recusado()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);
        using FormUrlEncodedContent form = new(new Dictionary<string, string> { ["Email"] = Email, ["Senha"] = Password });

        HttpResponseMessage response = await client.PostAsync("/painel/entrar", form);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.IsFalse(CreatedSession(response));
    }
}
