using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Playwright.Support;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Accessibility;

/// <summary>
/// NFR-16: cada tela da lista única (<c>Screens/screens.json</c>), no navegador, não tem nenhuma falha do axe-core nos níveis A e AA do WCAG 2.1, no computador (1280 px) e no celular (320 px). As telas do painel
/// abrem como Administrador; as do site público, como visitante. Variáveis: GAZETA_BASE_URL, GAZETA_E2E_EMAIL, GAZETA_E2E_PASSWORD, GAZETA_E2E_VIACEP_PORT (e GAZETA_DEV_BASE_URL para o catálogo de componentes).
/// </summary>
[TestClass]
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class AllScreensTests : SitePage
#pragma warning restore CA1515
{
    [TestMethod]
    [DynamicData(nameof(ScreenCatalog.Ids), typeof(ScreenCatalog), DynamicDataDisplayName = nameof(ScreenCatalog.DisplayName), DynamicDataDisplayNameDeclaringType = typeof(ScreenCatalog))]
    public async Task Screen_HasNoAxeViolations_LevelsAAndAA(string screenId)
    {
        Screen screen = ScreenCatalog.Find(screenId);
        if (screen.IsDevelopmentSite && ScreenData.DevUrl is null)
        {
            Assert.Inconclusive("Defina GAZETA_DEV_BASE_URL (o site em Development) para medir " + screen.Id);
        }

        IReadOnlyDictionary<string, string> values = await ScreenData.EnsureAsync(Page).ConfigureAwait(false);
        if (screen.As == "admin")
        {
            await ScreenData.SignInAdminAsync(Page, ScreenData.BaseOf(screen)).ConfigureAwait(false);
        }

        List<string> wrong = [];
        foreach ((int width, int height) in new[] { (1280, 800), (320, 700) })
        {
            await Page.SetViewportSizeAsync(width, height).ConfigureAwait(false);
            await ScreenData.OpenAsync(Page, screen, values).ConfigureAwait(false);
            foreach (string violation in await AxeHelper.ViolationsAsync(Page).ConfigureAwait(false))
            {
                wrong.Add($"{width} px: {violation}");
            }
        }

        Assert.IsEmpty(wrong, $"{screen.Id} ({screen.State}) tem falhas do axe:\n" + string.Join("\n", wrong));
    }
}
