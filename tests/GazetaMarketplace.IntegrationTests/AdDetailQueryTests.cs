using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>A página pública do anúncio (US-003) no SQL Server de verdade: só publicado aparece, as demais situações e o id inexistente respondem igual, as fotos seguem a posição e o endereço com o título editado leva ao atual.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdDetailQueryTests
#pragma warning restore CA1515
{
    private const int General = 86;
    private static readonly DateTime Noon = new(2026, 9, 12, 15, 0, 0, DateTimeKind.Utc);

    private static async Task<int> AddAsync(string connection, int author, string title, byte status, int[] photoPositions)
    {
        Ad ad = Ad.CreateDraft(title, author);
        ad.SetCategory(General);
        ad.SetText(title, "Descrição de " + title);
        ad.SetPrice(5_000_00);
        ad.SetLocation("13015100", "Campinas", "SP", false);
        ad.SetAttributes(new AdAttributes().Set("conditionId", 2));
        switch (status)
        {
            case AdStatus.InReview:
                ad.ApplyTransition(AdStatus.InReview, author, Noon, null);
                break;
            case AdStatus.Published:
                ad.ApplyTransition(AdStatus.InReview, author, Noon, null);
                ad.ApplyTransition(AdStatus.Published, author, Noon, null);
                break;
            case AdStatus.Rejected:
                ad.ApplyTransition(AdStatus.InReview, author, Noon, null);
                ad.ApplyTransition(AdStatus.Rejected, author, Noon, "Fotos escuras");
                break;
            case AdStatus.Archived:
                ad.ApplyTransition(AdStatus.Archived, author, Noon, null);
                break;
        }

        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        context.Ads.Add(ad);
        await context.SaveChangesAsync();
        foreach (int position in photoPositions)
        {
            context.AdPhotos.Add(new AdPhoto { AdId = ad.Id, SortOrder = position, StorageKey = $"detalhe-{Guid.NewGuid():N}.webp", Width = 1600, Height = 1200, SizeBytes = 1000, CreatedAt = Noon });
        }

        await context.SaveChangesAsync();
        return ad.Id;
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task SoPublicadoAparece_OutrasSituacoesEInexistenteRespondemIgual_ArquivadoSoAcrescentaACategoria()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        AppUser writer = await factory.CreateUserAsync("detalhe.redatora@exemplo.com.br", "Ana Souza", "Senha@Forte1", RoleNames.Writer);
        int published = await AddAsync(connection, writer.Id, "Livro publicado", AdStatus.Published, [0, 1]);
        int draft = await AddAsync(connection, writer.Id, "Livro rascunho", AdStatus.Draft, [0]);
        int review = await AddAsync(connection, writer.Id, "Livro em revisão", AdStatus.InReview, [0]);
        int rejected = await AddAsync(connection, writer.Id, "Livro rejeitado", AdStatus.Rejected, [0]);
        int archived = await AddAsync(connection, writer.Id, "Livro arquivado", AdStatus.Archived, [0]);
        using HttpClient visitor = factory.CreateBrowser();

        HttpResponseMessage ok = await visitor.GetAsync($"/anuncio/{published}/livro-publicado");
        string okHtml = WebUtility.HtmlDecode(await ok.Content.ReadAsStringAsync());
        List<(string Name, HttpResponseMessage Response, string Body)> unavailable = [];
        foreach ((string name, int id, string slug) in new[] { ("rascunho", draft, "livro-rascunho"), ("em revisão", review, "livro-em-revisao"), ("rejeitado", rejected, "livro-rejeitado"), ("inexistente", published + 1000, "x") })
        {
            HttpResponseMessage response = await visitor.GetAsync($"/anuncio/{id}/{slug}");
            unavailable.Add((name, response, await response.Content.ReadAsStringAsync()));
        }

        HttpResponseMessage archivedResponse = await visitor.GetAsync($"/anuncio/{archived}/livro-arquivado");
        string archivedBody = await archivedResponse.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, ok.StatusCode);
        StringAssert.Contains(okHtml, "Livro publicado");
        StringAssert.Contains(okHtml, "Descrição de Livro publicado");
        foreach ((string name, HttpResponseMessage response, string body) in unavailable)
        {
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, name);
            Assert.AreEqual(unavailable[0].Body, body, $"{name}: a mesma resposta");
            Assert.IsFalse(body.Contains("Livro ", StringComparison.Ordinal), name + " não revela o título");
        }

        Assert.AreEqual(HttpStatusCode.NotFound, archivedResponse.StatusCode);
        StringAssert.Contains(archivedBody, "data-archived-category");
        Assert.IsFalse(archivedBody.Contains("Livro arquivado", StringComparison.Ordinal));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Fotos_NaOrdemDaPosicao_NaoDaInsercao_ECapaEAPosicaoZero()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        AppUser writer = await factory.CreateUserAsync("detalhe.fotos@exemplo.com.br", "Ana Souza", "Senha@Forte1", RoleNames.Writer);
        int id = await AddAsync(connection, writer.Id, "Livro com fotos", AdStatus.Published, [2, 0, 1]);
        List<AdPhoto> byPosition;
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            byPosition = await context.AdPhotos.AsNoTracking().Where(p => p.AdId == id).OrderBy(p => p.SortOrder).ToListAsync();
        }

        using HttpClient visitor = factory.CreateBrowser();
        string html = await visitor.GetStringAsync($"/anuncio/{id}/livro-com-fotos");

        string[] thumbs = [.. Regex.Matches(html, @"data-gallery-thumb data-index=""\d+"" data-large=""/fotos/\d+/(\d+)-1600\.webp""").Select(m => m.Groups[1].Value)];
        CollectionAssert.AreEqual(byPosition.Select(p => p.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray(), thumbs, "miniaturas na ordem da posição");
        StringAssert.Contains(html, $"<img class=\"ad-galeria__imagem\" src=\"/fotos/{id}/{byPosition[0].Id}-1600.webp\"", "a foto em destaque é a de posição 0");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Endereco_TituloComAcentos_EditadoDepois_LevaAoEnderecoAtual()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        AppUser writer = await factory.CreateUserAsync("detalhe.slug@exemplo.com.br", "Ana Souza", "Senha@Forte1", RoleNames.Writer);
        int id = await AddAsync(connection, writer.Id, "Máquina de Costura Singer", AdStatus.Published, [0]);
        using HttpClient visitor = factory.CreateBrowser();

        HttpResponseMessage current = await visitor.GetAsync($"/anuncio/{id}/maquina-de-costura-singer");
        HttpResponseMessage wrong = await visitor.GetAsync($"/anuncio/{id}/maquina");
        HttpResponseMessage noSlug = await visitor.GetAsync($"/anuncio/{id}");

        Assert.AreEqual(HttpStatusCode.OK, current.StatusCode);
        Assert.AreEqual(HttpStatusCode.MovedPermanently, wrong.StatusCode);
        Assert.AreEqual($"/anuncio/{id}/maquina-de-costura-singer", wrong.Headers.Location!.OriginalString);
        Assert.AreEqual(HttpStatusCode.MovedPermanently, noSlug.StatusCode);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Contato_TelefoneVemDoBanco_NaPaginaPublica_ESemTelefoneOBlocoNaoAparece()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        AppUser writer = await factory.CreateUserAsync("detalhe.contato@exemplo.com.br", "Ana Souza", "Senha@Forte1", RoleNames.Writer);
        int id = await AddAsync(connection, writer.Id, "Sítio \"Boa Vista\" & Cia", AdStatus.Published, [0]);
        string path = AdRoutes.Detail(id, "Sítio \"Boa Vista\" & Cia");
        using HttpClient visitor = factory.CreateBrowser();

        string without = await visitor.GetStringAsync(path);

        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            context.SiteSettings.Add(new SiteSetting { Key = SiteSettingKeys.Phone, Value = "11912345678" });
            await context.SaveChangesAsync();
        }

        factory.Services.GetRequiredService<ISiteSettings>().Invalidate();
        string with = await visitor.GetStringAsync(path);

        Assert.IsFalse(Regex.IsMatch(without, @"tel:|wa\.me|Fale com a Gazeta"), "sem telefone configurado o bloco não aparece");
        StringAssert.Contains(with, "(11) 91234-5678");
        StringAssert.Contains(WebUtility.HtmlDecode(with), "href=\"tel:+5511912345678\"");
        Match whatsApp = Regex.Match(with, @"href=""(https://wa\.me/5511912345678\?text=[^""]+)""");
        Assert.IsTrue(whatsApp.Success);
        var query = System.Web.HttpUtility.ParseQueryString(new Uri(whatsApp.Groups[1].Value).Query);
        Assert.AreEqual($"Olá! Tenho interesse no anúncio “Sítio \"Boa Vista\" & Cia”: https://localhost{path}", query["text"]);
    }
}
