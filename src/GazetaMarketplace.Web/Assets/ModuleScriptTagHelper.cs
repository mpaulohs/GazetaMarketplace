using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace GazetaMarketplace.Web.Assets;

/// <summary>
/// <c>&lt;script type="module" asp-module-version="~/js/pages/search.js"&gt;</c>: escreve o <c>src</c> com o <c>?v=</c> que cobre a página e os módulos que ela importa (<see cref="ModuleVersions"/>).
/// Substitui o <c>asp-append-version</c> nos scripts de página.
/// </summary>
[HtmlTargetElement("script", Attributes = AttributeName)]
public sealed class ModuleScriptTagHelper(IModuleVersions versions, IUrlHelperFactory urlHelperFactory) : TagHelper
{
    private const string AttributeName = "asp-module-version";

    [HtmlAttributeName(AttributeName)]
    public string Path { get; set; }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        IUrlHelper url = urlHelperFactory.GetUrlHelper(ViewContext);
        output.Attributes.SetAttribute("src", url.Content(Path) + "?v=" + versions.For(Path));
    }
}
