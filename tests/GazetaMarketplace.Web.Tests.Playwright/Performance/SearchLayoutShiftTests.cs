using System;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Performance;
using GazetaMarketplace.Web.Tests.Playwright.Support;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Performance;

/// <summary>
/// NFR-03 na busca, de forma determinística: no celular o painel de filtros já vem recolhido no HTML, então a lista de resultados não pula quando o JavaScript chega. O defeito que isto trava (achado na 6.3) só aparecia
/// quando a primeira pintura vinha antes do script (celular lento e máquina ocupada); aqui o script é atrasado de propósito em 2 s, o que reproduz o caso pior sem depender da velocidade da máquina.
/// Roda no E2E normal (não precisa de GAZETA_VITALS).
/// </summary>
[TestClass]
[RequiresVariables("GAZETA_BASE_URL")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class SearchLayoutShiftTests : SitePage
#pragma warning restore CA1515
{
    private const string ShiftObserver = """
        window.__shift = { total: 0, sources: [] };
        new PerformanceObserver((list) => { for (const e of list.getEntries()) if (!e.hadRecentInput) { window.__shift.total += e.value; window.__shift.sources.push(e.value.toFixed(4) + ': ' + (e.sources || []).map(s => s.node ? s.node.nodeName.toLowerCase() + (s.node.id ? '#' + s.node.id : '') : '?').join(', ')); } }).observe({ type: 'layout-shift', buffered: true });
        """;

    [TestMethod]
    public async Task Search_OnAPhone_DoesNotShiftTheList_WhenTheScriptsArriveTwoSecondsLate()
    {
        Budgets budgets = Budgets.Current;
        IBrowserContext context = await Browser.NewContextAsync(PageMeter.MobileContext()).ConfigureAwait(false);
        IPage page = await context.NewPageAsync().ConfigureAwait(false);
        await page.AddInitScriptAsync(ShiftObserver).ConfigureAwait(false);
        await page.RouteAsync("**/*.js*", async route =>
        {
            await Task.Delay(2000).ConfigureAwait(false);
            await route.ContinueAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        string baseUrl = RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/');
        await page.GotoAsync(baseUrl + "/busca?q=livro", new PageGotoOptions { WaitUntil = WaitUntilState.Commit }).ConfigureAwait(false);
        await Expect(page.Locator("[data-ad-card]").First).ToBeVisibleAsync().ConfigureAwait(false); // a primeira pintura da lista, ainda sem nenhum script
        await Expect(page.Locator("#filtros")).ToBeHiddenAsync().ConfigureAwait(false);               // o painel já está recolhido antes do JavaScript
        await Expect(page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("^Filtros") })).ToBeVisibleAsync().ConfigureAwait(false); // e o botão de abrir também
        await page.WaitForLoadStateAsync(LoadState.Load).ConfigureAwait(false);                         // os scripts chegaram
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        await page.WaitForTimeoutAsync(1000).ConfigureAwait(false);

        JsonElement shift = await page.EvaluateAsync<JsonElement>("() => window.__shift").ConfigureAwait(false);
        double total = shift.GetProperty("total").GetDouble();
        PerfLog.Write($"BUSCA com script atrasado em 2 s: CLS {total:F4} (limite {budgets.ClsMax}) · {string.Join(" | ", System.Linq.Enumerable.Select(shift.GetProperty("sources").EnumerateArray(), e => e.GetString()))}");

        Assert.IsLessThan(budgets.ClsMax, total, "a lista pulou quando o JavaScript chegou: " + shift.GetProperty("sources"));
        await context.CloseAsync().ConfigureAwait(false);
    }
}
