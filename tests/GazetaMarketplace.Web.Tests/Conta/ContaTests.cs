using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Equipe;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Areas.Painel.Controllers;
using GazetaMarketplace.Web.Tests.Suporte;
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

    private static async Task<FabricaWeb> NovaFabricaAsync()
    {
        FabricaWeb fabrica = new(comBanco: true);
        await fabrica.CriarUsuarioAsync(Email, "Ana Souza", Senha, Papeis.Redator);
        await fabrica.CriarUsuarioAsync("marcos@exemplo.com.br", "Marcos Silva", Senha, Papeis.Administrador);
        return fabrica;
    }

    private static string Alerta(string html) =>
        Regex.Match(html, @"<div class=""alert alert-danger"" role=""alert"">([^<]*)</div>").Groups[1].Value;

    private static bool CriouSessao(HttpResponseMessage resposta) =>
        resposta.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> cookies)
        && cookies.Any(c => c.StartsWith("Gazeta.Equipe=", StringComparison.Ordinal) && !c.Contains("expires=Thu, 01 Jan 1970", StringComparison.Ordinal));

    [TestMethod]
    public async Task FalhaEBloqueioDeLogin_SaoRegistradosNoLog() // RC-16
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);

        for (int i = 1; i <= 5; i++)
        {
            await cliente.EntrarAsync(Email, "Senha@Errada9", ip: "10.0.0.7");
        }

        IReadOnlyList<LogEvent> eventos = fabrica.Logs.Eventos;
        string tudo = string.Join("\n", eventos.Select(ColetorSink.TudoComoTexto));

        LogEvent falha = eventos.First(e => ColetorSink.Texto(e).Contains("Falha de entrada", StringComparison.Ordinal));
        Assert.AreEqual(LogEventLevel.Warning, falha.Level);
        StringAssert.Contains(ColetorSink.Texto(falha), "senha incorreta");
        StringAssert.Contains(tudo, "a***@exemplo.com.br", "o e-mail aparece mascarado");
        Assert.IsFalse(tudo.Contains(Email, StringComparison.Ordinal), "o e-mail completo não pode ir para o log");
        Assert.IsFalse(tudo.Contains("Senha@Errada9", StringComparison.Ordinal), "a senha nunca vai para o log");

        Assert.IsTrue(eventos.Any(e => ColetorSink.Texto(e).Contains("Conta do usuário", StringComparison.Ordinal) && ColetorSink.Texto(e).Contains("bloqueada", StringComparison.Ordinal)), "bloqueio da conta registrado");
        Assert.IsTrue(eventos.Any(e => ColetorSink.Texto(e).Contains("Origem", StringComparison.Ordinal) && ColetorSink.Texto(e).Contains("bloqueada por 5 falhas", StringComparison.Ordinal)), "bloqueio da origem registrado");
    }

    [TestMethod]
    public async Task EntradaESaida_SaoRegistradasNoLog_SemSenha() // RC-16
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);

        await cliente.EntrarAsync(Email, Senha);
        await cliente.SairAsync();

        string[] textos = fabrica.Logs.Eventos.Select(ColetorSink.Texto).ToArray();
        Assert.IsTrue(textos.Any(t => t.StartsWith("Entrada do usuário", StringComparison.Ordinal) && t.Contains("Redator", StringComparison.Ordinal)));
        Assert.IsTrue(textos.Any(t => t.StartsWith("Saída do usuário", StringComparison.Ordinal)));
        Assert.IsFalse(string.Join("\n", fabrica.Logs.Eventos.Select(ColetorSink.TudoComoTexto)).Contains(Senha, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task SenhaDigitadaNoCampoDeEmail_NaoVaiParaOLog()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);

        await cliente.EntrarAsync("Minha$enhaSecreta9", "qualquer");

        string tudo = string.Join("\n", fabrica.Logs.Eventos.Select(ColetorSink.TudoComoTexto));
        Assert.IsFalse(tudo.Contains("Minha$enhaSecreta9", StringComparison.Ordinal));
        StringAssert.Contains(tudo, "(formato inválido)");
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
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);

        HttpResponseMessage entrada = await cliente.EntrarAsync(Email, Senha, retorno);

        Assert.AreEqual(HttpStatusCode.Redirect, entrada.StatusCode);
        Assert.AreEqual("/painel/anuncios", entrada.Destino(), "vai para a página inicial do papel, não para " + retorno);
    }

    [TestMethod]
    public async Task ReturnUrlLocal_E_Aceito_ParaOAdministradorTambem()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);

        HttpResponseMessage entrada = await cliente.EntrarAsync("marcos@exemplo.com.br", Senha, "/painel/anuncios?pagina=2");
        Assert.AreEqual("/painel/anuncios?pagina=2", entrada.Destino());
    }

    [TestMethod]
    public async Task ContaInexistente_SenhaErrada_Desativada_E_Bloqueada_RespondemIgual()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        await fabrica.CriarUsuarioAsync("inativo@exemplo.com.br", "Inativo", Senha, Papeis.Redator, ativo: false);
        await fabrica.CriarUsuarioAsync("travada@exemplo.com.br", "Travada", Senha, Papeis.Redator);
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);
        for (int i = 1; i <= 5; i++)
        {
            await cliente.EntrarAsync("travada@exemplo.com.br", "Senha@Errada9", ip: $"10.9.0.{i}");
        }

        (string Email, string Senha, string Ip)[] casos =
        [
            ("nao-existe@exemplo.com.br", Senha, "10.8.0.1"),
            (Email, "Senha@Errada9", "10.8.0.2"),
            ("inativo@exemplo.com.br", Senha, "10.8.0.3"),
            ("travada@exemplo.com.br", Senha, "10.8.0.4")
        ];

        foreach ((string email, string senha, string ip) in casos)
        {
            HttpResponseMessage resposta = await cliente.EntrarAsync(email, senha, ip: ip);
            Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode, email);
            Assert.AreEqual(ContaController.MensagemDeFalha, Alerta(await resposta.TextoAsync()), email + ": a mensagem não pode diferenciar os casos");
            Assert.IsFalse(CriouSessao(resposta), email + " não entra");
        }
    }

    [TestMethod]
    public async Task ContaBloqueadaPeloIdentity_BloqueiaDeVerdade_MesmoComSenhaCerta()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);
        for (int i = 1; i <= 5; i++)
        {
            await cliente.EntrarAsync(Email, "Senha@Errada9", ip: $"10.5.0.{i}");
        }

        using (IServiceScope escopo = fabrica.Services.CreateScope())
        {
            UserManager<UsuarioIdentity> usuarios = escopo.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
            Assert.IsTrue(await usuarios.IsLockedOutAsync(await usuarios.FindByEmailAsync(Email)), "o bloqueio da conta é real");
        }

        HttpResponseMessage resposta = await cliente.EntrarAsync(Email, Senha, ip: "10.5.0.99");
        Assert.AreEqual(ContaController.MensagemDeFalha, Alerta(await resposta.TextoAsync()), "a mensagem não revela o bloqueio da conta");
        Assert.IsFalse(CriouSessao(resposta));
    }

    [TestMethod]
    public async Task Formulario_SemTokenAntiforgery_E_Recusado()
    {
        using FabricaWeb fabrica = await NovaFabricaAsync();
        using HttpClient cliente = ClienteDaEquipe.Novo(fabrica);
        using FormUrlEncodedContent formulario = new(new Dictionary<string, string> { ["Email"] = Email, ["Senha"] = Senha });

        HttpResponseMessage resposta = await cliente.PostAsync("/painel/entrar", formulario);

        Assert.AreEqual(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.IsFalse(CriouSessao(resposta));
    }
}
