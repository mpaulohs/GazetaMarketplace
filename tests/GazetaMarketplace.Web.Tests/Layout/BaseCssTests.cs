using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
    [DataRow("--bs-primary: #d72213;")]
    [DataRow("--bs-body-font-family: \"Poppins\", system-ui")]
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
        StringAssert.Matches(css, new Regex(@"a:where\(:not\(\.btn\):not\(\.nav-link\):not\(\.page-link\)\) \{ text-decoration: underline; \}"));
    }

    [TestMethod]
    public void BtnPrimary_DeclaraSuasVariaveisProprias()
    {
        string css = Normalizar(BaseCss());

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
        double foco = Contraste("#0a58ca", "#ffffff");
        double borda = Contraste("#6c757d", "#ffffff");

        Assert.IsTrue(foco >= 3.0 && Math.Abs(foco - 6.44) < 0.05, "foco " + foco.ToString("0.00", CultureInfo.InvariantCulture));
        Assert.IsTrue(borda >= 3.0 && Math.Abs(borda - 4.69) < 0.05, "borda " + borda.ToString("0.00", CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Primaria_ComTextoBranco_PassaAA_ComOsValoresDeclaradosNoBaseCss()
    {
        string css = Normalizar(BaseCss());
        string primaria = Token(css, "--bs-primary");

        Assert.AreEqual("#d72213", primaria);
        double contraste = Contraste("#ffffff", primaria);
        Assert.IsTrue(contraste >= 4.5 && Math.Abs(contraste - 5.09) < 0.05, "branco sobre a primária: " + contraste.ToString("0.00", CultureInfo.InvariantCulture));
        Assert.IsTrue(Contraste("#ffffff", Token(css, "--bs-btn-hover-bg", "#b81d10")) >= 4.5, "botão primário em hover");
    }

    [TestMethod]
    public void PrimariaOriginalDoTemplate_FalhariaOAA()
    {
        // Documenta por que #e72a1a foi trocada: o teste quebra se alguém a devolver
        Assert.IsTrue(Contraste("#ffffff", "#e72a1a") < 4.5);
        Assert.IsTrue(Contraste("#6d7e9c", "#ffffff") < 4.5);
    }

    [TestMethod]
    public void ParesDeCorDoTema_PassamNoContrasteMinimo_LendoOBaseCss()
    {
        string css = Normalizar(BaseCss());
        string pagina = Token(css, "--app-page-bg");
        string degrade = Token(css, "--app-gradient-header", string.Empty);
        List<string> pontasDoDegrade = [.. Regex.Matches(degrade, "#[0-9a-fA-F]{6}").Select(m => m.Value)];

        Assert.AreEqual(2, pontasDoDegrade.Count, "o degradê tem duas cores");
        foreach (string ponta in pontasDoDegrade)
        {
            Assert.IsTrue(Contraste("#ffffff", ponta) >= 4.5, "branco sobre " + ponta);
        }

        (string nome, string texto, string fundo, double minimo)[] pares =
        [
            ("texto", Token(css, "--bs-body-color"), pagina, 4.5),
            ("texto secundário na página", Token(css, "--bs-secondary-color"), pagina, 4.5),
            ("texto secundário no branco", Token(css, "--bs-secondary-color"), "#ffffff", 4.5),
            ("link", Token(css, "--bs-link-color"), pagina, 4.5),
            ("link em hover", Token(css, "--bs-link-hover-color"), pagina, 4.5),
            ("code", Token(css, "--bs-code-color"), pagina, 4.5),
            ("rodapé", Token(css, "--app-footer-color"), Token(css, "--app-footer-bg"), 4.5),
            ("primária como texto no branco", Token(css, "--bs-primary"), "#ffffff", 4.5),
            ("foco na página", Regex.Match(Token(css, "--app-focus-outline", string.Empty), "#[0-9a-fA-F]{6}").Value, pagina, 3.0),
            ("foco no branco", Regex.Match(Token(css, "--app-focus-outline", string.Empty), "#[0-9a-fA-F]{6}").Value, "#ffffff", 3.0),
            ("borda de campo na página", Token(css, "--app-input-border-color"), pagina, 3.0),
            ("accent", Token(css, "--app-accent"), "#ffffff", 4.5)
        ];

        foreach ((string nome, string texto, string fundo, double minimo) in pares)
        {
            double contraste = Contraste(texto, fundo);
            Assert.IsTrue(contraste >= minimo, $"{nome}: {texto} sobre {fundo} = {contraste.ToString("0.00", CultureInfo.InvariantCulture)}:1 (mínimo {minimo})");
        }
    }

    [TestMethod]
    public void FocoSobreFundoEscuro_EBranco()
    {
        string css = Normalizar(BaseCss());

        StringAssert.Contains(css, "--app-on-dark-focus: 2px solid #fff;");
        Assert.IsTrue(Normalizar(File.ReadAllText(RaizDoRepositorio.Wwwroot("css", "components", "layout.css")))
            .Contains(".cabecalho :focus-visible, .rodape :focus-visible { outline: var(--app-on-dark-focus); }", StringComparison.Ordinal));
    }

    private static string Token(string cssNormalizado, string nome, string padrao = null)
    {
        Match m = Regex.Match(cssNormalizado, Regex.Escape(nome) + @":\s*([^;]+);");
        return m.Success
            ? m.Groups[1].Value.Trim()
            : padrao ?? throw new InvalidOperationException("token ausente: " + nome);
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
