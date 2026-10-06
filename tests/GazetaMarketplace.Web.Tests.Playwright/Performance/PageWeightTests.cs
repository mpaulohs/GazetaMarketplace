using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Performance;
using GazetaMarketplace.Web.Tests.Playwright.Support;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Playwright.Performance;

/// <summary>
/// NFR-05 e a parte de rede da NFR-01, no site publicado, com um visitante e o cache vazio: a primeira carga da lista de 24 anúncios pesa até 2 MB e a do detalhe, até 3 MB (o que trafegou na rede, já comprimido,
/// antes de qualquer toque); a lista mostra a capa na versão de 480 px; o HTML sai comprimido; e todo arquivo com <c>?v=</c> tem cache imutável. Os limites vêm de <c>budgets.json</c>.
/// O banco do E2E já tem centenas de anúncios; se a categoria não tiver 24, o teste fica inconclusivo.
/// </summary>
[TestClass]
[RequiresVariables("GAZETA_BASE_URL")]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public class PageWeightTests : SitePage
#pragma warning restore CA1515
{
    private const string ListPath = "/categoria/livros-e-revistas";

    private static string Url(string path) => RequiresVariablesAttribute.Value("GAZETA_BASE_URL").TrimEnd('/') + path;

    private static string Describe(IEnumerable<Transfer> transfers) =>
        string.Join("; ", transfers.GroupBy(t => t.Type).Select(g => $"{g.Key}: {g.Count()} arquivo(s), {g.Sum(t => t.Bytes) / 1024.0:F0} KB").OrderBy(x => x, StringComparer.Ordinal));

    private async Task<(PageMeter Meter, IPage Page)> OpenAsync(string path, IBrowserContext context = null)
    {
        context ??= await Browser.NewContextAsync(PageMeter.MobileContext()).ConfigureAwait(false);
        IPage page = await context.NewPageAsync().ConfigureAwait(false);
        PageMeter meter = await PageMeter.AttachAsync(page).ConfigureAwait(false);
        await page.GotoAsync(Url(path)).ConfigureAwait(false);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        return (meter, page);
    }

    [TestMethod]
    public async Task ListOf24_FirstLoad_IsUpTo2Mb_WithTheSmallCoverAndACompressedDocument()
    {
        Budgets budgets = Budgets.Current;
        (PageMeter meter, IPage page) = await OpenAsync(ListPath).ConfigureAwait(false);
        int cards = await page.Locator("[data-ad-card]").CountAsync().ConfigureAwait(false);
        if (cards < budgets.ListCardCount)
        {
            Assert.Inconclusive($"A categoria tem {cards} cards; o orçamento é para {budgets.ListCardCount}. Publique mais anúncios no banco do E2E.");
        }

        IReadOnlyList<Transfer> covers = [.. meter.Transfers.Where(t => t.Type == "Image" && Regex.IsMatch(t.Url, @"/fotos/\d+/\d+-\d+\.webp"))];
        Transfer document = meter.Transfers.First(t => t.Type == "Document");
        string summary = $"LISTA: {meter.TotalBytes / 1024.0:F0} KB (limite {budgets.ListPageWeightMaxBytes / 1024} KB) · {Describe(meter.Transfers)}";
        PerfLog.Write(summary);

        Assert.AreEqual(budgets.ListCardCount, cards, "a lista tem 24 cards por página");
        Assert.IsLessThanOrEqualTo(budgets.ListPageWeightMaxBytes, meter.TotalBytes, summary);
        long nonImage = meter.Transfers.Where(t => t.Type != "Image").Sum(t => t.Bytes);
        Assert.IsLessThanOrEqualTo(budgets.NonImageAllowanceBytes, nonImage, $"o que não é foto (HTML, CSS, JS, fontes) estourou a folga de {budgets.NonImageAllowanceBytes / 1024} KB: {nonImage / 1024.0:F0} KB. {summary}");
        Assert.IsNotEmpty(covers, "nenhuma capa baixada: " + summary);
        string[] notSmall = [.. covers.Where(c => !c.Url.Contains("-480.webp", StringComparison.Ordinal)).Select(c => c.Url)];
        Assert.IsEmpty(notSmall, "a lista só baixa a capa de 480 px: " + string.Join(", ", notSmall));
        Assert.IsTrue(document.ContentEncoding is "br" or "gzip", $"o HTML da lista sai comprimido (veio '{document.ContentEncoding}')");
    }

    [TestMethod]
    public async Task Detail_FirstLoad_IsUpTo3Mb_ForTheAdsOnTheFirstPageOfTheList()
    {
        Budgets budgets = Budgets.Current;
        (_, IPage listPage) = await OpenAsync(ListPath).ConfigureAwait(false);
        string[] links = [.. (await listPage.Locator("[data-ad-card] a.ad-card__link").EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('href'))").ConfigureAwait(false)).Take(8)];
        if (links.Length == 0)
        {
            Assert.Inconclusive("A categoria não tem anúncios; publique no banco do E2E.");
        }

        List<string> wrong = [];
        long heaviest = 0;
        foreach (string link in links)
        {
            (PageMeter meter, IPage page) = await OpenAsync(link).ConfigureAwait(false);
            heaviest = Math.Max(heaviest, meter.TotalBytes);
            long photoBytes = meter.Transfers.Where(t => t.Type == "Image" && t.Url.Contains("/fotos/", StringComparison.Ordinal)).Sum(t => t.Bytes);
            int photos = meter.Transfers.Count(t => t.Type == "Image" && t.Url.Contains("/fotos/", StringComparison.Ordinal));
            PerfLog.Write($"DETALHE {link}: {meter.TotalBytes / 1024.0:F0} KB (limite {budgets.DetailPageWeightMaxBytes / 1024} KB) · {photos} foto(s) baixada(s), {photoBytes / 1024.0:F0} KB · {Describe(meter.Transfers)}");
            long nonImage = meter.Transfers.Where(t => t.Type != "Image").Sum(t => t.Bytes);
            if (nonImage > budgets.NonImageAllowanceBytes)
            {
                wrong.Add($"{link}: o que não é foto passou da folga de {budgets.NonImageAllowanceBytes / 1024} KB ({nonImage / 1024.0:F0} KB)");
            }

            if (meter.TotalBytes > budgets.DetailPageWeightMaxBytes)
            {
                wrong.Add($"{link}: {meter.TotalBytes / 1024.0:F0} KB — {Describe(meter.Transfers)}");
            }

            int total = int.Parse(await page.Locator("[data-gallery]").First.GetAttributeAsync("data-total").ConfigureAwait(false) ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            if (total > 1)
            {
                int large = meter.Transfers.Count(t => Regex.IsMatch(t.Url, @"-1600\.webp"));
                if (large > 1)
                {
                    wrong.Add($"{link}: baixou {large} fotos grandes antes de qualquer toque (só a primeira deve vir)");
                }
            }

            await page.CloseAsync().ConfigureAwait(false);
        }

        Assert.IsEmpty(wrong, "peso do detalhe acima do orçamento:\n" + string.Join("\n", wrong));
        PerfLog.Write($"DETALHE mais pesado: {heaviest / 1024.0:F0} KB");
    }

    [TestMethod]
    public async Task VersionedFiles_HaveImmutableCache_OnEveryPublicPage()
    {
        // O que o navegador faz com o cabeçalho (guardar por um ano e não perguntar de novo) não dá para provar aqui: o site de teste usa certificado de desenvolvimento e o Chrome não guarda em cache
        // resposta de HTTPS com erro de certificado. Prova-se o cabeçalho em todo arquivo com ?v= que as páginas públicas baixam; o comportamento do navegador fica para o /verify, com o certificado de verdade.
        List<string> wrong = [];
        int seen = 0;
        foreach (string path in new[] { "/", ListPath, "/busca?q=livro", "/favoritos" })
        {
            (PageMeter meter, IPage page) = await OpenAsync(path).ConfigureAwait(false);
            IReadOnlyList<Transfer> versioned = [.. meter.Transfers.Where(t => t.Type is "Stylesheet" or "Script" or "Font" or "Image" && t.Url.Contains("?v=", StringComparison.Ordinal))];
            seen += versioned.Count;
            wrong.AddRange(versioned.Where(t => !t.CacheControl.Contains("immutable", StringComparison.Ordinal) || !t.CacheControl.Contains("max-age=31536000", StringComparison.Ordinal)).Select(t => $"{path}: {t.Url} ({t.CacheControl})"));
            await page.CloseAsync().ConfigureAwait(false);
        }

        Assert.IsGreaterThanOrEqualTo(12, seen, "as páginas carregam CSS e JS com ?v=");
        Assert.IsEmpty(wrong, "arquivo com ?v= sem cache imutável:\n" + string.Join("\n", wrong));
    }
}
