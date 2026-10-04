using System.Collections.Generic;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Web.Models;

namespace GazetaMarketplace.Web.Areas.Panel.Models;

/// <summary>Os exemplos da página de componentes (só Development).</summary>
public sealed class ComponentsViewModel
{
    public IReadOnlyList<AdCardModel> Cards { get; init; } = [];

    public AdBodyViewModel Body { get; init; }
}
