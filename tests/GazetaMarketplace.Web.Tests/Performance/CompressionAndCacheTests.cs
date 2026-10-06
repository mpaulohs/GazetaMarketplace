using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Middleware;
using GazetaMarketplace.Web.Tests.Ads;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Performance;

/// <summary>
/// NFR-01 e NFR-05, a parte do servidor: o site público e a API saem comprimidos (Brotli ou gzip); o painel <b>não</b> (BREACH: o HTML do painel carrega o token antiforgery, e comprimir HTML com segredo por HTTPS
/// deixa um atacante adivinhá-lo); nenhuma página pública carrega o token (por isso comprimi-las é seguro); e arquivo estático pedido com <c>?v=</c> sai com cache de um ano e <c>immutable</c>.
/// </summary>
[TestClass]
public sealed class CompressionAndCacheTests
{
    private static readonly string[] PublicPages = ["/", "/busca", "/busca?q=civic", "/favoritos", "/anuncio/999999/x", "/categoria/nao-existe", "/Home/Error", "/api/v1/public/cities?uf=SP", "/sitemap.xml", "/robots.txt"];

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string url, string acceptEncoding)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept-Encoding", acceptEncoding);
        return await client.SendAsync(request);
    }

    private static string Encoding(HttpResponseMessage response) => response.Content.Headers.ContentEncoding.SingleOrDefault();

    [TestMethod]
    public async Task PublicPagesAndApi_AreCompressed_WithBrotliOrGzip_AndVaryOnTheEncoding()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();
        List<string> wrong = [];

        foreach (string url in PublicPages)
        {
            using HttpResponseMessage brotli = await GetAsync(visitor, url, "br, gzip");
            using HttpResponseMessage gzip = await GetAsync(visitor, url, "gzip");
            if (Encoding(brotli) != "br")
            {
                wrong.Add($"{url}: com 'br, gzip' veio {Encoding(brotli) ?? "sem compressão"}");
            }

            if (Encoding(gzip) != "gzip")
            {
                wrong.Add($"{url}: com 'gzip' veio {Encoding(gzip) ?? "sem compressão"}");
            }

            if (!brotli.Headers.Vary.Contains("Accept-Encoding"))
            {
                wrong.Add($"{url}: falta Vary: Accept-Encoding (um cache poderia entregar a versão comprimida a quem não a entende)");
            }
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
    }

    [TestMethod]
    public async Task APageWithoutAcceptEncoding_IsNotCompressed()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();

        using HttpResponseMessage response = await GetAsync(visitor, "/", "identity");

        Assert.IsNull(Encoding(response));
    }

    [TestMethod]
    public async Task PanelPages_AreNeverCompressed_BecauseTheyCarryTheAntiforgeryToken()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();
        List<string> wrong = [];

        foreach (string url in new[] { "/painel/entrar", "/painel/esqueci-minha-senha", "/painel/acesso-negado" })
        {
            using HttpResponseMessage response = await GetAsync(visitor, url, "br, gzip");
            if (Encoding(response) is not null)
            {
                wrong.Add($"{url} saiu comprimida ({Encoding(response)})");
            }
        }

        foreach (string url in new[] { "/painel/anuncios", "/painel/anuncios/novo", "/painel/usuarios", "/painel/categorias", "/painel/configuracoes" })
        {
            using HttpResponseMessage response = await GetAsync(site.Admin, url, "br, gzip");
            string body = await response.Content.ReadAsStringAsync();
            if (Encoding(response) is not null)
            {
                wrong.Add($"{url} saiu comprimida ({Encoding(response)})");
            }

            if (!body.Contains("request-verification-token", System.StringComparison.Ordinal) && !body.Contains("__RequestVerificationToken", System.StringComparison.Ordinal))
            {
                wrong.Add($"{url}: o painel deixou de levar o token antiforgery no HTML (a razão da regra mudou; reveja a decisão)");
            }
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
    }

    /// <summary>
    /// Uma resposta de erro do painel é reexecutada numa página pública (<c>/Home/Status/404</c>, <c>/Home/Error</c>): o caminho do pedido muda, e uma regra que olhasse o caminho da reexecução comprimiria a resposta de quem está logado.
    /// A regra é pelo pedido original (<c>/painel</c>), e o corpo da página de erro vista por quem está logado não pode levar o token junto com texto que o atacante controla.
    /// </summary>
    [TestMethod]
    public async Task APanelRequestThatEndsInAnErrorPage_IsNeverCompressed_AndNeverCarriesTheToken()
    {
        using DraftSite site = await DraftSite.StartAsync();
        List<string> wrong = [];

        foreach (string url in new[] { "/painel/anuncios/999999/editar", "/painel/nao-existe", "/painel/anuncios/999999/revisar", "/painel/categorias/999999/editar", "/painel/usuarios/999999/desativar" })
        {
            using HttpResponseMessage response = await GetAsync(site.Admin, url, "br, gzip");
            string body = await response.Content.ReadAsStringAsync();
            if ((int)response.StatusCode < 400)
            {
                wrong.Add($"{url}: devia ser uma resposta de erro, veio {(int)response.StatusCode} (troque a rota de teste)");
                continue;
            }

            if (Encoding(response) is not null)
            {
                wrong.Add($"{url} ({(int)response.StatusCode}) saiu comprimida ({Encoding(response)}): a página de erro foi reexecutada fora de /painel");
            }

            if (body.Contains("RequestVerificationToken", System.StringComparison.OrdinalIgnoreCase) || body.Contains("request-verification-token", System.StringComparison.OrdinalIgnoreCase))
            {
                wrong.Add($"{url} ({(int)response.StatusCode}): a página de erro leva o token antiforgery");
            }
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
    }

    /// <summary>O caminho da falha lançada (500 reexecutado em <c>/Home/Error</c>): num servidor mínimo com a mesma ordem do <c>Program.cs</c> (tratador de exceção e depois a compressão pública).</summary>
    [TestMethod]
    public async Task AnExceptionInAPanelRequest_ReexecutedInTheErrorPage_IsNotCompressed_ButAPublicOneIs()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddPublicResponseCompression();
        await using WebApplication app = builder.Build();
        app.UseExceptionHandler("/erro");
        app.UsePublicResponseCompression();
        app.MapGet("/painel/falha", (Func<string>)(() => throw new InvalidOperationException("falha de teste")));
        app.MapGet("/publica/falha", (Func<string>)(() => throw new InvalidOperationException("falha de teste")));
        app.MapGet("/erro", () => Results.Text(new string('a', 5000)));
        await app.StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage panel = await GetAsync(client, "/painel/falha", "br, gzip");
        using HttpResponseMessage publicPage = await GetAsync(client, "/publica/falha", "br, gzip");

        Assert.AreEqual(System.Net.HttpStatusCode.InternalServerError, panel.StatusCode, "a página de erro mantém o 500 da falha");
        Assert.AreEqual(5000, (await panel.Content.ReadAsStringAsync()).Length, "a página de erro foi servida");
        Assert.IsNull(Encoding(panel), "a falha de um pedido do painel não pode sair comprimida");
        Assert.AreEqual("br", Encoding(publicPage), "a mesma falha num pedido público segue comprimida (a regra só tira o painel)");
    }

    [TestMethod]
    public async Task NoPublicPage_CarriesTheAntiforgeryToken_SoCompressingThemIsSafe()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();
        List<string> wrong = [];

        foreach (string url in PublicPages)
        {
            string body = await visitor.GetStringAsync(url).ContinueWith(t => t.IsFaulted ? string.Empty : t.Result, TaskScheduler.Default);
            if (body.Contains("RequestVerificationToken", System.StringComparison.OrdinalIgnoreCase) || body.Contains("request-verification-token", System.StringComparison.OrdinalIgnoreCase))
            {
                wrong.Add($"{url} leva o token antiforgery: comprimi-la abriria o BREACH");
            }
        }

        Assert.IsEmpty(wrong, string.Join("\n", wrong));
    }

    [TestMethod]
    public async Task VersionedStaticFile_GetsOneYearImmutableCache_AndAnUnversionedOneKeepsRevalidating()
    {
        using DraftSite site = await DraftSite.StartAsync();
        using HttpClient visitor = site.Harness.Anonymous();

        using HttpResponseMessage versioned = await GetAsync(visitor, "/css/base.css?v=qualquercoisa", "identity");
        using HttpResponseMessage plain = await GetAsync(visitor, "/css/base.css", "identity");
        using HttpResponseMessage missing = await GetAsync(visitor, "/css/nao-existe.css?v=1", "identity");
        using HttpResponseMessage page = await GetAsync(visitor, "/?v=1", "identity");

        Assert.AreEqual(System.Net.HttpStatusCode.OK, versioned.StatusCode);
        Assert.AreEqual("public, max-age=31536000, immutable", versioned.Headers.CacheControl?.ToString());
        Assert.AreNotEqual("public, max-age=31536000, immutable", plain.Headers.CacheControl?.ToString(), "sem ?v= o arquivo continua revalidando");
        Assert.AreNotEqual("public, max-age=31536000, immutable", missing.Headers.CacheControl?.ToString(), "o que não existe nunca fica em cache por um ano");
        Assert.AreNotEqual("public, max-age=31536000, immutable", page.Headers.CacheControl?.ToString(), "página com ?v= não é arquivo estático");
    }
}
