using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClaudeStack.Web.Tests.Playwright;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class Test1 : PageTest
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task HomepageHasPlaywrightInTitleAndGetStartedLinkLinkingToTheIntroPage()
    {
        await Page.GotoAsync("https://playwright.dev").ConfigureAwait(false);

        // Expect a title "to contain" a substring.
        await Expect(Page).ToHaveTitleAsync(new Regex("Playwright")).ConfigureAwait(false);

        // create a locator
        var getStarted = Page.Locator("text=Get Started");

        // Expect an attribute "to be strictly equal" to the value.
        await Expect(getStarted).ToHaveAttributeAsync("href", "/docs/intro").ConfigureAwait(false);

        // Click the get started link.
        await getStarted.ClickAsync().ConfigureAwait(false);

        // Expects the URL to contain intro.
        await Expect(Page).ToHaveURLAsync(new Regex(".*intro")).ConfigureAwait(false);
    }
}
