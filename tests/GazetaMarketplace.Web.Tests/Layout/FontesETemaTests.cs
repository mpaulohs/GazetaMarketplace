using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Layout;

/// <summary>Poppins, Font Awesome e a ausência de qualquer biblioteca do template Autolist (decisões de 2026-10-03).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class FontesETemaTests
#pragma warning restore CA1515
{
    private static readonly string[] _paginas = ["/teste/publica", "/teste/painel", "/teste/estados", "/"];

    [TestMethod]
    [DataRow("400")]
    [DataRow("600")]
    [DataRow("700")]
    public async Task Poppins_Woff2_EhServidaPeloProprioSite(string peso)
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        string css = await cliente.GetStringAsync("/css/poppins.css");
        Match face = Regex.Match(css, @"font-weight:\s*" + peso + @";[\s\S]*?url\(""([^""]+\.woff2)""\)");
        Assert.IsTrue(face.Success, "sem @font-face da Poppins " + peso);

        // O endereço do CSS é relativo a /css/poppins.css
        string url = new Uri(new Uri("https://localhost/css/poppins.css"), face.Groups[1].Value).AbsolutePath;
        HttpResponseMessage resposta = await cliente.GetAsync(url);

        Assert.IsTrue(resposta.IsSuccessStatusCode, url + " respondeu " + (int)resposta.StatusCode);
        Assert.AreEqual("font/woff2", resposta.Content.Headers.ContentType?.MediaType);
        Assert.IsTrue((await resposta.Content.ReadAsByteArrayAsync()).Length > 5000, "arquivo vazio ou truncado");
    }

    [TestMethod]
    public async Task FontAwesome_Woff2_EhServidaPeloProprioSite()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        string css = await cliente.GetStringAsync("/lib/font-awesome/css/font-awesome.min.css");
        Match face = Regex.Match(css, @"url\('([^']+\.woff2)[?'][^)]*\)");
        Assert.IsTrue(face.Success, "o CSS do Font Awesome não cita o woff2");

        string url = new Uri(new Uri("https://localhost/lib/font-awesome/css/font-awesome.min.css"), face.Groups[1].Value).AbsolutePath;
        HttpResponseMessage resposta = await cliente.GetAsync(url);

        Assert.IsTrue(resposta.IsSuccessStatusCode, url + " respondeu " + (int)resposta.StatusCode);
        Assert.AreEqual("font/woff2", resposta.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task TodoWoff2DeTodoCssDaPagina_EhServidoLocalmente()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        foreach (string caminho in _paginas)
        {
            string html = await cliente.GetStringAsync(caminho);
            foreach (Match folha in Regex.Matches(html, @"href=""(/[^""#?]+\.css)"))
            {
                string css = await cliente.GetStringAsync(folha.Groups[1].Value);
                Assert.AreEqual(0, Regex.Matches(css, @"url\(\s*['""]?(https?:)?//").Count, folha.Groups[1].Value + ": fonte ou imagem externa");

                foreach (Match fonte in Regex.Matches(css, @"url\(\s*['""]?([^'"")?#]+\.woff2)"))
                {
                    string url = new Uri(new Uri("https://localhost" + folha.Groups[1].Value), fonte.Groups[1].Value).AbsolutePath;
                    HttpResponseMessage resposta = await cliente.GetAsync(url);
                    Assert.IsTrue(resposta.IsSuccessStatusCode, caminho + ": " + url + " respondeu " + (int)resposta.StatusCode);
                }
            }
        }
    }

    [TestMethod]
    public async Task PreloadDasFontes_ApontaParaOProprioSite_ComCrossorigin()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        foreach (string caminho in _paginas)
        {
            string html = await cliente.GetStringAsync(caminho);
            MatchCollection preloads = Regex.Matches(html, @"<link[^>]*rel=""preload""[^>]*>");

            Assert.IsTrue(preloads.Count >= 2, caminho + ": esperava o preload de Poppins 400 e 600");
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
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();
        string[] proibidos = ["autolist", "owl", "select2", "mcustomscrollbar", "ionicons", "materialdesign", "simple-line", "themify", "typicons", "linearicons", "weathericons", "glyphicon", "switcher", "jquery"];

        foreach (string caminho in _paginas)
        {
            string html = await cliente.GetStringAsync(caminho);
            List<string> recursos = Regex.Matches(html, @"(?:href|src)=""([^""]+\.(?:css|js))(?:\?[^""]*)?""").Select(m => m.Groups[1].Value).ToList();

            Assert.IsTrue(recursos.Count >= 8, caminho + ": poucos recursos, a regex falhou?");
            foreach (string recurso in recursos)
            {
                string nome = recurso.ToLowerInvariant();
                Assert.IsFalse(proibidos.Any(p => nome.Contains(p, StringComparison.Ordinal)), caminho + " carrega " + recurso);
            }
        }
    }

    [TestMethod]
    public async Task IconesFontAwesome_SaoDecorativos_ComAriaHidden()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add(FabricaWeb.CabecalhoPapel, "Administrador");

        int total = 0;
        foreach (string caminho in _paginas)
        {
            string html = await cliente.GetStringAsync(caminho);
            foreach (Match icone in Regex.Matches(html, @"<i\b[^>]*class=""[^""]*\bfa\b[^""]*""[^>]*>"))
            {
                total++;
                StringAssert.Contains(icone.Value, "aria-hidden=\"true\"", caminho + ": ícone sem aria-hidden: " + icone.Value);
            }
        }

        Assert.IsTrue(total >= 4, "esperava ícones no cabeçalho e no painel; a regex falhou?");
    }

    [TestMethod]
    public async Task OutrasFamiliasDeIcones_NaoExistemNoSite()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        foreach (string caminho in _paginas)
        {
            string html = await cliente.GetStringAsync(caminho);
            Assert.AreEqual(0, Regex.Matches(html, @"<i\b[^>]*class=""[^""]*\b(?:mdi|typcn|ti|si|zmdi|wi|fe|lnr|ion|glyphicon|icon)-[^""]*""").Count, caminho + ": ícone de outra família");
        }
    }
}
