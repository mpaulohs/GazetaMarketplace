namespace GazetaMarketplace.Core.Ads;

/// <summary>Uma linha "rótulo: valor" das características do anúncio, já em texto pronto para mostrar.</summary>
public sealed record AdSpec(string Label, string Value);
