using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Team;

/// <summary>
/// US-014 no navegador (roda no /test). Variáveis: GAZETA_BASE_URL (site no ar) e GAZETA_E2E_EMAIL / GAZETA_E2E_PASSWORD
/// (conta de <b>Administrador</b> no banco de teste). Cada teste cria a própria conta, com e-mail único.
/// </summary>
[TestClass]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class UsersE2ETests : SitePage
#pragma warning restore CA1515
{
    private const string Provisional = "Provis0ria!";

    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static async Task SignInAsync(IPage page, string email, string password)
    {
        await page.GotoAsync(Url("/painel/entrar")).ConfigureAwait(false);
        await page.GetByLabel("E-mail").FillAsync(email).ConfigureAwait(false);
        await page.GetByLabel("Senha").FillAsync(password).ConfigureAwait(false);
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync().ConfigureAwait(false);
    }

    /// <summary>Entra como Administrador e cria uma conta de Redator com e-mail único; devolve nome e e-mail.</summary>
    private async Task<(string Name, string Email)> CreateWriterAsync()
    {
        await SignInAsync(Page, RequiresVariablesAttribute.Value("GAZETA_E2E_EMAIL"), RequiresVariablesAttribute.Value("GAZETA_E2E_PASSWORD")).ConfigureAwait(false);
        string unique = Guid.NewGuid().ToString("N")[..8];
        string name = "E2E " + unique;
        string email = $"e2e-{unique}@exemplo.com.br";

        await Page.GotoAsync(Url("/painel/usuarios/novo")).ConfigureAwait(false);
        await Page.GetByLabel("Nome").FillAsync(name).ConfigureAwait(false);
        await Page.GetByLabel("E-mail").FillAsync(email).ConfigureAwait(false);
        await Page.GetByLabel("Redator").CheckAsync().ConfigureAwait(false);
        await Page.GetByLabel("Senha provisória").FillAsync(Provisional).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Salvar" }).ClickAsync().ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Usuário {name} criado.").ConfigureAwait(false);
        return (name, email);
    }

    [TestMethod]
    public async Task US014S03_DesativarUmaConta() // @US-014-S03
    {
        (string name, string email) = await CreateWriterAsync().ConfigureAwait(false);

        await Page.GetByRole(AriaRole.Link, new() { Name = $"Desativar {name}" }).First.ClickAsync().ConfigureAwait(false);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = $"Desativar {name}?" })).ToBeVisibleAsync().ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Desativar" }).ClickAsync().ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Conta de {name} desativada.").ConfigureAwait(false);
        await Expect(Page.Locator("tr", new() { HasText = name })).ToContainTextAsync("Conta desativada").ConfigureAwait(false);

        // A pessoa desativada não entra, mesmo com os dados certos
        IBrowserContext separate = await Browser.NewContextAsync(ContextOptions()).ConfigureAwait(false);
        IPage other = await separate.NewPageAsync().ConfigureAwait(false);
        await SignInAsync(other, email, Provisional).ConfigureAwait(false);
        await Expect(other.GetByText("E-mail ou senha inválidos, ou conta desativada")).ToBeVisibleAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task US014S10_RedefinirASenhaDeAlguemDaEquipe() // @US-014-S10
    {
        (string name, string email) = await CreateWriterAsync().ConfigureAwait(false);
        const string NewProvisional = "Outr@Provis0ria";

        await Page.GetByRole(AriaRole.Link, new() { Name = $"Redefinir senha de {name}" }).First.ClickAsync().ConfigureAwait(false);
        await Page.GetByLabel("Senha provisória").FillAsync(NewProvisional).ConfigureAwait(false);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Redefinir senha" }).ClickAsync().ConfigureAwait(false);

        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Senha de {name} redefinida. Informe a senha provisória a ela fora do sistema.").ConfigureAwait(false);

        // A senha anterior não vale; a nova leva à tela "Defina sua nova senha"
        IBrowserContext separate = await Browser.NewContextAsync(ContextOptions()).ConfigureAwait(false);
        IPage other = await separate.NewPageAsync().ConfigureAwait(false);
        await SignInAsync(other, email, Provisional).ConfigureAwait(false);
        await Expect(other.GetByText("E-mail ou senha inválidos, ou conta desativada")).ToBeVisibleAsync().ConfigureAwait(false);
        await SignInAsync(other, email, NewProvisional).ConfigureAwait(false);
        await Expect(other).ToHaveURLAsync(new Regex(@"/painel/definir-senha")).ConfigureAwait(false);
        await Expect(other.GetByRole(AriaRole.Heading, new() { Name = "Defina sua nova senha" })).ToBeVisibleAsync().ConfigureAwait(false);
    }
}
