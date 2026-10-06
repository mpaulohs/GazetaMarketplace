using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Middleware;

/// <summary>
/// Uma resposta de erro que sairia em branco (endereço que não existe, ação do painel que devolve <c>NotFound()</c>) vira uma página em português com o mesmo status (404 continua 404, para os buscadores). O erro
/// inesperado (500) já tinha a sua página; o teste confirma que continua amigável e sem vazar o motivo. API, verificação de saúde e arquivos ficam como estavam.
/// </summary>
[TestClass]
public sealed class StatusPagesTests
{
    [TestMethod]
    public async Task UnknownAddress_Is404_WithAFriendlyPageInPortuguese_AndWayBackToTheHome()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();

        using HttpResponseMessage response = await visitor.GetAsync("/pagina-que-nao-existe");
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "o status continua 404");
        Assert.AreEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        StringAssert.Contains(html, "<title>Página não encontrada");
        StringAssert.Matches(html, new Regex(@"<h1[^>]*>Página não encontrada</h1>"));
        StringAssert.Matches(html, new Regex(@"<a class=""btn btn-primary"" href=""/"">Ir para a página inicial</a>"));
        Assert.IsFalse(html.Contains("Código de referência", StringComparison.Ordinal), "um 404 não tem código de referência");
        Assert.IsFalse(Regex.IsMatch(html, @"RequestVerificationToken", RegexOptions.IgnoreCase), "página pública não leva o token");
    }

    [TestMethod]
    public async Task APanelAction_ThatReturnsNotFound_AlsoShowsTheFriendlyPage_WithStatus404()
    {
        using DraftSite site = await DraftSite.StartAsync();

        using HttpResponseMessage response = await site.Admin.GetAsync("/painel/anuncios/999999/editar");
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        StringAssert.Contains(html, "Página não encontrada");
    }

    [TestMethod]
    public async Task UnexpectedError_Is500_WithTheFriendlyPage_AndNeverTheInternalReason()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();

        using HttpResponseMessage response = await visitor.GetAsync("/teste/pagina-erro");
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        StringAssert.Contains(html, "Algo deu errado");
        StringAssert.Contains(html, "Código de referência", "o 500 mostra o código que leva ao log");
        Assert.IsFalse(html.Contains("segredo-interno-123", StringComparison.Ordinal), "o motivo interno nunca aparece");
    }

    [TestMethod]
    public async Task ApiHealthAndFiles_KeepTheirPlainResponses_NotTheHtmlPage()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();

        foreach (string url in new[] { "/api/v1/nao-existe", "/css/nao-existe.css", "/js/nao-existe.js", "/wp-login.php" })
        {
            using HttpResponseMessage response = await visitor.GetAsync(url);
            string body = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, url);
            Assert.IsFalse(body.Contains("<html", StringComparison.OrdinalIgnoreCase), $"{url} não vira a página HTML de erro");
        }
    }

    [TestMethod]
    public async Task StatusAddress_WithAnInvalidCode_FallsBackTo404()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();

        using HttpResponseMessage response = await visitor.GetAsync("/Home/Status/200");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, "a página de status nunca responde 200");
    }
}
