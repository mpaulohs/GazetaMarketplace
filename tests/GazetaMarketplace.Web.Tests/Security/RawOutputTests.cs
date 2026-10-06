using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

/// <summary>
/// NFR-15, a rede de baixo da prova de texto hostil: nenhuma tela pode ter um caminho para escrever texto sem codificar. A varredura procura, nos arquivos <c>.cshtml</c> e no código do site,
/// as saídas "cruas" do Razor e as maneiras de montar HTML na mão, e o JavaScript solto na página. A lista de exceções é <b>vazia</b>: quem precisar de uma tem de decidir isso aqui, à vista.
/// </summary>
[TestClass]
public sealed class RawOutputTests
{
    private static readonly string[] Exceptions = [];

    private static readonly Regex RawOutput = new(@"Html\.Raw|\bHtmlString\b|\bMarkupString\b|\.AppendHtml\s*\(|\.SetHtmlContent\s*\(|\bHtmlContentBuilder\b|\bHtmlHelper\.Raw", RegexOptions.CultureInvariant);

    private static readonly Regex InlineScript = new(@"<script(?![^>]*\bsrc=)(?![^>]*type=""(?:application/json|application/ld\+json)"")[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex InlineHandler = new(@"<[a-zA-Z][^>]*\s(?:on[a-z]+)\s*=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex JavascriptUrl = new(@"(?:href|src|action|formaction)\s*=\s*[""']\s*javascript:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static string[] Views() => [.. Directory.GetFiles(RepositoryRoot.FullPath("src", "GazetaMarketplace.Web"), "*.cshtml", SearchOption.AllDirectories).Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))];

    private static string[] Sources() => [.. new[] { "GazetaMarketplace.Web", "GazetaMarketplace.Core", "GazetaMarketplace.Infrastructure" }
        .SelectMany(project => Directory.GetFiles(RepositoryRoot.FullPath("src", project), "*.cs", SearchOption.AllDirectories))
        .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))];

    // Tira os comentários Razor (@* ... *@) e os C# (// ...): um comentário que cita Html.Raw para dizer "nunca usar" não é uso
    private static string WithoutComments(string text)
    {
        text = Regex.Replace(text, @"@\*[\s\S]*?\*@", string.Empty);
        text = Regex.Replace(text, @"<!--[\s\S]*?-->", string.Empty);
        return Regex.Replace(text, @"(?m)^\s*//.*$|///.*$", string.Empty);
    }

    private static string Relative(string file) => Path.GetRelativePath(RepositoryRoot.FullPath("src"), file).Replace('\\', '/');

    [TestMethod]
    public void NoView_WritesTextWithoutEncoding()
    {
        string[] views = Views();
        Assert.IsGreaterThanOrEqualTo(40, views.Length, "a varredura achou poucas views: " + views.Length);

        string[] found = [.. views.Where(v => RawOutput.IsMatch(WithoutComments(File.ReadAllText(v)))).Select(Relative).Except(Exceptions)];

        Assert.IsEmpty(found, "saída crua do Razor (Html.Raw e parentes) em: " + string.Join(", ", found));
    }

    [TestMethod]
    public void NoSourceFile_BuildsHtmlByHand()
    {
        string[] sources = Sources();
        Assert.IsGreaterThanOrEqualTo(100, sources.Length, "a varredura achou poucos arquivos: " + sources.Length);

        string[] found = [.. sources.Where(s => !s.EndsWith("RawOutputTests.cs", StringComparison.Ordinal) && RawOutput.IsMatch(WithoutComments(File.ReadAllText(s)))).Select(Relative).Except(Exceptions)];

        Assert.IsEmpty(found, "HTML montado à mão (HtmlString, AppendHtml…) em: " + string.Join(", ", found));
    }

    [TestMethod]
    public void NoView_HasInlineScriptInlineHandlerOrJavascriptUrl()
    {
        List<string> wrong = [];
        foreach (string view in Views())
        {
            string text = WithoutComments(File.ReadAllText(view));
            if (InlineScript.IsMatch(text))
            {
                wrong.Add($"{Relative(view)}: <script> com código na página");
            }

            if (InlineHandler.IsMatch(text))
            {
                wrong.Add($"{Relative(view)}: atributo on…= (manipulador de evento) na página");
            }

            if (JavascriptUrl.IsMatch(text))
            {
                wrong.Add($"{Relative(view)}: endereço javascript: fixo na página");
            }
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
    }

    [TestMethod]
    public void TheScan_RecognizesTheCasesItLooksFor()
    {
        // Prova que as expressões não ficaram cegas: cada forma de risco é reconhecida, e o texto comum não
        string[] risky = ["@Html.Raw(x)", "new HtmlString(x)", "builder.AppendHtml(x)", "output.Content.SetHtmlContent(x)", "new MarkupString(x)", "new HtmlContentBuilder()"];
        foreach (string text in risky)
        {
            Assert.IsTrue(RawOutput.IsMatch(text), text);
        }

        Assert.IsFalse(RawOutput.IsMatch("<p>@Model.Title</p> @Html.AntiForgeryToken()"));
        Assert.IsTrue(InlineScript.IsMatch("<script>alert(1)</script>"));
        Assert.IsFalse(InlineScript.IsMatch("<script type=\"module\" src=\"/js/a.js\"></script>"));
        Assert.IsFalse(InlineScript.IsMatch("<script type=\"application/json\">{}</script>"));
        Assert.IsTrue(InlineHandler.IsMatch("<button onclick=\"x()\">"));
        Assert.IsFalse(InlineHandler.IsMatch("<button data-action=\"x\" aria-label=\"menu\">"));
        Assert.IsTrue(JavascriptUrl.IsMatch("<a href=\"javascript:void(0)\">"));
        Assert.AreEqual(string.Empty, WithoutComments("@* Html.Raw *@").Trim());
    }
}
