using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace GazetaMarketplace.Web.Tests.Playwright.Support;

/// <summary>NFR-17: a tela não rola na horizontal em 320, 768, 1024 e 1280 px. Abre a tela de novo em cada largura (o que a tela mostra depende dela) e, se a página estoura, aponta os elementos que passam da borda.</summary>
internal static class WidthHelper
{
    internal static readonly int[] Widths = [320, 768, 1024, 1280];

    private const int Height = 900;

    private const string Measure = """
        () => {
          const root = document.documentElement;
          const width = root.clientWidth;
          const overflow = root.scrollWidth > width;
          const offenders = [];
          if (overflow) {
            const scrolls = (el) => { for (let p = el.parentElement; p && p !== document.body; p = p.parentElement) { if (getComputedStyle(p).overflowX !== 'visible') return true; } return false; };
            for (const el of document.body.querySelectorAll('*')) {
              const r = el.getBoundingClientRect();
              if (r.width > 0 && r.right > width + 1 && !scrolls(el)) {
                offenders.push(el.tagName.toLowerCase() + (el.id ? '#' + el.id : '') + (el.className && typeof el.className === 'string' ? '.' + el.className.trim().split(/\s+/).slice(0, 2).join('.') : '') + ' (direita em ' + Math.round(r.right) + ')');
                if (offenders.length >= 3) break;
              }
            }
          }
          return { overflow, scrollWidth: root.scrollWidth, width, offenders };
        }
        """;

    /// <summary>Uma linha por largura em que a tela rola na horizontal (vazio = todas as larguras cabem). <paramref name="open"/> abre e prepara a tela na largura já ajustada.</summary>
    public static async Task<string[]> OverflowsAsync(IPage page, Func<Task> open)
    {
        List<string> wrong = [];
        foreach (int width in Widths)
        {
            await page.SetViewportSizeAsync(width, Height).ConfigureAwait(false);
            await open().ConfigureAwait(false);
            Measurement measurement = await page.EvaluateAsync<Measurement>(Measure).ConfigureAwait(false);
            if (measurement.Overflow)
            {
                wrong.Add($"{width} px: a página tem {measurement.ScrollWidth} px de largura para {measurement.Width} px de janela; passam da borda: {string.Join("; ", measurement.Offenders)}");
            }
        }

        return [.. wrong];
    }

    internal sealed class Measurement
    {
        public bool Overflow { get; set; }

        public int ScrollWidth { get; set; }

        public int Width { get; set; }

        public string[] Offenders { get; set; } = [];
    }
}
