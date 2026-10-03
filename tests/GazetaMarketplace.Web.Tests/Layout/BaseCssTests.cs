using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Layout;

/// <summary>Confere o base.css contra architecture/design-system.md §2.1 (a medição no navegador fica no projeto Playwright).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class BaseCssTests
#pragma warning restore CA1515
{
    private static string BaseCss() => File.ReadAllText(RepositoryRoot.Wwwroot("css", "base.css"));

    [TestMethod]
    [DataRow("--app-focus-outline: 2px solid #0a58ca;")]
    [DataRow("--app-focus-offset: 2px;")]
    [DataRow("--app-input-border-color: #6c757d;")]
    [DataRow("--app-media-ratio: 4 / 3;")]
    [DataRow("--app-touch-target: 44px;")]
    [DataRow("--bs-primary: #d72213;")]
    [DataRow("--bs-body-font-family: \"Poppins\", system-ui")]
    public void TokensDoDesignSystem_EstaoNoBaseCss(string section)
    {
        StringAssert.Contains(Normalize(BaseCss()), section);
    }

    [TestMethod]
    public void TresAjustesDeAcessibilidade_EstaoAplicados()
    {
        string css = Normalize(BaseCss());

        StringAssert.Matches(css, new Regex(@":focus-visible \{[^}]*outline: var\(--app-focus-outline\);[^}]*outline-offset: var\(--app-focus-offset\);[^}]*box-shadow: none;"));
        StringAssert.Matches(css, new Regex(@"\.form-control, \.form-select, \.form-check-input \{ border-color: var\(--app-input-border-color\); \}"));
        StringAssert.Matches(css, new Regex(@"a:where\(:not\(\.btn\):not\(\.nav-link\):not\(\.page-link\)\) \{ text-decoration: underline; \}"));
    }

    [TestMethod]
    public void BtnPrimary_DeclaraSuasVariaveisProprias()
    {
        string css = Normalize(BaseCss());

        StringAssert.Contains(css, "--bs-btn-bg: var(--bs-primary);");
        StringAssert.Contains(css, "--bs-btn-hover-bg: #b81d10;");
    }

    [TestMethod]
    public void BaseCss_NaoImportaNemCarregaNadaDeFora()
    {
        string css = BaseCss();

        Assert.IsFalse(Regex.IsMatch(css, @"@import|@font-face|url\("), "sem fontes, imports nem imagens externas");
    }

    [TestMethod]
    public void ContrasteDoFoco_EDaBordaDeCampo_PassaWcag()
    {
        double focus = Contrast("#0a58ca", "#ffffff");
        double border = Contrast("#6c757d", "#ffffff");

        Assert.IsTrue(focus >= 3.0 && Math.Abs(focus - 6.44) < 0.05, "foco " + focus.ToString("0.00", CultureInfo.InvariantCulture));
        Assert.IsTrue(border >= 3.0 && Math.Abs(border - 4.69) < 0.05, "borda " + border.ToString("0.00", CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Primaria_ComTextoBranco_PassaAA_ComOsValoresDeclaradosNoBaseCss()
    {
        string css = Normalize(BaseCss());
        string primary = Token(css, "--bs-primary");

        Assert.AreEqual("#d72213", primary);
        double contrast = Contrast("#ffffff", primary);
        Assert.IsTrue(contrast >= 4.5 && Math.Abs(contrast - 5.09) < 0.05, "branco sobre a primária: " + contrast.ToString("0.00", CultureInfo.InvariantCulture));
        Assert.IsTrue(Contrast("#ffffff", Token(css, "--bs-btn-hover-bg", "#b81d10")) >= 4.5, "botão primário em hover");
    }

    [TestMethod]
    public void PrimariaOriginalDoTemplate_FalhariaOAA()
    {
        // Documenta por que #e72a1a foi trocada: o teste quebra se alguém a devolver
        Assert.IsTrue(Contrast("#ffffff", "#e72a1a") < 4.5);
        Assert.IsTrue(Contrast("#6d7e9c", "#ffffff") < 4.5);
    }

    [TestMethod]
    public void ParesDeCorDoTema_PassamNoContrasteMinimo_LendoOBaseCss()
    {
        string css = Normalize(BaseCss());
        string page = Token(css, "--app-page-bg");
        string gradient = Token(css, "--app-gradient-header", string.Empty);
        List<string> gradientStops = [.. Regex.Matches(gradient, "#[0-9a-fA-F]{6}").Select(m => m.Value)];

        Assert.AreEqual(2, gradientStops.Count, "o degradê tem duas cores");
        foreach (string stop in gradientStops)
        {
            Assert.IsTrue(Contrast("#ffffff", stop) >= 4.5, "branco sobre " + stop);
        }

        (string name, string text, string background, double minimum)[] pairs =
        [
            ("texto", Token(css, "--bs-body-color"), page, 4.5),
            ("texto secundário na página", Token(css, "--bs-secondary-color"), page, 4.5),
            ("texto secundário no branco", Token(css, "--bs-secondary-color"), "#ffffff", 4.5),
            ("link", Token(css, "--bs-link-color"), page, 4.5),
            ("link em hover", Token(css, "--bs-link-hover-color"), page, 4.5),
            ("code", Token(css, "--bs-code-color"), page, 4.5),
            ("rodapé", Token(css, "--app-footer-color"), Token(css, "--app-footer-bg"), 4.5),
            ("primária como texto no branco", Token(css, "--bs-primary"), "#ffffff", 4.5),
            ("foco na página", Regex.Match(Token(css, "--app-focus-outline", string.Empty), "#[0-9a-fA-F]{6}").Value, page, 3.0),
            ("foco no branco", Regex.Match(Token(css, "--app-focus-outline", string.Empty), "#[0-9a-fA-F]{6}").Value, "#ffffff", 3.0),
            ("borda de campo na página", Token(css, "--app-input-border-color"), page, 3.0),
            ("accent", Token(css, "--app-accent"), "#ffffff", 4.5)
        ];

        foreach ((string name, string text, string background, double minimum) in pairs)
        {
            double contrast = Contrast(text, background);
            Assert.IsTrue(contrast >= minimum, $"{name}: {text} sobre {background} = {contrast.ToString("0.00", CultureInfo.InvariantCulture)}:1 (mínimo {minimum})");
        }
    }

    [TestMethod]
    public void FocoSobreFundoEscuro_EBranco()
    {
        string css = Normalize(BaseCss());

        StringAssert.Contains(css, "--app-on-dark-focus: 2px solid #fff;");
        Assert.IsTrue(Normalize(File.ReadAllText(RepositoryRoot.Wwwroot("css", "components", "layout.css")))
            .Contains(".cabecalho :focus-visible, .rodape :focus-visible { outline: var(--app-on-dark-focus); }", StringComparison.Ordinal));
    }

    private static string Token(string normalizedCss, string name, string defaultValue = null)
    {
        Match m = Regex.Match(normalizedCss, Regex.Escape(name) + @":\s*([^;]+);");
        return m.Success
            ? m.Groups[1].Value.Trim()
            : defaultValue ?? throw new InvalidOperationException("token ausente: " + name);
    }

    private static string Normalize(string css)
    {
        string withoutComments = Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutComments, @"\s+", " ");
    }

    private static double Contrast(string a, string b)
    {
        double la = Luminance(a);
        double lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(string hex)
    {
        List<double> channels = [];
        for (int i = 1; i < 7; i += 2)
        {
            double c = int.Parse(hex.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            channels.Add(c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4));
        }

        return (0.2126 * channels[0]) + (0.7152 * channels[1]) + (0.0722 * channels[2]);
    }
}
