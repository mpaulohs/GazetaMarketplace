using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Areas.Painel.Controllers;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Conta;

/// <summary>Regras da entrada que não são um cenário do SPEC: log (RC-16), retorno seguro (RC-18) e não revelar contas.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ContaTests
#pragma warning restore CA1515
{
    private const string Email = "ana@exemplo.com.br";
    private const string Senha = "Senha@Forte1";

    private static async Task<WebFactory> NovaFabricaAsync()
    {
        WebFactory factory = new(withDatabase: true);
        await factory.CreateUserAsync(Email, "Ana Souza", Senha, RoleNames.Writer);
        await factory.CreateUserAsync("marcos@exemplo.com.br", "Marcos Silva", Senha, RoleNames.Administrator);
        return factory;
    }

    private static string Alerta(string html) =>
        Regex.Match(html, @"<div class=""alert alert-danger"" role=""alert"">([^<]*)</div>").Groups[1].Value;

    private static bool CriouSessao(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> cookies)
        && cookies.Any(c => c.StartsWith("Gazeta.Equipe=", StringComparison.Ordinal) && !c.Contains("expires=Thu, 01 Jan 1970", StringComparison.Ordinal));

    [TestMethod]
    public async Task FalhaEBloqueioDeLogin_SaoRegistradosNoLog() // RC-16
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        for (int i = 1; i <= 5; i++)
        {
            await client.EntrarAsync(Email, "Senha@Errada9", ip: "10.0.0.7");
        }

        IReadOnlyList<LogEvent> eventos = factory.Logs.Events;
        string all = string.Join("\n", eventos.Select(CollectorSink.AllAsText));

        LogEvent falha = eventos.First(e => CollectorSink.Text(e).Contains("Falha de entrada", StringComparison.Ordinal));
        Assert.AreEqual(LogEventLevel.Warning, falha.Level);
        StringAssert.Contains(CollectorSink.Text(falha), "senha incorreta");
        StringAssert.Contains(all, "a***@exemplo.com.br", "o e-mail aparece mascarado");
        Assert.IsFalse(all.Contains(Email, StringComparison.Ordinal), "o e-mail completo não pode ir para o log");
        Assert.IsFalse(all.Contains("Senha@Errada9", StringComparison.Ordinal), "a senha nunca vai para o log");

        Assert.IsTrue(eventos.Any(e => CollectorSink.Text(e).Contains("Conta do usuário", StringComparison.Ordinal) && CollectorSink.Text(e).Contains("bloqueada", StringComparison.Ordinal)), "bloqueio da conta registrado");
        Assert.IsTrue(eventos.Any(e => CollectorSink.Text(e).Contains("Origem", StringComparison.Ordinal) && CollectorSink.Text(e).Contains("bloqueada por 5 falhas", StringComparison.Ordinal)), "bloqueio da origem registrado");
    }

    [TestMethod]
    public async Task EntradaESaida_SaoRegistradasNoLog_SemSenha() // RC-16
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        await client.EntrarAsync(Email, Senha);
        await client.SairAsync();

        string[] textos = factory.Logs.Events.Select(CollectorSink.Text).ToArray();
        Assert.IsTrue(textos.Any(t => t.StartsWith("Entrada do usuário", StringComparison.Ordinal) && t.Contains("Redator", StringComparison.Ordinal)));
        Assert.IsTrue(textos.Any(t => t.StartsWith("Saída do usuário", StringComparison.Ordinal)));
        Assert.IsFalse(string.Join("\n", factory.Logs.Events.Select(CollectorSink.AllAsText)).Contains(Senha, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task SenhaDigitadaNoCampoDeEmail_NaoVaiParaOLog()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        await client.EntrarAsync("Minha$enhaSecreta9", "qualquer");

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
    public async Task ReturnUrlExterno_E_Ignorado(string retorno) // RC-18
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        HttpResponseMessage entry = await client.EntrarAsync(Email, Senha, retorno);

        Assert.AreEqual(HttpStatusCode.Redirect, entry.StatusCode);
        Assert.AreEqual("/painel/anuncios", entry.Destino(), "vai para a página inicial do papel, não para " + retorno);
    }

    [TestMethod]
    public async Task ReturnUrlLocal_E_Aceito_ParaOAdministradorTambem()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        HttpResponseMessage entry = await client.EntrarAsync("marcos@exemplo.com.br", Senha, "/painel/anuncios?pagina=2");
        Assert.AreEqual("/painel/anuncios?pagina=2", entry.Destino());
    }

    [TestMethod]
    public async Task ContaInexistente_SenhaErrada_Desativada_E_Bloqueada_RespondemIgual()
    {
        using WebFactory factory = await NovaFabricaAsync();
        await factory.CreateUserAsync("inativo@exemplo.com.br", "Inativo", Senha, RoleNames.Writer, active: false);
        await factory.CreateUserAsync("travada@exemplo.com.br", "Travada", Senha, RoleNames.Writer);
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        for (int i = 1; i <= 5; i++)
        {
            await client.EntrarAsync("travada@exemplo.com.br", "Senha@Errada9", ip: $"10.9.0.{i}");
        }

        (string Email, string Senha, string Ip)[] casos =
        [
            ("nao-existe@exemplo.com.br", Senha, "10.8.0.1"),
            (Email, "Senha@Errada9", "10.8.0.2"),
            ("inativo@exemplo.com.br", Senha, "10.8.0.3"),
            ("travada@exemplo.com.br", Senha, "10.8.0.4")
        ];

        foreach ((string email, string password, string ip) in casos)
        {
            HttpResponseMessage response = await client.EntrarAsync(email, password, ip: ip);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, email);
            Assert.AreEqual(ContaController.MensagemDeFalha, Alerta(await response.TextoAsync()), email + ": a mensagem não pode diferenciar os casos");
            Assert.IsFalse(CriouSessao(response), email + " não entra");
        }
    }

    [TestMethod]
    public async Task ContaBloqueadaPeloIdentity_BloqueiaDeVerdade_MesmoComSenhaCerta()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        for (int i = 1; i <= 5; i++)
        {
            await client.EntrarAsync(Email, "Senha@Errada9", ip: $"10.5.0.{i}");
        }

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            UserManager<UsuarioIdentity> users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
            Assert.IsTrue(await users.IsLockedOutAsync(await users.FindByEmailAsync(Email)), "o bloqueio da conta é real");
        }

        HttpResponseMessage response = await client.EntrarAsync(Email, Senha, ip: "10.5.0.99");
        Assert.AreEqual(ContaController.MensagemDeFalha, Alerta(await response.TextoAsync()), "a mensagem não revela o bloqueio da conta");
        Assert.IsFalse(CriouSessao(response));
    }

    [TestMethod]
    public async Task Formulario_SemTokenAntiforgery_E_Recusado()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        using FormUrlEncodedContent formulario = new(new Dictionary<string, string> { ["Email"] = Email, ["Senha"] = Senha });

        HttpResponseMessage response = await client.PostAsync("/painel/entrar", formulario);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.IsFalse(CriouSessao(response));
    }
}
