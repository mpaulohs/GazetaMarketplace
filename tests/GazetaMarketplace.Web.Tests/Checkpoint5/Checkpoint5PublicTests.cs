using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Detail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Checkpoint5;

/// <summary>
/// Checkpoint 5, item "início, categoria, busca, detalhe, contato e favoritos funcionam sem login": cada endereço público, aberto por um visitante <b>sem login</b>, responde o que deve
/// (200, ou 404 para o que não existe) e <b>nunca</b> redireciona para a entrada do painel; já o painel e a API da equipe continuam pedindo login. O que o navegador faz depois (favoritar, ligar) é provado no E2E do checkpoint.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class Checkpoint5PublicTests
#pragma warning restore CA1515
{
    private const int Cars = 33;

    private static HttpClient NoRedirectVisitor(DraftSite site) => site.Harness.Anonymous();

    private static async Task<(string Path, HttpStatusCode Status, string Location)[]> OpenAsync(DraftSite site, IEnumerable<string> paths)
    {
        using HttpClient visitor = NoRedirectVisitor(site);
        List<(string, HttpStatusCode, string)> results = [];
        foreach (string path in paths)
        {
            using HttpResponseMessage response = await visitor.GetAsync(path);
            results.Add((path, response.StatusCode, response.Headers.Location?.OriginalString));
        }

        return [.. results];
    }

    [TestMethod]
    public async Task EnderecosPublicos_SemLogin_Respondem200_SemRedirecionarParaAEntradaDoPainel()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AdDetailTests.AddAsync(site, "Honda Civic 2018", Cars);
        string slug = (await site.Harness.Factory.Services.GetRequiredService<ICategoryTree>().GetAsync(default)).Find(Cars).Slug;
        string[] paths =
        [
            "/", $"/categoria/{slug}", $"/categoria/{slug}?pagina=2", "/busca", "/busca?q=civic&uf=SP&ordem=menor-preco", "/busca?categoria=" + slug,
            $"/anuncio/{id}/honda-civic-2018", "/favoritos", "/favoritos/lista?ids=1,2", "/api/v1/ads?ids=1,2", "/api/v1/public/cities?uf=SP",
            "/sitemap.xml", "/robots.txt"
        ];

        (string Path, HttpStatusCode Status, string Location)[] results = await OpenAsync(site, paths);

        foreach ((string path, HttpStatusCode status, string location) in results)
        {
            Assert.AreEqual(HttpStatusCode.OK, status, path);
            Assert.IsNull(location, path + ": nenhuma página pública redireciona");
        }
    }

    [TestMethod]
    public async Task OQueNaoExiste_Da404_SemPedirLogin()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string[] paths = ["/categoria/nao-existe-mais", "/anuncio/999999/qualquer", "/anuncio/999999", "/pagina-que-nao-existe"];

        (string Path, HttpStatusCode Status, string Location)[] results = await OpenAsync(site, paths);

        foreach ((string path, HttpStatusCode status, string location) in results)
        {
            Assert.AreEqual(HttpStatusCode.NotFound, status, path);
            Assert.IsNull(location, path);
        }
    }

    [TestMethod]
    public async Task EnderecosDaEquipe_ContinuamPedindoLogin()
    {
        using DraftSite site = await DraftSite.StartAsync();
        string[] paths = ["/painel/anuncios", "/painel/anuncios/fila", "/painel/categorias", "/painel/usuarios", "/painel/configuracoes", "/painel/anuncios/novo"];

        (string Path, HttpStatusCode Status, string Location)[] results = await OpenAsync(site, paths);

        foreach ((string path, HttpStatusCode status, string location) in results)
        {
            Assert.AreEqual(HttpStatusCode.Redirect, status, path);
            StringAssert.Contains(location, "/painel/entrar", path);
        }

        using HttpClient visitor = NoRedirectVisitor(site);
        HttpResponseMessage staffApi = await visitor.GetAsync("/api/v1/cities?uf=SP");
        Assert.IsTrue(staffApi.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect or HttpStatusCode.Forbidden, "a lista de cidades da equipe pede login: " + staffApi.StatusCode);
    }

    [TestMethod]
    public async Task PaginasPublicas_NaoCriamCookieDeSessao_NemPedemDadosDoVisitante()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AdDetailTests.AddAsync(site, "Honda Civic 2018", Cars);
        string[] paths = ["/", "/busca?q=civic", $"/anuncio/{id}/honda-civic-2018", "/favoritos"];
        using HttpClient visitor = NoRedirectVisitor(site);

        foreach (string path in paths)
        {
            using HttpResponseMessage response = await visitor.GetAsync(path);
            Assert.IsFalse(response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> cookies) && cookies.Any(c => c.StartsWith(".AspNetCore.Identity", StringComparison.Ordinal)), path + ": o visitante não recebe cookie de entrada");
        }
    }
}
