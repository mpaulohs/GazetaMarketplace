using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Layout;

/// <summary>Poppins, Font Awesome e a ausência de qualquer biblioteca do template Autolist (decisões de 2026-10-03).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class FontsAndThemeTests
#pragma warning restore CA1515
{
    private static readonly string[] _pages = ["/teste/publica", "/teste/painel", "/teste/estados", "/"];

    [TestMethod]
    [DataRow("400")]
    [DataRow("600")]
    [DataRow("700")]
    public async Task Poppins_Woff2_EhServidaPeloProprioSite(string peso)
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        string css = await client.GetStringAsync("/css/poppins.css");
        Match face = Regex.Match(css, @"font-weight:\s*" + peso + @";[\s\S]*?url\(""([^""]+\.woff2)""\)");
        Assert.IsTrue(face.Success, "sem @font-face da Poppins " + peso);

        // O endereço do CSS é relativo a /css/poppins.css
        string url = new Uri(new Uri("https://localhost/css/poppins.css"), face.Groups[1].Value).AbsolutePath;
        HttpResponseMessage response = await client.GetAsync(url);

        Assert.IsTrue(response.IsSuccessStatusCode, url + " respondeu " + (int)response.StatusCode);
        Assert.AreEqual("font/woff2", response.Content.Headers.ContentType?.MediaType);
        Assert.IsTrue((await response.Content.ReadAsByteArrayAsync()).Length > 5000, "arquivo vazio ou truncado");
    }

    [TestMethod]
    public async Task FontAwesome_Woff2_EhServidaPeloProprioSite()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        string css = await client.GetStringAsync("/lib/font-awesome/css/font-awesome.min.css");
        Match face = Regex.Match(css, @"url\('([^']+\.woff2)[?'][^)]*\)");
        Assert.IsTrue(face.Success, "o CSS do Font Awesome não cita o woff2");

        string url = new Uri(new Uri("https://localhost/lib/font-awesome/css/font-awesome.min.css"), face.Groups[1].Value).AbsolutePath;
        HttpResponseMessage response = await client.GetAsync(url);

        Assert.IsTrue(response.IsSuccessStatusCode, url + " respondeu " + (int)response.StatusCode);
        Assert.AreEqual("font/woff2", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task TodoWoff2DeTodoCssDaPagina_EhServidoLocalmente()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        foreach (string path in _pages)
        {
            string html = await client.GetStringAsync(path);
            foreach (Match sheet in Regex.Matches(html, @"href=""(/[^""#?]+\.css)"))
            {
                string css = await client.GetStringAsync(sheet.Groups[1].Value);
                Assert.AreEqual(0, Regex.Matches(css, @"url\(\s*['""]?(https?:)?//").Count, sheet.Groups[1].Value + ": fonte ou imagem externa");

                foreach (Match font in Regex.Matches(css, @"url\(\s*['""]?([^'"")?#]+\.woff2)"))
                {
                    string url = new Uri(new Uri("https://localhost" + sheet.Groups[1].Value), font.Groups[1].Value).AbsolutePath;
                    HttpResponseMessage response = await client.GetAsync(url);
                    Assert.IsTrue(response.IsSuccessStatusCode, path + ": " + url + " respondeu " + (int)response.StatusCode);
                }
            }
        }
    }

    [TestMethod]
    public async Task PreloadDasFontes_ApontaParaOProprioSite_ComCrossorigin()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        foreach (string path in _pages)
        {
            string html = await client.GetStringAsync(path);
            MatchCollection preloads = Regex.Matches(html, @"<link[^>]*rel=""preload""[^>]*>");

            Assert.IsTrue(preloads.Count >= 2, path + ": esperava o preload de Poppins 400 e 600");
            foreach (Match preload in preloads)
            {
                StringAssert.Matches(preload.Value, new Regex(@"href=""/lib/poppins/files/poppins-latin-(400|600)-normal\.woff2"""));
                StringAssert.Contains(preload.Value, "crossorigin");
                StringAssert.Contains(preload.Value, "type=\"font/woff2\"");
            }
        }
    }

    [TestMethod]
    public async Task Paginas_SoCarregamBootstrapFontAwesomeEOCssDoProjeto()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();
        string[] forbidden = ["autolist", "owl", "select2", "mcustomscrollbar", "ionicons", "materialdesign", "simple-line", "themify", "typicons", "linearicons", "weathericons", "glyphicon", "switcher", "jquery"];

        foreach (string path in _pages)
        {
            string html = await client.GetStringAsync(path);
            List<string> recursos = Regex.Matches(html, @"(?:href|src)=""([^""]+\.(?:css|js))(?:\?[^""]*)?""").Select(m => m.Groups[1].Value).ToList();

            Assert.IsTrue(recursos.Count >= 8, path + ": poucos recursos, a regex falhou?");
            foreach (string recurso in recursos)
            {
                string name = recurso.ToLowerInvariant();
                Assert.IsFalse(forbidden.Any(p => name.Contains(p, StringComparison.Ordinal)), path + " carrega " + recurso);
            }
        }
    }

    [TestMethod]
    public async Task IconesFontAwesome_SaoDecorativos_ComAriaHidden()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(WebFactory.RoleHeader, "Administrador");

        int total = 0;
        foreach (string path in _pages)
        {
            string html = await client.GetStringAsync(path);
            foreach (Match icon in Regex.Matches(html, @"<i\b[^>]*class=""[^""]*\bfa\b[^""]*""[^>]*>"))
            {
                total++;
                StringAssert.Contains(icon.Value, "aria-hidden=\"true\"", path + ": ícone sem aria-hidden: " + icon.Value);
            }
        }

        Assert.IsTrue(total >= 4, "esperava ícones no cabeçalho e no painel; a regex falhou?");
    }

    [TestMethod]
    public async Task OutrasFamiliasDeIcones_NaoExistemNoSite()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        foreach (string path in _pages)
        {
            string html = await client.GetStringAsync(path);
            Assert.AreEqual(0, Regex.Matches(html, @"<i\b[^>]*class=""[^""]*\b(?:mdi|typcn|ti|si|zmdi|wi|fe|lnr|ion|glyphicon|icon)-[^""]*""").Count, path + ": ícone de outra família");
        }
    }
}
