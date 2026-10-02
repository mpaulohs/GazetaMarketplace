using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Layout;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class EstadosTests
#pragma warning restore CA1515
{
    [TestMethod]
    public async Task EstadoDeErro_MostraCodigoDeReferenciaETentarNovamente()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync("/teste/erro");
        string html = await resposta.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.InternalServerError, resposta.StatusCode);
        string codigo = resposta.Headers.GetValues("X-Correlation-ID").Single();
        StringAssert.Matches(html, new Regex(@"Código de referência:[\s\S]*?<code>" + Regex.Escape(codigo) + "</code>"));
        StringAssert.Matches(html, new Regex(@"<a[^>]*href=""/teste/erro""[^>]*>\s*Tentar novamente\s*</a>"));
        StringAssert.Matches(html, new Regex(@"<h1[^>]*\bdata-foco-inicial[^>]*\btabindex=""-1""|<h1[^>]*\btabindex=""-1""[^>]*\bdata-foco-inicial"));
        Assert.IsFalse(html.Contains("falha de teste", System.StringComparison.Ordinal), "a mensagem da exceção não pode vazar");
    }

    [TestMethod]
    public async Task EstadoVazio_MostraTituloMensagemEAcao()
    {
        string html = await BaixarAsync();

        StringAssert.Contains(html, "Você ainda não criou anúncios");
        StringAssert.Matches(html, new Regex(@"<a[^>]*href=""/painel/anuncios""[^>]*>\s*Criar anúncio\s*</a>"));
    }

    [TestMethod]
    public async Task EstadoSemResultado_OferecerLimparFiltros()
    {
        string html = await BaixarAsync();

        StringAssert.Matches(html, new Regex(@"<a[^>]*href=""/busca""[^>]*>\s*Limpar filtros\s*</a>"));
    }

    [TestMethod]
    public async Task Esqueleto_ReservaAlturaPorClasse_ESinalizaCarregando()
    {
        string html = await BaixarAsync();

        Assert.AreEqual(4, Regex.Matches(html, @"class=""[^""]*\besqueleto-card\b").Count);
        StringAssert.Matches(html, new Regex(@"<[^>]*role=""status""[^>]*aria-busy=""true""|<[^>]*aria-busy=""true""[^>]*role=""status"""));
        StringAssert.Contains(html, "Carregando");
        Assert.AreEqual(0, Regex.Matches(html, @"\sstyle\s*=").Count, "altura reservada só por classe (CSP)");
    }

    private static async Task<string> BaixarAsync()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();
        HttpResponseMessage resposta = await cliente.GetAsync("/teste/estados");
        Assert.IsTrue(resposta.IsSuccessStatusCode);

        // O Razor codifica letras acentuadas como entidades (&#xFA;); o navegador mostra o mesmo texto
        return WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
    }
}
