using System;
using System.Linq;
using System.Threading.Tasks;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace GazetaMarketplace.Web.Tests.Playwright.Support;

/// <summary>NFR-16: roda o axe-core na tela aberta com as regras do WCAG 2.1 níveis A e AA e devolve uma linha por violação (regra, impacto, o que fazer e os primeiros elementos).</summary>
internal static class AxeHelper
{
    /// <summary>As etiquetas do axe que valem como "0 falhas de nível A ou AA" da NFR-16.</summary>
    internal static readonly string[] Tags = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"];

    public static async Task<string[]> ViolationsAsync(IPage page)
    {
        AxeResult result = await page.RunAxe(new AxeRunOptions { RunOnly = new RunOnlyOptions { Type = "tag", Values = [.. Tags] } }).ConfigureAwait(false);
        return [.. result.Violations.Select(Describe)];
    }

    private static string Describe(AxeResultItem violation)
    {
        string elements = string.Join(" | ", violation.Nodes.Take(3).Select(n => Shorten(n.Html)));
        return $"{violation.Id} ({violation.Impact}): {violation.Help} — {violation.Nodes.Length} elemento(s): {elements}";
    }

    private static string Shorten(string html) => html.Length <= 140 ? html : html[..140] + "…";
}
