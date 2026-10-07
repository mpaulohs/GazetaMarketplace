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

    // O limite por IP (LoginFailureCounter) alto: assim só o bloqueio por conta e origem entra na conta
    private static async Task<WebFactory> NewPerAccountFactoryAsync()
    {
        WebFactory factory = new(withDatabase: true, configuration: new Dictionary<string, string> { ["RateLimiting:LoginFailuresPerOrigin"] = "500" });
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
    public async Task EntradaNoLog_UsaAsPropriedadesEmIngles_UserIdERole() // regra de idioma: propriedades de log em inglês
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        await client.SignInAsync(Email, Password);

        var entry = factory.Logs.Events.First(e => CollectorSink.Text(e).StartsWith("Entrada do usuário", StringComparison.Ordinal));
        Assert.IsTrue(entry.Properties.ContainsKey("UserId"));
        Assert.IsTrue(entry.Properties.ContainsKey("Role"));
        Assert.IsFalse(entry.Properties.ContainsKey("UsuarioId"));
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
    [DataRow("Senha@123")]
    [DataRow("Joao@Gazeta1")]
    [DataRow("a@b")]
    [DataRow("Segredo@x.com9")]
    [DataRow("@@@@")]
    public async Task SenhaComArrobaDigitadaNoCampoDeEmail_NaoVaiParaOLog(string typed) // SC-06
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        await client.SignInAsync(typed, "qualquer");

        string all = string.Join("\n", factory.Logs.Events.Select(CollectorSink.AllAsText));
        Assert.IsFalse(all.Contains(typed, StringComparison.Ordinal), "o texto digitado não pode estar no log: " + typed);
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
    public async Task ContaInexistente_SenhaErrada_Desativada_E_BloqueadaParaAOrigem_RespondemIgual()
    {
        using WebFactory factory = await NewPerAccountFactoryAsync();
        await factory.CreateUserAsync("inativo@exemplo.com.br", "Inativo", Password, RoleNames.Writer, active: false);
        await factory.CreateUserAsync("travada@exemplo.com.br", "Travada", Password, RoleNames.Writer);
        using HttpClient client = TeamClient.Create(factory);
        for (int i = 1; i <= 5; i++)
        {
            await client.SignInAsync("travada@exemplo.com.br", "Senha@Errada9", ip: "10.8.0.4"); // bloqueia aquele IP naquela conta (SC-03)
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
    public async Task ContaBloqueadaParaAOrigem_BloqueiaDeVerdade_MesmoComSenhaCerta_SemTravarAContaParaOsOutros() // SC-03
    {
        using WebFactory factory = await NewPerAccountFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);
        for (int i = 1; i <= 5; i++)
        {
            await client.SignInAsync(Email, "Senha@Errada9", ip: "10.5.0.1");
        }

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            AppUser account = await users.FindByEmailAsync(Email);
            Assert.IsTrue(factory.Services.GetRequiredService<AccountOriginLockout>().IsBlocked(account.Id, "10.5.0.1"), "o bloqueio daquele IP naquela conta é real");
            Assert.IsFalse(await users.IsLockedOutAsync(account), "a conta em si não fica travada para quem vem de outro IP");
        }

        HttpResponseMessage blocked = await client.SignInAsync(Email, Password, ip: "10.5.0.1");
        Assert.AreEqual(AccountController.InvalidCredentialsMessage, Alert(await blocked.TextAsync()), "a mensagem não revela o bloqueio");
        Assert.IsFalse(CreatedSession(blocked));

        HttpResponseMessage elsewhere = await client.SignInAsync(Email, Password, ip: "10.5.0.99");
        Assert.AreEqual(HttpStatusCode.Redirect, elsewhere.StatusCode, "de outro IP a conta entra");
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
