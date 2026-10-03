using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace GazetaMarketplace.Web.Tests.Playwright;

/// <summary>
/// Base dos testes de navegador que entram no painel: o site exige HTTPS (cookie e antiforgery Secure) e, fora de
/// produção, o certificado é o de desenvolvimento, que o navegador de teste não conhece.
/// </summary>
public abstract class SitePage : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new() { IgnoreHTTPSErrors = true };
}
