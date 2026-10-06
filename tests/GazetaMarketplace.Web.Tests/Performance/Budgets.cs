using System;
using System.IO;
using System.Text.Json;

namespace GazetaMarketplace.Performance;

/// <summary>Os limites de desempenho de <c>Performance/budgets.json</c> (NFR-01 a NFR-05). O arquivo e esta classe são compartilhados pelos três projetos de teste.</summary>
internal sealed class Budgets
{
    private static readonly Lazy<Budgets> Instance = new(() => JsonSerializer.Deserialize<Budgets>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Performance", "budgets.json")), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));

    public static Budgets Current => Instance.Value;

    public long ListPageWeightMaxBytes { get; set; }

    public long DetailPageWeightMaxBytes { get; set; }

    public int ListCardCount { get; set; }

    public int VolumeActiveAds { get; set; }

    public double ServerP95MaxMs { get; set; }

    public int WarmupRequests { get; set; }

    public int MeasuredRequests { get; set; }

    public double LcpMaxMs { get; set; }

    public double InpMaxMs { get; set; }

    public double ClsMax { get; set; }
}
