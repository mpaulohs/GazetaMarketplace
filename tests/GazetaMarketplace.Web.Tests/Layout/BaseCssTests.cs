using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Layout;

/// <summary>Confere o base.css contra architecture/design-system.md §2.1 (a medição no navegador fica no projeto Playwright).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class BaseCssTests
#pragma warning restore CA1515
{
    private static string BaseCss() => File.ReadAllText(RaizDoRepositorio.Wwwroot("css", "base.css"));

    [TestMethod]
    [DataRow("--app-focus-outline: 2px solid #0a58ca;")]
    [DataRow("--app-focus-offset: 2px;")]
    [DataRow("--app-input-border-color: #6c757d;")]
    [DataRow("--app-media-ratio: 4 / 3;")]
    [DataRow("--app-touch-target: 44px;")]
    [DataRow("--bs-primary: #0d6efd;")]
    [DataRow("--bs-body-font-family: system-ui")]
    public void TokensDoDesignSystem_EstaoNoBaseCss(string trecho)
    {
        StringAssert.Contains(Normalizar(BaseCss()), trecho);
    }

    [TestMethod]
    public void TresAjustesDeAcessibilidade_EstaoAplicados()
    {
        string css = Normalizar(BaseCss());

        StringAssert.Matches(css, new Regex(@":focus-visible \{[^}]*outline: var\(--app-focus-outline\);[^}]*outline-offset: var\(--app-focus-offset\);[^}]*box-shadow: none;"));
        StringAssert.Matches(css, new Regex(@"\.form-control, \.form-select, \.form-check-input \{ border-color: var\(--app-input-border-color\); \}"));
        StringAssert.Matches(css, new Regex(@"a:not\(\.btn\):not\(\.nav-link\):not\(\.page-link\) \{ text-decoration: underline; \}"));
    }

    [TestMethod]
    public void BtnPrimary_DeclaraSuasVariaveisProprias()
    {
        string css = Normalizar(BaseCss());

        StringAssert.Contains(css, "--bs-btn-bg: var(--bs-primary);");
        StringAssert.Contains(css, "--bs-btn-hover-bg: #0b5ed7;");
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
        double foco = Contraste("#0a58ca", "#ffffff");
        double borda = Contraste("#6c757d", "#ffffff");

        Assert.IsTrue(foco >= 3.0 && Math.Abs(foco - 6.44) < 0.05, "foco " + foco.ToString("0.00", CultureInfo.InvariantCulture));
        Assert.IsTrue(borda >= 3.0 && Math.Abs(borda - 4.69) < 0.05, "borda " + borda.ToString("0.00", CultureInfo.InvariantCulture));
    }

    private static string Normalizar(string css)
    {
        string semComentarios = Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(semComentarios, @"\s+", " ");
    }

    private static double Contraste(string a, string b)
    {
        double la = Luminancia(a);
        double lb = Luminancia(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminancia(string hex)
    {
        List<double> canais = [];
        for (int i = 1; i < 7; i += 2)
        {
            double c = int.Parse(hex.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            canais.Add(c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4));
        }

        return (0.2126 * canais[0]) + (0.7152 * canais[1]) + (0.0722 * canais[2]);
    }
}
