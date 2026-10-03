using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Areas.Painel.Controllers;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Team;

/// <summary>US-006-S09: a senha provisória obriga a trocar antes de ver o painel.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PrimeiroAcessoTests
#pragma warning restore CA1515
{
    private const string Email = "nova@exemplo.com.br";
    private const string Provisoria = "Provisoria@1";
    private const string Nova = "Nova@Senha2";

    private static async Task<WebFactory> NovaFabricaAsync(string role = RoleNames.Writer)
    {
        WebFactory factory = new(withDatabase: true);
        await factory.CreateUserAsync(Email, "Nova Pessoa", Provisoria, role, mustChangePassword: true);
        await factory.CreateUserAsync("veterana@exemplo.com.br", "Veterana", "Senha@Forte1", RoleNames.Writer);
        return factory;
    }

    private static async Task<bool> PrecisaTrocarAsync(WebFactory factory) =>
        (await factory.ListUsersAsync()).Single(u => u.Email == Email).MustChangePassword;

    [TestMethod]
    public async Task US006S09_PrimeiroAcessoExigeTrocarASenhaProvisoria() // @US-006-S09
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        // Entra com a senha provisória e cai na troca, antes de ver o painel
        HttpResponseMessage entry = await client.EntrarAsync(Email, Provisoria);
        Assert.AreEqual("/painel/definir-senha", entry.Destino());
        HttpResponseMessage tela = await client.GetAsync("/painel/definir-senha");
        string html = await tela.TextoAsync();
        Assert.AreEqual(HttpStatusCode.OK, tela.StatusCode);
        StringAssert.Contains(html, "Defina sua nova senha");
        StringAssert.Contains(html, "Você entrou com uma senha provisória.");
        StringAssert.Contains(html, "A senha precisa ter 8 caracteres ou mais, maiúscula, minúscula, número e símbolo.");

        // Define e confirma uma senha que cumpre a política: vê o painel
        HttpResponseMessage troca = await client.DefinirSenhaAsync(Nova);
        Assert.AreEqual(HttpStatusCode.Redirect, troca.StatusCode);
        Assert.AreEqual("/painel/anuncios", troca.Destino());
        StringAssert.Contains(await (await client.GetAsync("/painel/anuncios")).TextoAsync(), "Meus anúncios");
        Assert.IsFalse(await PrecisaTrocarAsync(factory));

        // A senha provisória deixou de valer; a nova entra direto
        using HttpClient outro = ClienteDaEquipe.Novo(factory);
        StringAssert.Contains(await (await outro.EntrarAsync(Email, Provisoria)).TextoAsync(), ContaController.MensagemDeFalha);
        Assert.AreEqual("/painel/anuncios", (await outro.EntrarAsync(Email, Nova)).Destino());
    }

    [TestMethod]
    public async Task EnquantoASenhaForProvisoria_TodaPaginaDoPainelLevaATroca()
    {
        using WebFactory factory = await NovaFabricaAsync(RoleNames.Administrator);
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Email, Provisoria);

        foreach (string page in new[] { "/painel/anuncios", "/painel/anuncios/fila" })
        {
            HttpResponseMessage response = await client.GetAsync(page);
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, page);
            Assert.AreEqual("/painel/definir-senha", response.Destino(), page);
        }

        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/painel/definir-senha")).StatusCode, "a própria tela de troca não entra em laço");
    }

    [TestMethod]
    public async Task DuranteATroca_OMenuFicaOculto_ESairContinuaDisponivel()
    {
        using WebFactory factory = await NovaFabricaAsync(RoleNames.Administrator);
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Email, Provisoria);

        string html = await (await client.GetAsync("/painel/definir-senha")).TextoAsync();
        foreach (string route in new[] { "/painel/anuncios", "/painel/categorias", "/painel/usuarios", "/painel/configuracoes" })
        {
            Assert.IsFalse(html.Contains("href=\"" + route + "\"", StringComparison.Ordinal), "sem menu: " + route);
        }

        StringAssert.Matches(html, new Regex(@"<button[^>]*>(?:\s*<i[^>]*></i>)?\s*Sair\s*</button>"));
        HttpResponseMessage saida = await client.SairAsync("/painel/definir-senha");
        Assert.AreEqual("/painel/entrar", saida.Destino());
    }

    [TestMethod]
    public async Task SenhaFraca_E_Recusada_ESenhaProvisoriaContinuaValendo()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Email, Provisoria);

        (string Fraca, string Message)[] casos =
        [
            ("Ab1!", "A senha precisa ter 8 caracteres ou mais."),
            ("senha@forte1", "A senha precisa ter uma letra maiúscula."),
            ("SENHA@FORTE1", "A senha precisa ter uma letra minúscula."),
            ("Senha@Forte", "A senha precisa ter um número."),
            ("SenhaForte12", "A senha precisa ter um símbolo")
        ];
        foreach ((string fraca, string message) in casos)
        {
            HttpResponseMessage response = await client.DefinirSenhaAsync(fraca);
            string html = await response.TextoAsync();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, fraca);
            StringAssert.Contains(html, message, fraca);
            Assert.IsFalse(html.Contains(fraca, StringComparison.Ordinal), "a senha digitada não volta para a tela: " + fraca);
        }

        // Atômico: nenhuma tentativa recusada pode ter apagado ou trocado a senha provisória
        Assert.IsTrue(await PrecisaTrocarAsync(factory));
        using HttpClient outro = ClienteDaEquipe.Novo(factory);
        Assert.AreEqual("/painel/definir-senha", (await outro.EntrarAsync(Email, Provisoria)).Destino(), "a provisória ainda entra");
    }

    [TestMethod]
    public async Task NovaSenhaIgualAProvisoria_E_Recusada()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Email, Provisoria);

        string html = await (await client.DefinirSenhaAsync(Provisoria)).TextoAsync();

        StringAssert.Contains(html, SenhaController.MensagemIgualAProvisoria);
        Assert.IsTrue(await PrecisaTrocarAsync(factory));
    }

    [TestMethod]
    public async Task ConfirmacaoDiferente_ECamposEmBranco_MostramMensagemPorCampo()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Email, Provisoria);

        StringAssert.Contains(await (await client.DefinirSenhaAsync(Nova, "Outra@Senha3")).TextoAsync(), "As senhas não são iguais.");
        string vazio = await (await client.DefinirSenhaAsync(string.Empty, string.Empty)).TextoAsync();
        StringAssert.Contains(vazio, "Informe a nova senha.");
        StringAssert.Contains(vazio, "Confirme a nova senha.");
        Assert.IsTrue(await PrecisaTrocarAsync(factory));
    }

    [TestMethod]
    public async Task Administrador_DepoisDaTroca_VaiParaAFila()
    {
        using WebFactory factory = await NovaFabricaAsync(RoleNames.Administrator);
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Email, Provisoria);

        HttpResponseMessage troca = await client.DefinirSenhaAsync(Nova);

        Assert.AreEqual("/painel/anuncios/fila", troca.Destino());
    }

    [TestMethod]
    public async Task QuemNaoPrecisaTrocar_NaoVeATela_EQuemNaoEntrouVaiParaAEntrada()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient veterana = ClienteDaEquipe.Novo(factory);
        await veterana.EntrarAsync("veterana@exemplo.com.br", "Senha@Forte1");
        HttpResponseMessage semPrecisar = await veterana.GetAsync("/painel/definir-senha");
        Assert.AreEqual("/painel/anuncios", semPrecisar.Destino());

        using HttpClient anonimo = ClienteDaEquipe.Novo(factory);
        HttpResponseMessage semLogin = await anonimo.GetAsync("/painel/definir-senha");
        StringAssert.StartsWith(semLogin.Destino(), "/painel/entrar");
    }

    [TestMethod]
    public async Task SenhasDaTroca_NaoVaoParaOLog()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Email, Provisoria);
        await client.DefinirSenhaAsync("fraca");
        await client.DefinirSenhaAsync(Nova);

        string all = string.Join("\n", factory.Logs.Events.Select(CollectorSink.AllAsText));
        foreach (string secret in new[] { Provisoria, Nova, "fraca" })
        {
            Assert.IsFalse(all.Contains(secret, StringComparison.Ordinal), "a senha " + secret + " foi para o log");
        }

        StringAssert.Contains(all, "definiu a nova senha");
    }
}
