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

/// <summary>US-006: entrar e sair do painel da equipe (cenários S01 a S08).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ContaTests
#pragma warning restore CA1515
{
    internal const string Writer = "ana@exemplo.com.br";
    internal const string Administrator = "marcos@exemplo.com.br";
    internal const string SenhaCerta = "Senha@Forte1";

    private static async Task<WebFactory> NovaFabricaAsync()
    {
        WebFactory factory = new(withDatabase: true);
        await factory.CreateUserAsync(Writer, "Ana Souza", SenhaCerta, RoleNames.Writer);
        await factory.CreateUserAsync(Administrator, "Marcos Silva", SenhaCerta, RoleNames.Administrator);
        return factory;
    }

    [TestMethod]
    public async Task US006S01_RedatorEntraNoPainel() // @US-006-S01
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        HttpResponseMessage entry = await client.EntrarAsync(Writer, SenhaCerta);
        Assert.AreEqual(HttpStatusCode.Redirect, entry.StatusCode);
        Assert.AreEqual("/painel/anuncios", entry.Destino());

        HttpResponseMessage page = await client.GetAsync("/painel/anuncios");
        string html = await page.TextoAsync();

        Assert.AreEqual(HttpStatusCode.OK, page.StatusCode);
        StringAssert.Contains(html, "Meus anúncios");
        StringAssert.Contains(html, "Ana Souza");
        StringAssert.Matches(html, new Regex(@"<button[^>]*>(?:\s*<i[^>]*></i>)?\s*Sair\s*</button>"));
        foreach (string menu in new[] { "/painel/categorias", "/painel/usuarios", "/painel/configuracoes" })
        {
            Assert.IsFalse(html.Contains("href=\"" + menu + "\"", StringComparison.Ordinal), "o Redator não vê " + menu);
        }
    }

    [TestMethod]
    public async Task US006S02_AdministradorEntraNoPainel() // @US-006-S02
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        HttpResponseMessage entry = await client.EntrarAsync(Administrator, SenhaCerta);
        Assert.AreEqual("/painel/anuncios/fila", entry.Destino());

        string html = await (await client.GetAsync("/painel/anuncios/fila")).TextoAsync();

        StringAssert.Contains(html, "Fila de revisão");
        foreach (string item in new[] { "Anúncios", "Categorias", "Usuários", "Configurações" })
        {
            StringAssert.Matches(html, new Regex(@"<a[^>]*class=""nav-link[^""]*""[^>]*>" + item + "</a>"));
        }
    }

    [TestMethod]
    public async Task US006S03_SairDoPainel() // @US-006-S03
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Writer, SenhaCerta);

        HttpResponseMessage panel = await client.GetAsync("/painel/anuncios");
        Assert.AreEqual(HttpStatusCode.OK, panel.StatusCode);
        // Sem cache, o botão Voltar do navegador não consegue reabrir o painel guardado
        StringAssert.Contains(panel.Headers.CacheControl.ToString(), "no-store");

        HttpResponseMessage saida = await client.SairAsync();
        Assert.AreEqual(HttpStatusCode.Redirect, saida.StatusCode);
        Assert.AreEqual("/painel/entrar", saida.Destino());

        // "Voltar": pedir o painel de novo leva à entrada, sem o conteúdo
        HttpResponseMessage depois = await client.GetAsync("/painel/anuncios");
        Assert.AreEqual(HttpStatusCode.Redirect, depois.StatusCode);
        StringAssert.StartsWith(depois.Destino(), "/painel/entrar");
        Assert.IsFalse(depois.Destino().Contains("expirada", StringComparison.Ordinal), "sair não é sessão expirada");
    }

    [TestMethod]
    public async Task US006S04_EMailOuSenhaIncorretos() // @US-006-S04
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        HttpResponseMessage response = await client.EntrarAsync(Writer, "Senha@Errada9");
        string html = await response.TextoAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "continua na página de entrada");
        StringAssert.Contains(html, ContaController.MensagemDeFalha);
        StringAssert.Matches(html, new Regex(@"<input[^>]*type=""password""(?![^>]*\bvalue=)[^>]*>"));
        Assert.IsFalse(html.Contains("Senha@Errada9", StringComparison.Ordinal), "a senha digitada nunca volta para a tela");
        Assert.IsFalse(response.Headers.Contains("Set-Cookie") && response.Headers.GetValues("Set-Cookie").Any(c => c.StartsWith("Gazeta.Equipe", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task US006S05_ContaDesativada() // @US-006-S05
    {
        using WebFactory factory = await NovaFabricaAsync();
        await factory.CreateUserAsync("joao@exemplo.com.br", "João Lima", SenhaCerta, RoleNames.Writer, active: false);
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        HttpResponseMessage response = await client.EntrarAsync("joao@exemplo.com.br", SenhaCerta);
        string html = await response.TextoAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(html, ContaController.MensagemDeFalha);
        Assert.IsFalse(response.Headers.Contains("Set-Cookie") && response.Headers.GetValues("Set-Cookie").Any(c => c.StartsWith("Gazeta.Equipe", StringComparison.Ordinal)), "não entra no painel");
        Assert.AreEqual(HttpStatusCode.Redirect, (await client.GetAsync("/painel/anuncios")).StatusCode);
    }

    [TestMethod]
    public async Task US006S06_MuitasTentativasDeEntrada() // @US-006-S06
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        for (int i = 1; i <= 5; i++)
        {
            HttpResponseMessage falha = await client.EntrarAsync($"quem{i}@exemplo.com.br", "Senha@Errada9", ip: "10.0.0.1");
            StringAssert.Contains(await falha.TextoAsync(), ContaController.MensagemDeFalha);
        }

        // A sexta, mesmo com a senha certa, não entra
        HttpResponseMessage bloqueada = await client.EntrarAsync(Writer, SenhaCerta, ip: "10.0.0.1");
        Assert.AreEqual((HttpStatusCode)429, bloqueada.StatusCode);
        StringAssert.Contains(await bloqueada.TextoAsync(), ContaController.MensagemDeExcesso);
        Assert.IsTrue(bloqueada.Headers.Contains("Retry-After"));
        Assert.AreEqual(HttpStatusCode.Redirect, (await client.GetAsync("/painel/anuncios")).StatusCode, "não entrou");

        // Outra rede não é afetada; passado o período, a mesma volta a entrar
        HttpResponseMessage outraRede = await client.EntrarAsync(Writer, SenhaCerta, ip: "10.0.0.2");
        Assert.AreEqual(HttpStatusCode.Redirect, outraRede.StatusCode);
        await client.SairAsync();

        factory.Clock.Now += TimeSpan.FromMinutes(16);
        HttpResponseMessage depois = await client.EntrarAsync(Writer, SenhaCerta, ip: "10.0.0.1");
        Assert.AreEqual(HttpStatusCode.Redirect, depois.StatusCode);
    }

    [TestMethod]
    public async Task US006S06_SeisPessoasDaMesmaRedacao_EntramDeManhaSemBloqueio() // @US-006-S06 (conta falhas, não requisições)
    {
        using WebFactory factory = await NovaFabricaAsync();
        for (int i = 1; i <= 6; i++)
        {
            await factory.CreateUserAsync($"redator{i}@exemplo.com.br", "Redator " + i, SenhaCerta, RoleNames.Writer);
        }

        for (int i = 1; i <= 6; i++)
        {
            using HttpClient client = ClienteDaEquipe.Novo(factory);
            HttpResponseMessage entry = await client.EntrarAsync($"redator{i}@exemplo.com.br", SenhaCerta, ip: "200.1.1.1");
            Assert.AreEqual(HttpStatusCode.Redirect, entry.StatusCode, "a pessoa " + i + " da mesma rede entra");
        }
    }

    [TestMethod]
    public async Task US006S07_AbrirUmaPaginaDoPainelSemEstarLogado() // @US-006-S07
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        HttpResponseMessage without = await client.GetAsync("/painel/anuncios");
        Assert.AreEqual(HttpStatusCode.Redirect, without.StatusCode);
        string destino = without.Destino();
        Assert.AreEqual("/painel/entrar?ReturnUrl=%2Fpainel%2Fanuncios", destino, "sem o aviso de sessão expirada: nunca entrou");

        string retorno = Uri.UnescapeDataString(destino[(destino.IndexOf('=', StringComparison.Ordinal) + 1)..]);
        HttpResponseMessage entry = await client.EntrarAsync(Writer, SenhaCerta, retorno);

        Assert.AreEqual("/painel/anuncios", entry.Destino());
        StringAssert.Contains(await (await client.GetAsync("/painel/anuncios")).TextoAsync(), "Meus anúncios");
    }

    [TestMethod]
    public async Task US006S08_SessaoExpiradaPorInatividade() // @US-006-S08
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Writer, SenhaCerta);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/painel/anuncios")).StatusCode);

        factory.Clock.Now += TimeSpan.FromMinutes(31);
        HttpResponseMessage expirada = await client.GetAsync("/painel/anuncios");

        Assert.AreEqual(HttpStatusCode.Redirect, expirada.StatusCode);
        Assert.AreEqual("/painel/entrar?ReturnUrl=%2Fpainel%2Fanuncios&expirada=1", expirada.Destino());
        HttpResponseMessage entry = await client.GetAsync(expirada.Destino());
        StringAssert.Contains(await entry.TextoAsync(), "Sua sessão expirou. Entre novamente.");
    }

    [TestMethod]
    public async Task EntradaSemAviso_QuandoNaoHaSessaoExpirada()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        string html = await client.GetStringAsync("/painel/entrar");

        Assert.IsFalse(html.Contains("Sua sessão expirou", StringComparison.Ordinal));
        StringAssert.Contains(html, "Esqueci minha senha");
    }

    [TestMethod]
    public async Task CamposEmBranco_MostramMensagemPorCampo_SemTentarEntrar()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);

        string html = await (await client.EntrarAsync(string.Empty, string.Empty)).TextoAsync();

        StringAssert.Contains(html, "Informe o e-mail.");
        StringAssert.Contains(html, "Informe a senha.");
        Assert.IsFalse(html.Contains(ContaController.MensagemDeFalha, StringComparison.Ordinal), "campo vazio não é credencial errada");
    }

    [TestMethod]
    public async Task Redator_NaoAbreAFilaDeRevisao_VaiParaAcessoNegado()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Writer, SenhaCerta);

        HttpResponseMessage fila = await client.GetAsync("/painel/anuncios/fila");
        Assert.AreEqual(HttpStatusCode.Redirect, fila.StatusCode);
        StringAssert.StartsWith(fila.Destino(), "/painel/acesso-negado");

        HttpResponseMessage negado = await client.GetAsync(fila.Destino());
        Assert.AreEqual(HttpStatusCode.Forbidden, negado.StatusCode);
        StringAssert.Contains(await negado.TextoAsync(), "Você não tem permissão para acessar esta página");
    }

    [TestMethod]
    public async Task Administrador_AcessaAAreaDoRedator()
    {
        using WebFactory factory = await NovaFabricaAsync();
        using HttpClient client = ClienteDaEquipe.Novo(factory);
        await client.EntrarAsync(Administrator, SenhaCerta);

        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/painel/anuncios")).StatusCode, "a política Redator aceita o Administrador");
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/painel/anuncios/fila")).StatusCode);
    }
}
