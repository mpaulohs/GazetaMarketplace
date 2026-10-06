using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Performance;
using GazetaMarketplace.Web.Tests.Playwright.Support;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Performance;

/// <summary>
/// NFR-01, NFR-02 e NFR-03: LCP abaixo de 2,5 s, INP abaixo de 200 ms e CLS abaixo de 0,1 nas páginas públicas (início, categoria, busca e detalhe), no perfil de celular médio (janela 412 × 823, toque, processador 4 vezes
/// mais lento, rede 4G de 1,6 Mbit/s e 150 ms). Os limites vêm de <c>budgets.json</c>. <b>Só roda com GAZETA_VITALS=1</b>: com o site de teste na mesma máquina e rede e processador simulados, o número é uma amostra que prova
/// o mecanismo; os números que valem são os do <c>/verify</c>, contra o artefato de verdade.
/// </summary>
[TestClass]
[DoNotParallelize] // a lentidão simulada e a medição de tempo não combinam com outros navegadores disputando a máquina
[RequiresVariables("GAZETA_BASE_URL", "GAZETA_VITALS")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class VitalsTests : SitePage
#pragma warning restore CA1515
{
    private const string ListPath = "/categoria/livros-e-revistas";

    private const string Observers = """
        window.__v = { lcp: 0, cls: 0, inp: 0, interactions: 0, list: [], shifts: [] };
        new PerformanceObserver((list) => { for (const e of list.getEntries()) window.__v.lcp = e.startTime; }).observe({ type: 'largest-contentful-paint', buffered: true });
        new PerformanceObserver((list) => { for (const e of list.getEntries()) if (!e.hadRecentInput) { window.__v.cls += e.value; window.__v.shifts.push(e.value.toFixed(4) + ' em ' + Math.round(e.startTime) + ' ms: ' + (e.sources || []).map(s => s.node ? s.node.nodeName.toLowerCase() + (s.node.id ? '#' + s.node.id : '') + (s.node.className && typeof s.node.className === 'string' ? '.' + s.node.className.split(' ')[0] : '') : '?').join(', ')); } }).observe({ type: 'layout-shift', buffered: true });
        new PerformanceObserver((list) => { for (const e of list.getEntries()) if (e.interactionId) { window.__v.interactions++; window.__v.inp = Math.max(window.__v.inp, e.duration); window.__v.list.push(e.name + ' ' + Math.round(e.duration) + ' ms (espera ' + Math.round(e.processingStart - e.startTime) + ', código ' + Math.round(e.processingEnd - e.processingStart) + ') em ' + (e.target ? e.target.tagName.toLowerCase() + (e.target.className && typeof e.target.className === 'string' ? '.' + e.target.className.split(' ')[0] : '') : '?')); } }).observe({ type: 'event', durationThreshold: 16, buffered: true });
        """;

    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    public static System.Collections.Generic.IEnumerable<object[]> Pages() => [["home"], ["category"], ["search"], ["detail"]];

    public static string DisplayName(System.Reflection.MethodInfo method, object[] data) => $"{method.Name}({data[0]})";

    private static async Task TapAsync(IPage page, ILocator target)
    {
        if (await target.CountAsync().ConfigureAwait(false) > 0 && await target.First.IsVisibleAsync().ConfigureAwait(false))
        {
            await target.First.TapAsync().ConfigureAwait(false);
            await page.WaitForTimeoutAsync(600).ConfigureAwait(false); // o navegador só fecha a medida da interação depois de pintar o resultado
        }
    }

    [TestMethod]
    [DynamicData(nameof(Pages), DynamicDataDisplayName = nameof(DisplayName))]
    public async Task LcpInpCls_OnTheMobileProfile_AreWithinTheBudget(string kind)
    {
        Budgets budgets = Budgets.Current;
        IBrowserContext context = await Browser.NewContextAsync(PageMeter.MobileContext()).ConfigureAwait(false);
        IPage page = await context.NewPageAsync().ConfigureAwait(false);
        _ = await PageMeter.AttachAsync(page, throttle: true).ConfigureAwait(false);
        await page.AddInitScriptAsync(Observers).ConfigureAwait(false);

        string path = kind switch { "home" => "/", "category" => ListPath, "search" => "/busca?q=livro", _ => null };
        if (path is null)
        {
            await page.GotoAsync(Url(ListPath)).ConfigureAwait(false);
            path = await page.Locator("[data-ad-card] a.ad-card__link").First.GetAttributeAsync("href").ConfigureAwait(false);
            if (path is null)
            {
                Assert.Inconclusive("A categoria não tem anúncios; publique no banco do E2E.");
            }
        }

        await page.GotoAsync(Url(path), new PageGotoOptions { WaitUntil = WaitUntilState.Load }).ConfigureAwait(false);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        await page.WaitForTimeoutAsync(1500).ConfigureAwait(false);
        JsonElement afterLoad = await page.EvaluateAsync<JsonElement>("() => window.__v").ConfigureAwait(false);
        double lcp = afterLoad.GetProperty("lcp").GetDouble();

        // Interações de verdade: favoritar e desfavoritar, abrir o painel de filtros, passar a foto e ampliar
        await TapAsync(page, page.Locator("[data-favorite-toggle]:visible")).ConfigureAwait(false);
        await TapAsync(page, page.Locator("[data-favorite-toggle]:visible")).ConfigureAwait(false);
        await TapAsync(page, page.Locator("[data-filters-toggle]")).ConfigureAwait(false);
        await TapAsync(page, page.Locator("[data-gallery-next]")).ConfigureAwait(false);
        await TapAsync(page, page.Locator("[data-gallery-open]")).ConfigureAwait(false);

        JsonElement end = await page.EvaluateAsync<JsonElement>("() => window.__v").ConfigureAwait(false);
        double cls = end.GetProperty("cls").GetDouble();
        double inp = end.GetProperty("inp").GetDouble();
        int interactions = end.GetProperty("interactions").GetInt32();
        PerfLog.Write("  mudanças de layout: " + string.Join(" | ", end.GetProperty("shifts").EnumerateArray().Select(e => e.GetString())));
        PerfLog.Write("  interações: " + string.Join(" | ", end.GetProperty("list").EnumerateArray().Select(e => e.GetString())));
        PerfLog.Write(string.Create(CultureInfo.InvariantCulture, $"VITAIS {kind} ({path}): LCP {lcp:F0} ms (limite {budgets.LcpMaxMs:F0}) · INP {inp:F0} ms em {interactions} interação(ões) (limite {budgets.InpMaxMs:F0}) · CLS {cls:F4} (limite {budgets.ClsMax})"));

        Assert.IsGreaterThan(0, lcp, "o navegador não informou o LCP");
        Assert.IsGreaterThan(0, interactions, "nenhuma interação foi medida (o teste não provaria o INP)");
        Assert.IsLessThan(budgets.LcpMaxMs, lcp, $"LCP de {kind}");
        Assert.IsLessThan(budgets.InpMaxMs, inp, $"INP de {kind}");
        Assert.IsLessThan(budgets.ClsMax, cls, $"CLS de {kind}");
        await context.CloseAsync().ConfigureAwait(false);
    }
}
