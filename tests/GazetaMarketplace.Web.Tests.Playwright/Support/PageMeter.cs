using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace GazetaMarketplace.Web.Tests.Playwright.Support;

/// <summary>Um arquivo que a página baixou: endereço, tipo (Document, Stylesheet, Script, Image, Font...), bytes que trafegaram (cabeçalhos e corpo, já comprimidos) e os cabeçalhos que importam.</summary>
internal sealed record Transfer(string Url, string Type, long Bytes, int Status, string CacheControl, string ContentEncoding);

/// <summary>Mede o que a página custa na rede, como o navegador a vê (protocolo do Chrome), e prepara o perfil de celular das métricas (rede 4G e processador mais lento).</summary>
internal sealed class PageMeter
{
    private readonly Dictionary<string, (string Url, string Type, int Status, string CacheControl, string ContentEncoding)> _responses = [];
    private readonly Dictionary<string, long> _bytes = [];

    private PageMeter()
    {
    }

    public IReadOnlyList<Transfer> Transfers => [.. _bytes.Where(b => _responses.ContainsKey(b.Key)).Select(b =>
    {
        (string url, string type, int status, string cache, string encoding) = _responses[b.Key];
        return new Transfer(url, type, b.Value, status, cache, encoding);
    })];

    public long TotalBytes => Transfers.Sum(t => t.Bytes);

    /// <summary>Contexto de celular médio: janela 412×823, toque, escala 2,625 (um Android de faixa média).</summary>
    public static BrowserNewContextOptions MobileContext() => new()
    {
        IgnoreHTTPSErrors = true,
        ViewportSize = new ViewportSize { Width = 412, Height = 823 },
        DeviceScaleFactor = 2.625f,
        IsMobile = true,
        HasTouch = true,
        UserAgent = "Mozilla/5.0 (Linux; Android 11; Moto G Power) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Mobile Safari/537.36"
    };

    /// <summary>Liga a escuta de rede na página. Com <paramref name="throttle"/>, aplica a rede 4G (1,6 Mbit/s, 150 ms, o perfil móvel padrão do Lighthouse) e o processador 4 vezes mais lento.</summary>
    public static async Task<PageMeter> AttachAsync(IPage page, bool throttle = false)
    {
        PageMeter meter = new();
        ICDPSession session = await page.Context.NewCDPSessionAsync(page).ConfigureAwait(false);
        session.Event("Network.responseReceived").OnEvent += (_, e) => meter.OnResponse(e);
        session.Event("Network.loadingFinished").OnEvent += (_, e) => meter.OnFinished(e);
        await session.SendAsync("Network.enable").ConfigureAwait(false);
        if (throttle)
        {
            await session.SendAsync("Network.emulateNetworkConditions", new Dictionary<string, object>
            {
                ["offline"] = false,
                ["latency"] = 150,
                ["downloadThroughput"] = 1.6 * 1024 * 1024 / 8,
                ["uploadThroughput"] = 750 * 1024 / 8.0
            }).ConfigureAwait(false);
            await session.SendAsync("Emulation.setCPUThrottlingRate", new Dictionary<string, object> { ["rate"] = 4 }).ConfigureAwait(false);
        }

        return meter;
    }

    private void OnResponse(JsonElement? e)
    {
        if (e is not { } json)
        {
            return;
        }

        JsonElement response = json.GetProperty("response");
        string id = json.GetProperty("requestId").GetString();
        string Header(string name) => response.TryGetProperty("headers", out JsonElement headers)
            ? headers.EnumerateObject().FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.OrdinalIgnoreCase)).Value.ToString()
            : string.Empty;
        lock (_responses)
        {
            _responses[id] = (response.GetProperty("url").GetString(), json.GetProperty("type").GetString(), response.GetProperty("status").GetInt32(), Header("cache-control"), Header("content-encoding"));
        }
    }

    private void OnFinished(JsonElement? e)
    {
        if (e is not { } json)
        {
            return;
        }

        lock (_responses)
        {
            _bytes[json.GetProperty("requestId").GetString()!] = (long)json.GetProperty("encodedDataLength").GetDouble();
        }
    }
}
