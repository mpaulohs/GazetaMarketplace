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

/// <summary>US-006-S09: a senha provisória obriga a trocar antes de ver o painel.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class FirstAccessTests
#pragma warning restore CA1515
{
    private const string Email = "nova@exemplo.com.br";
    private const string Provisional = "Provisoria@1";
    private const string NewPassword = "Nova@Senha2";

    private static async Task<WebFactory> NewFactoryAsync(string role = RoleNames.Writer)
    {
        WebFactory factory = new(withDatabase: true);
        await factory.CreateUserAsync(Email, "Nova Pessoa", Provisional, role, mustChangePassword: true);
        await factory.CreateUserAsync("veterana@exemplo.com.br", "Veterana", "Senha@Forte1", RoleNames.Writer);
        return factory;
    }

    private static async Task<bool> NeedsPasswordChangeAsync(WebFactory factory) =>
        (await factory.ListUsersAsync()).Single(u => u.Email == Email).MustChangePassword;

    [TestMethod]
    public async Task US006S09_PrimeiroAcessoExigeTrocarASenhaProvisoria() // @US-006-S09
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);

        // Entra com a senha provisória e cai na troca, antes de ver o painel
        HttpResponseMessage entry = await client.SignInAsync(Email, Provisional);
        Assert.AreEqual("/painel/definir-senha", entry.Destination());
        HttpResponseMessage screen = await client.GetAsync("/painel/definir-senha");
        string html = await screen.TextAsync();
        Assert.AreEqual(HttpStatusCode.OK, screen.StatusCode);
        StringAssert.Contains(html, "Defina sua nova senha");
        StringAssert.Contains(html, "Você entrou com uma senha provisória.");
        StringAssert.Contains(html, "A senha precisa ter 8 caracteres ou mais, maiúscula, minúscula, número e símbolo.");

        // Define e confirma uma senha que cumpre a política: vê o painel
        HttpResponseMessage change = await client.SetPasswordAsync(NewPassword);
        Assert.AreEqual(HttpStatusCode.Redirect, change.StatusCode);
        Assert.AreEqual("/painel/anuncios", change.Destination());
        StringAssert.Contains(await (await client.GetAsync("/painel/anuncios")).TextAsync(), "Meus anúncios");
        Assert.IsFalse(await NeedsPasswordChangeAsync(factory));

        // A senha provisória deixou de valer; a nova entra direto
        using HttpClient other = TeamClient.Create(factory);
        StringAssert.Contains(await (await other.SignInAsync(Email, Provisional)).TextAsync(), AccountController.InvalidCredentialsMessage);
        Assert.AreEqual("/painel/anuncios", (await other.SignInAsync(Email, NewPassword)).Destination());
    }

    [TestMethod]
    public async Task EnquantoASenhaForProvisoria_TodaPaginaDoPainelLevaATroca()
    {
        using WebFactory factory = await NewFactoryAsync(RoleNames.Administrator);
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Email, Provisional);

        foreach (string page in new[] { "/painel/anuncios", "/painel/anuncios/fila" })
        {
            HttpResponseMessage response = await client.GetAsync(page);
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, page);
            Assert.AreEqual("/painel/definir-senha", response.Destination(), page);
        }

        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/painel/definir-senha")).StatusCode, "a própria tela de troca não entra em laço");
    }

    [TestMethod]
    public async Task DuranteATroca_OMenuFicaOculto_ESairContinuaDisponivel()
    {
        using WebFactory factory = await NewFactoryAsync(RoleNames.Administrator);
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Email, Provisional);

        string html = await (await client.GetAsync("/painel/definir-senha")).TextAsync();
        foreach (string route in new[] { "/painel/anuncios", "/painel/categorias", "/painel/usuarios", "/painel/configuracoes" })
        {
            Assert.IsFalse(html.Contains("href=\"" + route + "\"", StringComparison.Ordinal), "sem menu: " + route);
        }

        StringAssert.Matches(html, new Regex(@"<button[^>]*>(?:\s*<i[^>]*></i>)?\s*Sair\s*</button>"));
        HttpResponseMessage output = await client.SignOutAsync("/painel/definir-senha");
        Assert.AreEqual("/painel/entrar", output.Destination());
    }

    [TestMethod]
    public async Task SenhaFraca_E_Recusada_ESenhaProvisoriaContinuaValendo()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Email, Provisional);

        (string Weak, string Message)[] cases =
        [
            ("Ab1!", "A senha precisa ter 8 caracteres ou mais."),
            ("senha@forte1", "A senha precisa ter uma letra maiúscula."),
            ("SENHA@FORTE1", "A senha precisa ter uma letra minúscula."),
            ("Senha@Forte", "A senha precisa ter um número."),
            ("SenhaForte12", "A senha precisa ter um símbolo")
        ];
        foreach ((string weak, string message) in cases)
        {
            HttpResponseMessage response = await client.SetPasswordAsync(weak);
            string html = await response.TextAsync();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, weak);
            StringAssert.Contains(html, message, weak);
            Assert.IsFalse(html.Contains(weak, StringComparison.Ordinal), "a senha digitada não volta para a tela: " + weak);
        }

        // Atômico: nenhuma tentativa recusada pode ter apagado ou trocado a senha provisória
        Assert.IsTrue(await NeedsPasswordChangeAsync(factory));
        using HttpClient other = TeamClient.Create(factory);
        Assert.AreEqual("/painel/definir-senha", (await other.SignInAsync(Email, Provisional)).Destination(), "a provisória ainda entra");
    }

    [TestMethod]
    public async Task NovaSenhaIgualAProvisoria_E_Recusada()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Email, Provisional);

        string html = await (await client.SetPasswordAsync(Provisional)).TextAsync();

        StringAssert.Contains(html, PasswordController.SameAsProvisionalMessage);
        Assert.IsTrue(await NeedsPasswordChangeAsync(factory));
    }

    [TestMethod]
    public async Task ConfirmacaoDiferente_ECamposEmBranco_MostramMensagemPorCampo()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Email, Provisional);

        StringAssert.Contains(await (await client.SetPasswordAsync(NewPassword, "Outra@Senha3")).TextAsync(), "As senhas não são iguais.");
        string empty = await (await client.SetPasswordAsync(string.Empty, string.Empty)).TextAsync();
        StringAssert.Contains(empty, "Informe a nova senha.");
        StringAssert.Contains(empty, "Confirme a nova senha.");
        Assert.IsTrue(await NeedsPasswordChangeAsync(factory));
    }

    [TestMethod]
    public async Task Administrador_DepoisDaTroca_VaiParaAFila()
    {
        using WebFactory factory = await NewFactoryAsync(RoleNames.Administrator);
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Email, Provisional);

        HttpResponseMessage change = await client.SetPasswordAsync(NewPassword);

        Assert.AreEqual("/painel/anuncios/fila", change.Destination());
    }

    [TestMethod]
    public async Task QuemNaoPrecisaTrocar_NaoVeATela_EQuemNaoEntrouVaiParaAEntrada()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient veteran = TeamClient.Create(factory);
        await veteran.SignInAsync("veterana@exemplo.com.br", "Senha@Forte1");
        HttpResponseMessage withoutNeed = await veteran.GetAsync("/painel/definir-senha");
        Assert.AreEqual("/painel/anuncios", withoutNeed.Destination());

        using HttpClient anonymous = TeamClient.Create(factory);
        HttpResponseMessage withoutSignIn = await anonymous.GetAsync("/painel/definir-senha");
        StringAssert.StartsWith(withoutSignIn.Destination(), "/painel/entrar");
    }

    [TestMethod]
    public async Task SenhasDaTroca_NaoVaoParaOLog()
    {
        using WebFactory factory = await NewFactoryAsync();
        using HttpClient client = TeamClient.Create(factory);
        await client.SignInAsync(Email, Provisional);
        await client.SetPasswordAsync("fraca");
        await client.SetPasswordAsync(NewPassword);

        string all = string.Join("\n", factory.Logs.Events.Select(CollectorSink.AllAsText));
        foreach (string secret in new[] { Provisional, NewPassword, "fraca" })
        {
            Assert.IsFalse(all.Contains(secret, StringComparison.Ordinal), "a senha " + secret + " foi para o log");
        }

        StringAssert.Contains(all, "definiu a nova senha");
    }
}
