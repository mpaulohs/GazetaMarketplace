using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Web.Areas.Panel.Controllers;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Team;

/// <summary>US-006: entrar e sair do painel da equipe (cenários S01 a S08).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AccountTests
#pragma warning restore CA1515
{
    internal const string Writer = "ana@exemplo.com.br";
    internal const string Administrator = "marcos@exemplo.com.br";
    internal const string CorrectPassword = "Senha@Forte1";

    private static async Task<WebFactory> NewFactoryAsync()
    {
        WebFactory factory = new(withDatabase: true);
        await factory.CreateUserAsync(Writer, "Ana Souza", CorrectPassword, RoleNames.Writer);
        await factory.CreateUserAsync(Administrator, "Marcos Silva", CorrectPassword, RoleNames.Administrator);
        return factory;
    }

    [TestMethod]
    public async Task US006S01_RedatorEntraNoPainel() // @US-006-S01
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        HttpResponseMessage entry = await client.SignInAsync(Writer, CorrectPassword);
        Assert.AreEqual(HttpStatusCode.Redirect, entry.StatusCode);
        Assert.AreEqual("/painel/anuncios", entry.Destination());

        HttpResponseMessage page = await client.GetAsync("/painel/anuncios");
        string html = await page.TextAsync();

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
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        HttpResponseMessage entry = await client.SignInAsync(Administrator, CorrectPassword);
        Assert.AreEqual("/painel/anuncios/fila", entry.Destination());

        string html = await (await client.GetAsync("/painel/anuncios/fila")).TextAsync();

        StringAssert.Contains(html, "Fila de revisão");
        foreach (string item in new[] { "Anúncios", "Categorias", "Usuários", "Configurações" })
        {
            StringAssert.Matches(html, new Regex(@"<a[^>]*class=""nav-link[^""]*""[^>]*>" + item + "</a>"));
        }
    }

    [TestMethod]
    public async Task US006S03_SairDoPainel() // @US-006-S03
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Writer, CorrectPassword);

        HttpResponseMessage panel = await client.GetAsync("/painel/anuncios");
        Assert.AreEqual(HttpStatusCode.OK, panel.StatusCode);
        // Sem cache, o botão Voltar do navegador não consegue reabrir o painel guardado
        StringAssert.Contains(panel.Headers.CacheControl.ToString(), "no-store");

        HttpResponseMessage output = await client.SignOutAsync();
        Assert.AreEqual(HttpStatusCode.Redirect, output.StatusCode);
        Assert.AreEqual("/painel/entrar", output.Destination());

        // "Voltar": pedir o painel de novo leva à entrada, sem o conteúdo
        HttpResponseMessage after = await client.GetAsync("/painel/anuncios");
        Assert.AreEqual(HttpStatusCode.Redirect, after.StatusCode);
        StringAssert.StartsWith(after.Destination(), "/painel/entrar");
        Assert.IsFalse(after.Destination().Contains("expirada", StringComparison.Ordinal), "sair não é sessão expirada");
    }

    [TestMethod]
    public async Task US006S04_EMailOuSenhaIncorretos() // @US-006-S04
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        HttpResponseMessage response = await client.SignInAsync(Writer, "Senha@Errada9");
        string html = await response.TextAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "continua na página de entrada");
        StringAssert.Contains(html, AccountController.InvalidCredentialsMessage);
        StringAssert.Matches(html, new Regex(@"<input[^>]*type=""password""(?![^>]*\bvalue=)[^>]*>"));
        Assert.IsFalse(html.Contains("Senha@Errada9", StringComparison.Ordinal), "a senha digitada nunca volta para a tela");
        Assert.IsFalse(response.Headers.Contains("Set-Cookie") && response.Headers.GetValues("Set-Cookie").Any(c => c.StartsWith("Gazeta.Team", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task US006S05_ContaDesativada() // @US-006-S05
    {
        using WebFactory factory = await NewFactoryAsync();
        await factory.CreateUserAsync("joao@exemplo.com.br", "João Lima", CorrectPassword, RoleNames.Writer, active: false);
        using HttpClient client = TeamClient.Create(factory);

        HttpResponseMessage response = await client.SignInAsync("joao@exemplo.com.br", CorrectPassword);
        string html = await response.TextAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(html, AccountController.InvalidCredentialsMessage);
        Assert.IsFalse(response.Headers.Contains("Set-Cookie") && response.Headers.GetValues("Set-Cookie").Any(c => c.StartsWith("Gazeta.Team", StringComparison.Ordinal)), "não entra no painel");
        Assert.AreEqual(HttpStatusCode.Redirect, (await client.GetAsync("/painel/anuncios")).StatusCode);
    }

    [TestMethod]
    public async Task US006S06_MuitasTentativasDeEntrada() // @US-006-S06
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        for (int i = 1; i <= 5; i++)
        {
            HttpResponseMessage failure = await client.SignInAsync($"quem{i}@exemplo.com.br", "Senha@Errada9", ip: "10.0.0.1");
            StringAssert.Contains(await failure.TextAsync(), AccountController.InvalidCredentialsMessage);
        }

        // A sexta, mesmo com a senha certa, não entra
        HttpResponseMessage blocked = await client.SignInAsync(Writer, CorrectPassword, ip: "10.0.0.1");
        Assert.AreEqual((HttpStatusCode)429, blocked.StatusCode);
        StringAssert.Contains(await blocked.TextAsync(), AccountController.TooManyAttemptsMessage);
        Assert.IsTrue(blocked.Headers.Contains("Retry-After"));
        Assert.AreEqual(HttpStatusCode.Redirect, (await client.GetAsync("/painel/anuncios")).StatusCode, "não entrou");

        // Outra rede não é afetada; passado o período, a mesma volta a entrar
        HttpResponseMessage otherNetwork = await client.SignInAsync(Writer, CorrectPassword, ip: "10.0.0.2");
        Assert.AreEqual(HttpStatusCode.Redirect, otherNetwork.StatusCode);
        await client.SignOutAsync();

        factory.Clock.Now += TimeSpan.FromMinutes(16);
        HttpResponseMessage after = await client.SignInAsync(Writer, CorrectPassword, ip: "10.0.0.1");
        Assert.AreEqual(HttpStatusCode.Redirect, after.StatusCode);
    }

    [TestMethod]
    public async Task US006S06_SeisPessoasDaMesmaRedacao_EntramDeManhaSemBloqueio() // @US-006-S06 (conta falhas, não requisições)
    {
        using WebFactory factory = await NewFactoryAsync();
        for (int i = 1; i <= 6; i++)
        {
            await factory.CreateUserAsync($"redator{i}@exemplo.com.br", "Redator " + i, CorrectPassword, RoleNames.Writer);
        }

        for (int i = 1; i <= 6; i++)
        {
            using HttpClient client = TeamClient.Create(factory);
            HttpResponseMessage entry = await client.SignInAsync($"redator{i}@exemplo.com.br", CorrectPassword, ip: "200.1.1.1");
            Assert.AreEqual(HttpStatusCode.Redirect, entry.StatusCode, "a pessoa " + i + " da mesma rede entra");
        }
    }

    [TestMethod]
    public async Task US006S07_AbrirUmaPaginaDoPainelSemEstarLogado() // @US-006-S07
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        HttpResponseMessage without = await client.GetAsync("/painel/anuncios");
        Assert.AreEqual(HttpStatusCode.Redirect, without.StatusCode);
        string destination = without.Destination();
        Assert.AreEqual("/painel/entrar?ReturnUrl=%2Fpainel%2Fanuncios", destination, "sem o aviso de sessão expirada: nunca entrou");

        string returnUrl = Uri.UnescapeDataString(destination[(destination.IndexOf('=', StringComparison.Ordinal) + 1)..]);
        HttpResponseMessage entry = await client.SignInAsync(Writer, CorrectPassword, returnUrl);

        Assert.AreEqual("/painel/anuncios", entry.Destination());
        StringAssert.Contains(await (await client.GetAsync("/painel/anuncios")).TextAsync(), "Meus anúncios");
    }

    [TestMethod]
    public async Task US006S08_SessaoExpiradaPorInatividade() // @US-006-S08
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Writer, CorrectPassword);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/painel/anuncios")).StatusCode);

        factory.Clock.Now += TimeSpan.FromMinutes(31);
        HttpResponseMessage expired = await client.GetAsync("/painel/anuncios");

        Assert.AreEqual(HttpStatusCode.Redirect, expired.StatusCode);
        Assert.AreEqual("/painel/entrar?ReturnUrl=%2Fpainel%2Fanuncios&expirada=1", expired.Destination());
        HttpResponseMessage entry = await client.GetAsync(expired.Destination());
        StringAssert.Contains(await entry.TextAsync(), "Sua sessão expirou. Entre novamente.");
    }

    [TestMethod]
    public async Task EntradaSemAviso_QuandoNaoHaSessaoExpirada()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        string html = await client.GetStringAsync("/painel/entrar");

        Assert.IsFalse(html.Contains("Sua sessão expirou", StringComparison.Ordinal));
        StringAssert.Contains(html, "Esqueci minha senha");
    }

    [TestMethod]
    public async Task CamposEmBranco_MostramMensagemPorCampo_SemTentarEntrar()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        string html = await (await client.SignInAsync(string.Empty, string.Empty)).TextAsync();

        StringAssert.Contains(html, "Informe o e-mail.");
        StringAssert.Contains(html, "Informe a senha.");
        Assert.IsFalse(html.Contains(AccountController.InvalidCredentialsMessage, StringComparison.Ordinal), "campo vazio não é credencial errada");
    }

    [TestMethod]
    public async Task Redator_NaoAbreAFilaDeRevisao_VaiParaAcessoNegado()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Writer, CorrectPassword);

        HttpResponseMessage reviewQueue = await client.GetAsync("/painel/anuncios/fila");
        Assert.AreEqual(HttpStatusCode.Redirect, reviewQueue.StatusCode);
        StringAssert.StartsWith(reviewQueue.Destination(), "/painel/acesso-negado");

        HttpResponseMessage denied = await client.GetAsync(reviewQueue.Destination());
        Assert.AreEqual(HttpStatusCode.Forbidden, denied.StatusCode);
        StringAssert.Contains(await denied.TextAsync(), "Você não tem permissão para acessar esta página");
    }

    [TestMethod]
    public async Task Administrador_AcessaAAreaDoRedator()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Administrator, CorrectPassword);

        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/painel/anuncios")).StatusCode, "a política Redator aceita o Administrador");
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/painel/anuncios/fila")).StatusCode);
    }
}
