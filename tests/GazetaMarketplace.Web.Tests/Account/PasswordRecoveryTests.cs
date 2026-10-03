using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Web.Areas.Panel.Controllers;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Account;

/// <summary>Recuperar a senha esquecida por e-mail (US-007): os sete cenários da SPEC e as regras ao redor.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PasswordRecoveryTests
#pragma warning restore CA1515
{
    private const string Email = "ana@exemplo.com.br";
    private const string OldPassword = "Senha@Forte1";
    private const string NewPassword = "Nova@Senha2";

    private static async Task<RecoveryHarness> NewHarnessAsync()
    {
        RecoveryHarness harness = new();
        await harness.Factory.CreateUserAsync(Email, "Ana Souza", OldPassword, RoleNames.Writer);
        return harness;
    }

    private static async Task<RecoveryLink> RequestLinkAsync(RecoveryHarness harness)
    {
        await harness.RequestAsync(Email);
        await harness.WaitForSendingAsync();
        return harness.LastLink();
    }

    [TestMethod]
    public async Task US007S01_PedirARedefinicaoDeSenha() // @US-007-S01
    {
        using RecoveryHarness harness = await NewHarnessAsync();

        // A tela de pedido
        HttpResponseMessage screen = await harness.Client.GetAsync("/painel/esqueci-minha-senha");
        string form = await screen.TextAsync();
        Assert.AreEqual(HttpStatusCode.OK, screen.StatusCode);
        StringAssert.Contains(form, "Esqueci minha senha");
        StringAssert.Contains(form, ">Enviar</button>");

        // O pedido: mensagem neutra e um e-mail com o link
        HttpResponseMessage response = await harness.RequestAsync(Email);
        string html = await response.TextAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(html, RecoveryHarness.Neutral);
        StringAssert.Contains(html, "role=\"status\"");

        await harness.WaitForSendingAsync();
        EmailMessage mail = harness.Mail.Sent.Single();
        Assert.AreEqual(Email, mail.To);
        Assert.AreEqual("Redefinição de senha — GazetaMarketplace", mail.Subject);
        RecoveryLink link = harness.LastLink();
        StringAssert.StartsWith(link.Url, "https://localhost/painel/redefinir-senha?id=");

        // Texto simples e HTML, com a validade e o aviso para quem não pediu
        foreach (string body in new[] { mail.TextBody, mail.HtmlBody })
        {
            StringAssert.Contains(body, "1 hora");
            StringAssert.Contains(body, "Se você não solicitou a redefinição, ignore este e-mail");
        }

        StringAssert.Contains(mail.HtmlBody, "<a href=\"" + link.Url.Replace("&", "&amp;", StringComparison.Ordinal) + "\"");
    }

    [TestMethod]
    public async Task US007S02_DefinirUmaNovaSenhaPeloLink() // @US-007-S02
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        RecoveryLink link = await RequestLinkAsync(harness);
        harness.Factory.Clock.Now = harness.Factory.Clock.Now.AddMinutes(30);

        HttpResponseMessage screen = await harness.OpenAsync(link);
        string html = await screen.TextAsync();
        Assert.AreEqual(HttpStatusCode.OK, screen.StatusCode);
        StringAssert.Contains(html, "Definir nova senha");
        StringAssert.Contains(html, "Salvar senha");

        HttpResponseMessage save = await harness.SetNewPasswordAsync(link, NewPassword);
        Assert.AreEqual(HttpStatusCode.Redirect, save.StatusCode);
        Assert.AreEqual("/painel/entrar?alterada=1", save.Destination());

        StringAssert.Contains(await (await harness.Client.GetAsync(save.Destination())).TextAsync(), "Senha alterada. Entre com a nova senha.");

        using HttpClient other = TeamClient.Create(harness.Factory);
        Assert.AreEqual("/painel/anuncios", (await other.SignInAsync(Email, NewPassword)).Destination(), "a nova senha entra");
        using HttpClient another = TeamClient.Create(harness.Factory);
        StringAssert.Contains(await (await another.SignInAsync(Email, OldPassword)).TextAsync(), AccountController.InvalidCredentialsMessage);
    }

    [TestMethod]
    public async Task US007S03_EMailNaoCadastrado() // @US-007-S03
    {
        using RecoveryHarness harness = await NewHarnessAsync();

        HttpResponseMessage response = await harness.RequestAsync("naoexiste@exemplo.com.br");
        await harness.WaitForSendingAsync();

        StringAssert.Contains(await response.TextAsync(), RecoveryHarness.Neutral);
        Assert.IsEmpty(harness.Mail.Sent, "nenhum e-mail é enviado");
    }

    [TestMethod]
    public async Task US007S04_LinkDeRedefinicaoExpirado() // @US-007-S04
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        RecoveryLink link = await RequestLinkAsync(harness);

        // Aos 59 minutos ainda vale; aos 61, não
        harness.Factory.Clock.Now = harness.Factory.Clock.Now.AddMinutes(59);
        Assert.AreEqual(HttpStatusCode.OK, (await harness.OpenAsync(link)).StatusCode);
        StringAssert.Contains(await (await harness.OpenAsync(link)).TextAsync(), "Definir nova senha");

        harness.Factory.Clock.Now = harness.Factory.Clock.Now.AddMinutes(2);
        string html = await (await harness.OpenAsync(link)).TextAsync();

        StringAssert.Contains(html, "Este link expirou");
        StringAssert.Contains(html, "Pedir novo link");
        StringAssert.Contains(html, "href=\"/painel/esqueci-minha-senha\"");
        Assert.DoesNotContain("Salvar senha", html, "o formulário não aparece");
    }

    [TestMethod]
    public async Task US007S05_LinkDeRedefinicaoJaUtilizado() // @US-007-S05
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        RecoveryLink link = await RequestLinkAsync(harness);
        Assert.AreEqual(HttpStatusCode.Redirect, (await harness.SetNewPasswordAsync(link, NewPassword)).StatusCode);

        string html = await (await harness.OpenAsync(link)).TextAsync();

        StringAssert.Contains(html, "Este link já foi usado");
        StringAssert.Contains(html, "Pedir novo link");
        Assert.DoesNotContain("Salvar senha", html);

        // Nem mandando o formulário direto o link funciona de novo: a senha não muda
        string post = await (await harness.SetNewPasswordAsync(link, "Outra@Senha3")).TextAsync();
        StringAssert.Contains(post, "Este link já foi usado");
        using HttpClient other = TeamClient.Create(harness.Factory);
        Assert.AreEqual("/painel/anuncios", (await other.SignInAsync(Email, NewPassword)).Destination());
    }

    [TestMethod]
    public async Task US007S06_NovaSenhaQueNaoCumpreAPolitica() // @US-007-S06
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        RecoveryLink link = await RequestLinkAsync(harness);

        HttpResponseMessage response = await harness.SetNewPasswordAsync(link, "abc123");
        string html = await response.TextAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(html, "A senha precisa ter 8 caracteres ou mais.");
        StringAssert.Contains(html, "A senha precisa ter uma letra maiúscula.");
        StringAssert.Contains(html, "A senha precisa ter um símbolo");
        Assert.DoesNotContain("uma letra minúscula", html.Replace("maiúscula, minúscula", string.Empty, StringComparison.Ordinal), "o que já cumpre não é cobrado");
        Assert.DoesNotContain("abc123", html, "a senha nunca volta para a tela");

        using HttpClient other = TeamClient.Create(harness.Factory);
        Assert.AreEqual("/painel/anuncios", (await other.SignInAsync(Email, OldPassword)).Destination(), "a senha não mudou");

        // Senha fraca não gasta o link: com uma senha boa, ele ainda funciona
        Assert.AreEqual(HttpStatusCode.Redirect, (await harness.SetNewPasswordAsync(link, NewPassword)).StatusCode);
    }

    [TestMethod]
    public async Task US007S07_ConfirmacaoDeSenhaDiferente() // @US-007-S07
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        RecoveryLink link = await RequestLinkAsync(harness);

        string html = await (await harness.SetNewPasswordAsync(link, NewPassword, "Outra@Senha3")).TextAsync();

        StringAssert.Contains(html, "As senhas não coincidem");
        using HttpClient other = TeamClient.Create(harness.Factory);
        Assert.AreEqual("/painel/anuncios", (await other.SignInAsync(Email, OldPassword)).Destination(), "a senha não mudou");
        Assert.AreEqual(HttpStatusCode.Redirect, (await harness.SetNewPasswordAsync(link, NewPassword)).StatusCode, "o link continua valendo");
    }

    [TestMethod]
    public async Task ContaDesativada_NaoRecebeEmailDeRedefinicao()
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        await harness.Factory.CreateUserAsync("bia@exemplo.com.br", "Bia Lima", OldPassword, RoleNames.Writer, active: false);

        StringAssert.Contains(await (await harness.RequestAsync("bia@exemplo.com.br")).TextAsync(), RecoveryHarness.Neutral);
        await harness.WaitForSendingAsync();

        Assert.IsEmpty(harness.Mail.Sent);
    }

    [TestMethod]
    public async Task ContaDesativadaDepoisDoPedido_OLinkNaoFunciona()
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        RecoveryLink link = await RequestLinkAsync(harness);
        await harness.Factory.UpdateUserAsync(Email, u => u.IsActive = false);

        StringAssert.Contains(await (await harness.OpenAsync(link)).TextAsync(), "Este link não é válido");
        Assert.AreEqual(HttpStatusCode.OK, (await harness.SetNewPasswordAsync(link, NewPassword)).StatusCode);
        using HttpClient other = TeamClient.Create(harness.Factory);
        StringAssert.Contains(await (await other.SignInAsync(Email, NewPassword)).TextAsync(), AccountController.InvalidCredentialsMessage);
    }

    [TestMethod]
    public async Task EMailEmMaiusculasEComEspacos_AcheiaAMesmaConta()
    {
        using RecoveryHarness harness = await NewHarnessAsync();

        await harness.RequestAsync("  ANA@Exemplo.com.br ");
        await harness.WaitForSendingAsync();

        Assert.AreEqual(Email, harness.Mail.Sent.Single().To);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("não-é-email")]
    public async Task EMailEmBrancoOuInvalido_MostraErro_ENaoGravaPedido(string email)
    {
        using RecoveryHarness harness = await NewHarnessAsync();

        HttpResponseMessage response = await harness.RequestAsync(email);
        string html = await response.TextAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(RecoveryHarness.Neutral, html);
        Assert.AreEqual(0, harness.AttemptCount());
        Assert.IsEmpty(harness.Mail.Sent);
    }

    [TestMethod]
    [DataRow("lixo")]
    [DataRow("")]
    [DataRow("!!!")]
    public async Task CodigoQueNaoEUmCodigoNosso_MostraLinkInvalido(string code)
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        AppUser user = (await harness.Factory.ListUsersAsync()).Single();

        string html = await (await harness.Client.GetAsync($"/painel/redefinir-senha?id={user.Id}&code={Uri.EscapeDataString(code)}")).TextAsync();

        StringAssert.Contains(html, "Este link não é válido");
        StringAssert.Contains(html, "Pedir novo link");
    }

    [TestMethod]
    public async Task LinkDeUmaPessoa_NaoAbreAContaDeOutra()
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        AppUser other = await harness.Factory.CreateUserAsync("caio@exemplo.com.br", "Caio Reis", OldPassword, RoleNames.Writer);
        RecoveryLink link = await RequestLinkAsync(harness);

        string html = await (await harness.Client.GetAsync($"/painel/redefinir-senha?id={other.Id}&code={link.Code}")).TextAsync();

        StringAssert.Contains(html, "Este link não é válido");
    }

    [TestMethod]
    public async Task OLinkUsaOEnderecoConfigurado_NaoOCabecalhoHost()
    {
        // Sem isto, quem forjasse o cabeçalho Host receberia no e-mail da vítima um link para o próprio servidor
        using RecoveryHarness harness = new(new() { ["Site:BaseUrl"] = "https://gazeta.exemplo.com.br" });
        await harness.Factory.CreateUserAsync(Email, "Ana Souza", OldPassword, RoleNames.Writer);

        await harness.RequestAsync(Email, host: "atacante.example");
        await harness.WaitForSendingAsync();

        string text = harness.Mail.Sent.Single().TextBody;
        StringAssert.Contains(text, "https://gazeta.exemplo.com.br/painel/redefinir-senha?id=");
        Assert.DoesNotContain("atacante.example", text);
    }

    [TestMethod]
    public async Task RedefinirComSucesso_AuditaSemSenhaNemLink()
    {
        using RecoveryHarness harness = await NewHarnessAsync();
        RecoveryLink link = await RequestLinkAsync(harness);
        await harness.SetNewPasswordAsync(link, NewPassword);

        using IServiceScope scope = harness.Factory.Services.CreateScope();
        var entry = scope.ServiceProvider.GetRequiredService<GazetaMarketplace.Infrastructure.Data.AppDbContext>().AuditEntries.Single(a => a.Action == "user.recover_password");
        Assert.AreEqual("User", entry.TargetType);
        Assert.AreEqual(GazetaMarketplace.Core.Entities.AuditResult.Success, entry.Result);
        Assert.IsNull(entry.NewValue);
        Assert.IsNull(entry.PreviousValue);
    }
}
