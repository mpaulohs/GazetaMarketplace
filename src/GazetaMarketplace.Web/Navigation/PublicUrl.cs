using GazetaMarketplace.Core.Configuration;
using Microsoft.AspNetCore.Http;

namespace GazetaMarketplace.Web.Navigation;

/// <summary>O endereço completo de uma página pública: o do site configurado (<c>Site:BaseUrl</c>, obrigatório em Production) e, sem ele, o do próprio pedido. Vale para o <c>canonical</c> e para a mensagem do WhatsApp.</summary>
public static class PublicUrl
{
    public static string Absolute(HttpRequest request, SiteOptions site, string path) =>
        (string.IsNullOrWhiteSpace(site.BaseUrl) ? $"{request.Scheme}://{request.Host}" : site.BaseUrl.TrimEnd('/')) + path;
}
