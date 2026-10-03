using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Team;

/// <summary>
/// US-015 no navegador (roda no /test). Variáveis: GAZETA_BASE_URL (site no ar) e GAZETA_E2E_EMAIL / GAZETA_E2E_PASSWORD
/// (conta de <b>Administrador</b> no banco de teste). O telefone é um valor só do site inteiro; por isso a classe não roda em paralelo.
/// </summary>
[TestClass]
[DoNotParallelize]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class SettingsE2ETests : SitePage
#pragma warning restore CA1515
{
    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static async Task SignInAsync(IPage page, string email, string password)
    {
        await page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await page.GetByLabel("E-mail").FillAsync(email).ConfigureAwait(false);
        await page.GetByLabel("Senha").FillAsync(password).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
    }

    private async Task OpenSettingsAsAdminAsync()
    {
        await SignInAsync(Page, RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL"), RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Configurações" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page).ToHaveURLAsync(new Regex(@"/painel/configuracoes$")).ConfigureAwait(false);
    }

    private ILocator Phone => Page.GetByLabel(new Regex("^Telefone/WhatsApp do site"));

    private async Task SaveAsync(string value)
    {
        await Phone.FillAsync(value).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US015S01eS03_SalvarOTelefone_SemFormatacao_EVoltaFormatado() // @US-015-S01 @US-015-S03
    {
        await OpenSettingsAsAdminAsync().ConfigureAwait(false);

        await SaveAsync("11912345678").ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Configurações salvas").ConfigureAwait(false);
        await Expect(Phone).ToHaveValueAsync("(11) 91234-5678").ConfigureAwait(false);

        // Só vale o que o servidor guardou: depois de recarregar o número continua lá, formatado
        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Phone).ToHaveValueAsync("(11) 91234-5678").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US015S02_TrocarONumero_ENumeroComPrefixoDoPais() // @US-015-S02
    {
        await OpenSettingsAsAdminAsync().ConfigureAwait(false);
        await SaveAsync("(11) 91234-5678").ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Configurações salvas").ConfigureAwait(false);

        await SaveAsync("+55 (21) 98765-4321").ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Configurações salvas").ConfigureAwait(false);
        await Page.ReloadAsync().ConfigureAwait(false);
        await Expect(Phone).ToHaveValueAsync("(21) 98765-4321").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US015S04_NumeroInvalido_AvisaEMantemOQueFoiDigitado() // @US-015-S04
    {
        await OpenSettingsAsAdminAsync().ConfigureAwait(false);
        await SaveAsync("(11) 91234-5678").ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Configurações salvas").ConfigureAwait(false);

        await SaveAsync("abc123").ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Informe um número com DDD, por exemplo (11) 91234-5678" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(Phone).ToHaveValueAsync("abc123").ConfigureAwait(false);
        await Expect(Phone).ToHaveAttributeAsync("aria-invalid", "true").ConfigureAwait(false);

        // Recarregar reenviaria o formulário: abrir o endereço de novo mostra o que o servidor guardou
        await Page.GotoAsync(Url("/painel/configuracoes")).ConfigureAwait(false);
        await Expect(Phone).ToHaveValueAsync("(11) 91234-5678").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US015S05_NumeroVazio_AvisaQueEObrigatorio() // @US-015-S05
    {
        await OpenSettingsAsAdminAsync().ConfigureAwait(false);
        await SaveAsync("(11) 91234-5678").ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Configurações salvas").ConfigureAwait(false);

        await SaveAsync("").ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "O telefone é obrigatório" })).ToBeVisibleAsync().ConfigureAwait(false);
        // Recarregar reenviaria o formulário: abrir o endereço de novo mostra o que o servidor guardou
        await Page.GotoAsync(Url("/painel/configuracoes")).ConfigureAwait(false);
        await Expect(Phone).ToHaveValueAsync("(11) 91234-5678").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US015S06_RedatorNaoAcessaAsConfiguracoes() // @US-015-S06
    {
        // O Administrador cria o Redator; o Redator define a senha no primeiro acesso e tenta abrir o endereço direto
        await SignInAsync(Page, RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL"), RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        string unique = Guid.NewGuid().ToString("N")[..8];
        string email = $"e2e-{unique}@exemplo.com.br";
        const string Provisional = "Provis0ria!";
        const string OwnPassword = "Minh@Senha2026";
        await Page.GotoAsync(Url("/painel/usuarios/novo")).ConfigureAwait(false);
        await Page.GetByLabel("Nome").FillAsync("E2E " + unique).ConfigureAwait(false);
        await Page.GetByLabel("E-mail").FillAsync(email).ConfigureAwait(false);
        await Page.GetByLabel("Redator").CheckAsync().ConfigureAwait(false);
        await Page.GetByLabel("Senha provisória").FillAsync(Provisional).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("criado").ConfigureAwait(false);

        IBrowserContext separate = await Browser.NewContextAsync(ContextOptions()).ConfigureAwait(false);
        IPage writer = await separate.NewPageAsync().ConfigureAwait(false);
        await SignInAsync(writer, email, Provisional).ConfigureAwait(false);
        await writer.GetByLabel(new Regex("^Nova senha")).FillAsync(OwnPassword).ConfigureAwait(false);
        await writer.GetByLabel("Confirmar nova senha").FillAsync(OwnPassword).ConfigureAwait(false);
        await writer.GetByRole(AriaRole.Button, new() { Name = "Salvar senha" }).ClickAsync().ConfigureAwait(false);
        await Expect(writer.GetByRole(AriaRole.Button, new() { Name = "Sair" })).ToBeVisibleAsync().ConfigureAwait(false);

        await writer.GotoAsync(Url("/painel/configuracoes")).ConfigureAwait(false);

        await Expect(writer.GetByText("Você não tem permissão para acessar esta página")).ToBeVisibleAsync().ConfigureAwait(false);
        await Expect(writer.GetByLabel(new Regex("^Telefone/WhatsApp do site"))).ToHaveCountAsync(0).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Pagina_SemViolacoesDeAcessibilidade_ComTecladoEAvisoDeSalvando()
    {
        await OpenSettingsAsAdminAsync().ConfigureAwait(false);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);

        AxeResult result = await Page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"] }
        }).ConfigureAwait(false);
        Assert.AreEqual(0, result.Violations.Length, string.Join("; ", System.Linq.Enumerable.Select(result.Violations, v => v.Id + ": " + v.Help)));

        // Teclado: o campo tem rótulo e dica ligados, e Enter envia o formulário
        await Expect(Phone).ToHaveAttributeAsync("type", "tel").ConfigureAwait(false);
        await Expect(Phone).ToHaveAttributeAsync("aria-describedby", new Regex("phone-help")).ConfigureAwait(false);
        await Phone.FillAsync("(11) 91234-5678").ConfigureAwait(false);
        await Phone.PressAsync("Enter").ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Configurações salvas").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SemJavaScript_SalvarContinuaFuncionando() // @progressive-enhancement
    {
        IBrowserContext noScript = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true, JavaScriptEnabled = false }).ConfigureAwait(false);
        IPage page = await noScript.NewPageAsync().ConfigureAwait(false);
        await SignInAsync(page, RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL"), RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        await page.GotoAsync(Url("/painel/configuracoes")).ConfigureAwait(false);

        await page.GetByLabel(new Regex("^Telefone/WhatsApp do site")).FillAsync("11912345678").ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);

        await Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("Configurações salvas").ConfigureAwait(false);
        await Expect(page.GetByLabel(new Regex("^Telefone/WhatsApp do site"))).ToHaveValueAsync("(11) 91234-5678").ConfigureAwait(false);
    }
}
