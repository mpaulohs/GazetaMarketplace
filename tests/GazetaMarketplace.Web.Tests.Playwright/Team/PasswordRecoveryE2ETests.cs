using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Team;

/// <summary>
/// US-007 no navegador (roda no /test). Além das variáveis dos outros E2E, usa GAZETA_E2E_SENDGRID_PORT: porta de um SendGrid de mentira
/// que o próprio teste abre e para onde o site (iniciado com <c>SendGrid__BaseUrl=http://localhost:PORTA</c>) manda o e-mail. É assim que
/// o teste lê o link, sem caixa de correio real, e confere de passagem o pedido à API v3.
/// </summary>
[TestClass]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_SENDGRID_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class PasswordRecoveryE2ETests : SitePage
#pragma warning restore CA1515
{
    private const string Provisional = "Provis0ria!";
    private const string NewPassword = "Nova@Senha2";
    private const string Neutral = "Se o e-mail estiver cadastrado, enviaremos as instruções";

    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private async Task<string> CreateWriterAsync()
    {
        await Page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await Page.GetByLabel("E-mail").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL")).ConfigureAwait(false);
        await Page.GetByLabel("Senha").FillAsync(RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);

        string unique = Guid.NewGuid().ToString("N")[..8];
        string email = $"e2e-rec-{unique}@exemplo.com.br";
        await Page.GotoAsync(Url("/painel/usuarios/novo")).ConfigureAwait(false);
        await Page.GetByLabel("Nome").FillAsync("E2E Recuperação " + unique).ConfigureAwait(false);
        await Page.GetByLabel("E-mail").FillAsync(email).ConfigureAwait(false);
        await Page.GetByLabel("Redator").CheckAsync().ConfigureAwait(false);
        await Page.GetByLabel("Senha provisória").FillAsync(Provisional).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("criado.").ConfigureAwait(false);
        return email;
    }

    /// <summary>Contexto novo (sem os cookies do Administrador), como a pessoa que esqueceu a senha.</summary>
    private async Task<IPage> NewVisitorAsync()
    {
        IBrowserContext context = await Browser.NewContextAsync(ContextOptions()).ConfigureAwait(false);
        return await context.NewPageAsync().ConfigureAwait(false);
    }

    private static async Task AssertNoAxeViolationsAsync(IPage page)
    {
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        AxeResult result = await page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] }
        }).ConfigureAwait(false);
        Assert.AreEqual(0, result.Violations.Length, string.Join("; ", System.Linq.Enumerable.Select(result.Violations, v => v.Id + ": " + v.Help)));
    }

    [TestMethod]
    public async Task US007_EsqueciMinhaSenha_PedirOLink_DefinirANovaSenha_ELinkNaoValeDeNovo()
    {
        string email = await CreateWriterAsync().ConfigureAwait(false);
        int port = int.Parse(RequiresVariablesAttribute.Value("GAZETA_E2E_SENDGRID_PORT"), System.Globalization.CultureInfo.InvariantCulture);
        using HttpListener sendGrid = new();
        sendGrid.Prefixes.Add($"http://localhost:{port}/");
        sendGrid.Start();
        Task<HttpListenerContext> incoming = sendGrid.GetContextAsync();

        IPage visitor = await NewVisitorAsync().ConfigureAwait(false);

        // Acessibilidade das telas novas (WCAG 2.1 AA): pedido e definição da senha
        await visitor.GotoAsync(Url("/painel/esqueci-minha-senha")).ConfigureAwait(false);
        await AssertNoAxeViolationsAsync(visitor).ConfigureAwait(false);

        // S01: pelo link "Esqueci minha senha" da tela de entrada
        await visitor.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Link, new() { Name = "Esqueci minha senha" }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = "Esqueci minha senha" })).ToBeVisibleAsync().ConfigureAwait(false);
        await visitor.GetByLabel("E-mail").FillAsync(email).ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Button, new() { Name = "Enviar" }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Status)).ToContainTextAsync(Neutral).ConfigureAwait(false);

        // O e-mail chegou ao "SendGrid": POST /v3/mail/send com Bearer, texto e HTML
        HttpListenerContext request = await incoming.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
        string body = await new System.IO.StreamReader(request.Request.InputStream, Encoding.UTF8).ReadToEndAsync().ConfigureAwait(false);
        request.Response.StatusCode = 202;
        request.Response.Close();
        Assert.AreEqual("POST", request.Request.HttpMethod);
        Assert.AreEqual("/v3/mail/send", request.Request.Url.AbsolutePath);
        StringAssert.StartsWith(request.Request.Headers["Authorization"], "Bearer ");
        using JsonDocument json = JsonDocument.Parse(body);
        Assert.AreEqual(email, json.RootElement.GetProperty("personalizations")[0].GetProperty("to")[0].GetProperty("email").GetString());
        Assert.AreEqual("Redefinição de senha — GazetaMarketplace", json.RootElement.GetProperty("subject").GetString());
        string text = json.RootElement.GetProperty("content")[0].GetProperty("value").GetString();
        string link = Regex.Match(text, @"https?://\S+/painel/redefinir-senha\?id=\d+&code=[A-Za-z0-9_\-]+").Value;
        Assert.IsFalse(string.IsNullOrEmpty(link), "o e-mail traz o link");

        // S06 e S07 no navegador: senha fraca e confirmação diferente não mudam nada
        await visitor.GotoAsync(link).ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Heading, new() { Name = "Definir nova senha" })).ToBeVisibleAsync().ConfigureAwait(false);
        await AssertNoAxeViolationsAsync(visitor).ConfigureAwait(false);
        await visitor.GetByLabel(new Regex("^Nova senha")).FillAsync("abc123").ConfigureAwait(false);
        await visitor.GetByLabel("Confirmar nova senha").FillAsync("abc123").ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Button, new() { Name = "Salvar senha" }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByText("A senha precisa ter uma letra maiúscula.")).ToBeVisibleAsync().ConfigureAwait(false);

        await visitor.GetByLabel(new Regex("^Nova senha")).FillAsync(NewPassword).ConfigureAwait(false);
        await visitor.GetByLabel("Confirmar nova senha").FillAsync("Outra@Senha3").ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Button, new() { Name = "Salvar senha" }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByText("As senhas não coincidem")).ToBeVisibleAsync().ConfigureAwait(false);

        // S02: senha válida duas vezes
        await visitor.GetByLabel(new Regex("^Nova senha")).FillAsync(NewPassword).ConfigureAwait(false);
        await visitor.GetByLabel("Confirmar nova senha").FillAsync(NewPassword).ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Button, new() { Name = "Salvar senha" }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"/painel/entrar\?alterada=1")).ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Status)).ToContainTextAsync("Senha alterada. Entre com a nova senha.").ConfigureAwait(false);

        // A senha nova entra; a provisória não
        await visitor.GetByLabel("E-mail").FillAsync(email).ConfigureAwait(false);
        await visitor.GetByLabel("Senha").FillAsync(Provisional).ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor.GetByText("E-mail ou senha inválidos, ou conta desativada")).ToBeVisibleAsync().ConfigureAwait(false);
        await visitor.GetByLabel("Senha").FillAsync(NewPassword).ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
        await Expect(visitor).ToHaveURLAsync(new Regex(@"/painel/anuncios$")).ConfigureAwait(false);

        // S05: o mesmo link, de novo, já foi usado; e o botão leva a pedir outro
        IPage again = await NewVisitorAsync().ConfigureAwait(false);
        await again.GotoAsync(link).ConfigureAwait(false);
        await Expect(again.GetByText("Este link já foi usado")).ToBeVisibleAsync().ConfigureAwait(false);
        await again.GetByRole(AriaRole.Link, new() { Name = "Pedir novo link" }).ClickAsync().ConfigureAwait(false);
        await Expect(again).ToHaveURLAsync(new Regex(@"/painel/esqueci-minha-senha$")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US007S03_EmailNaoCadastrado_MostraAMesmaMensagem()
    {
        IPage visitor = await NewVisitorAsync().ConfigureAwait(false);
        await visitor.GotoAsync(Url("/painel/esqueci-minha-senha")).ConfigureAwait(false);
        await visitor.GetByLabel("E-mail").FillAsync($"naoexiste-{Guid.NewGuid():N}"[..24] + "@exemplo.com.br").ConfigureAwait(false);
        await visitor.GetByRole(AriaRole.Button, new() { Name = "Enviar" }).ClickAsync().ConfigureAwait(false);

        await Expect(visitor.GetByRole(AriaRole.Status)).ToContainTextAsync(Neutral).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LinkInventado_MostraLinkInvalido_ComBotaoParaPedirOutro()
    {
        IPage visitor = await NewVisitorAsync().ConfigureAwait(false);
        await visitor.GotoAsync(Url("/painel/redefinir-senha?id=1&code=AAAA")).ConfigureAwait(false);

        await Expect(visitor.GetByText("Este link não é válido")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(visitor.GetByRole(AriaRole.Link, new() { Name = "Pedir novo link" })).ToBeVisibleAsync().ConfigureAwait(false);
        await AssertNoAxeViolationsAsync(visitor).ConfigureAwait(false);
    }
}
