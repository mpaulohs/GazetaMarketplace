using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Layout;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class StatesTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task EstadoDeErro_MostraCodigoDeReferenciaETentarNovamente()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/teste/erro");
        string html = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        string code = response.Headers.GetValues("X-Correlation-ID").Single();
        StringAssert.Matches(html, new Regex(@"Código de referência:[\s\S]*?<code>" + Regex.Escape(code) + "</code>"));
        StringAssert.Matches(html, new Regex(@"<a[^>]*href=""/teste/erro""[^>]*>\s*Tentar novamente\s*</a>"));
        StringAssert.Matches(html, new Regex(@"<h1[^>]*\bdata-foco-inicial[^>]*\btabindex=""-1""|<h1[^>]*\btabindex=""-1""[^>]*\bdata-foco-inicial"));
        Assert.IsFalse(html.Contains("falha de teste", System.StringComparison.Ordinal), "a mensagem da exceção não pode vazar");
    }

    [TestMethod]
    public async Task EstadoVazio_MostraTituloMensagemEAcao()
    {
        string html = await DownloadAsync();

        StringAssert.Contains(html, "Você ainda não criou anúncios");
        StringAssert.Matches(html, new Regex(@"<a[^>]*href=""/painel/anuncios""[^>]*>\s*Criar anúncio\s*</a>"));
    }

    [TestMethod]
    public async Task EstadoSemResultado_OferecerLimparFiltros()
    {
        string html = await DownloadAsync();

        StringAssert.Matches(html, new Regex(@"<a[^>]*href=""/busca""[^>]*>\s*Limpar filtros\s*</a>"));
    }

    [TestMethod]
    public async Task Esqueleto_ReservaAlturaPorClasse_ESinalizaCarregando()
    {
        string html = await DownloadAsync();

        Assert.AreEqual(4, Regex.Matches(html, @"class=""[^""]*\besqueleto-card\b").Count);
        StringAssert.Matches(html, new Regex(@"<[^>]*role=""status""[^>]*aria-busy=""true""|<[^>]*aria-busy=""true""[^>]*role=""status"""));
        StringAssert.Contains(html, "Carregando");
        Assert.AreEqual(0, Regex.Matches(html, @"\sstyle\s*=").Count, "altura reservada só por classe (CSP)");
    }

    private static async Task<string> DownloadAsync()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/teste/estados");
        Assert.IsTrue(response.IsSuccessStatusCode);

        // O Razor codifica letras acentuadas como entidades (&#xFA;); o navegador mostra o mesmo texto
        return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    }
}
