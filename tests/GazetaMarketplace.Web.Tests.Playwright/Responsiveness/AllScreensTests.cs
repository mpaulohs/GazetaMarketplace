using System.Collections.Generic;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Playwright.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Responsiveness;

/// <summary>
/// NFR-17: cada tela da lista única (<c>Screens/screens.json</c>) não rola na horizontal em 320, 768, 1024 e 1280 px (a tela é aberta de novo em cada largura). Mesmas variáveis e mesma lista dos testes de acessibilidade.
/// </summary>
[TestClass]
[DoNotParallelize] // o preparo dos dados usa o ViaCEP de mentira, que escuta numa porta só: os testes de CEP e de publicação não podem rodar ao mesmo tempo
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_E2E_EMAIL", "GAZETA_E2E_PASSWORD", "GAZETA_E2E_VIACEP_PORT")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class AllScreensTests : SitePage
#pragma warning restore CA1515
{
    [TestMethod]
    [DynamicData(nameof(ScreenCatalog.Ids), typeof(ScreenCatalog), DynamicDataDisplayName = nameof(ScreenCatalog.DisplayName), DynamicDataDisplayNameDeclaringType = typeof(ScreenCatalog))]
    public async Task Screen_DoesNotScrollHorizontally_AtFourWidths(string screenId)
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

        string[] wrong = await WidthHelper.OverflowsAsync(Page, () => ScreenData.OpenAsync(Page, screen, values)).ConfigureAwait(false);

        Assert.IsEmpty(wrong, $"{screen.Id} ({screen.State}) rola na horizontal:\n" + string.Join("\n", wrong));
    }
}
