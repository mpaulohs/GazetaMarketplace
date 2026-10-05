using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// Checkpoint 5, itens "Serviços e Vagas aparecem corretamente em cards, detalhe e busca" e "mapa só com publicados", no SQL Server real e com um visitante sem login: o Serviço mostra o tipo e nunca um
/// preço, fica fora da faixa de preço e no fim de "Menor preço"; a Vaga mostra o salário e as áreas e nunca uma foto; os dois saem em categoria, busca, favoritos, API e mapa. Depois, arquivar um e despublicar o
/// outro tira ambos de todos esses lugares na hora.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class Checkpoint5ServicesJobsTests
#pragma warning restore CA1515
{
    private const int Cars = 33;
    private const int Services = 66;
    private const int Jobs = 96;

    private static readonly DateTime Base = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private static string Decode(string html) => WebUtility.HtmlDecode(html);

    private static async Task<int> AddAsync(string connection, int author, string title, int category, long? price, byte status, Action<AdAttributes> attributes = null, bool photo = true, DateTime? at = null)
    {
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        Ad ad = Ad.CreateDraft(title, author);
        ad.SetCategory(category);
        ad.SetText(title, $"Descrição de {title}, com experiência comprovada.");
        ad.SetPrice(price);
        ad.SetLocation("13015100", "Campinas", "SP", false);
        if (attributes is not null)
        {
            AdAttributes values = new();
            attributes(values);
            ad.SetAttributes(values);
        }

        DateTime when = at ?? Base;
        if (status == AdStatus.Published)
        {
            ad.ApplyTransition(AdStatus.InReview, author, when, null);
            ad.ApplyTransition(AdStatus.Published, author, when, null);
        }

        context.Ads.Add(ad);
        await context.SaveChangesAsync();
        if (photo)
        {
            context.AdPhotos.Add(new AdPhoto { AdId = ad.Id, SortOrder = 0, StorageKey = $"cp5-{Guid.NewGuid():N}.webp", Width = 1600, Height = 1200, SizeBytes = 1000, CreatedAt = Base });
            await context.SaveChangesAsync();
        }

        return ad.Id;
    }

    private static async Task SetStatusAsync(string connection, int adId, int actor, byte status)
    {
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        Ad ad = await context.Ads.SingleAsync(a => a.Id == adId);
        ad.ApplyTransition(status, actor, Base.AddDays(30), null);
        await context.SaveChangesAsync();
    }

    // O texto do card (<article>) que traz o título; nulo se o título não está na página
    private static string CardOf(string html, string title)
    {
        string decoded = Decode(html);
        foreach (string card in Regex.Split(decoded, @"(?=<article class=""card ad-card)").Skip(1))
        {
            if (card.Contains(title, StringComparison.Ordinal))
            {
                return card;
            }
        }

        return null;
    }

    private static string Visible(string html) => Regex.Replace(Regex.Replace(Decode(html), @"<[^>]+>", " "), @"\s+", " ").Trim();

    private static string[] TitlesInOrder(string html, params string[] titles) =>
        [.. titles.Where(t => Decode(html).Contains(t, StringComparison.Ordinal)).OrderBy(t => Decode(html).IndexOf(t, StringComparison.Ordinal))];

    private static string[] Locs(string xml) => [.. XDocument.Parse(xml).Root!.Elements(Ns + "url").Select(u => u.Element(Ns + "loc")!.Value)];

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ServicosEVagas_EmCardsDetalheBuscaFavoritosApiEMapa_EDepoisDeArquivarEDespublicar_SaemDeTodoLugar()
    {
        const string serviceTitle = "Diarista com experiência";
        const string jobTitle = "Pizzaiolo com experiência";
        const string carTitle = "Honda Civic 2018 com experiência";
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        AppUser writer = await factory.CreateUserAsync("cp5.redatora@exemplo.com.br", "Ana Souza", "Senha@Forte1", RoleNames.Writer);
        int car = await AddAsync(connection, writer.Id, carTitle, Cars, 6_200_000, AdStatus.Published, at: Base.AddDays(1));
        int service = await AddAsync(connection, writer.Id, serviceTitle, Services, null, AdStatus.Published, a => a.Set("serviceTypeId", 1), at: Base.AddDays(2));
        int job = await AddAsync(connection, writer.Id, jobTitle, Jobs, 280_000, AdStatus.Published, a => a.Set("jobAreaIds", new[] { 1 }), at: Base.AddDays(3));
        int draft = await AddAsync(connection, writer.Id, "Rascunho com experiência", Services, null, AdStatus.Draft, a => a.Set("serviceTypeId", 1));
        using HttpClient visitor = factory.CreateBrowser();
        ICategoryTree tree = factory.Services.GetRequiredService<ICategoryTree>();
        CategoryTreeSnapshot snapshot = await tree.GetAsync(default);

        // Cards na página inicial: tipo no Serviço (sem preço), salário na Vaga (sem foto), preço no carro
        string home = await visitor.GetStringAsync("/");
        string serviceCard = CardOf(home, serviceTitle);
        string jobCard = CardOf(home, jobTitle);
        Assert.IsNotNull(serviceCard, "o Serviço aparece na página inicial");
        Assert.IsNotNull(jobCard, "a Vaga aparece na página inicial");
        StringAssert.Contains(Visible(serviceCard), "Serviços domésticos");
        Assert.IsFalse(serviceCard.Contains("R$", StringComparison.Ordinal), "Serviço nunca mostra preço");
        StringAssert.Contains(serviceCard, "<img ", "Serviço mostra a capa");
        StringAssert.Contains(Visible(jobCard), "Salário R$ 2.800");
        Assert.IsFalse(jobCard.Contains("<img", StringComparison.Ordinal), "Vaga nunca mostra foto");
        StringAssert.Contains(jobCard, "data-ad-placeholder");
        StringAssert.Contains(Visible(CardOf(home, carTitle)), "R$ 62.000");
        Assert.IsFalse(Decode(home).Contains("Rascunho com experiência", StringComparison.Ordinal));

        // Páginas de categoria
        string servicesPage = await visitor.GetStringAsync("/categoria/" + snapshot.Find(Services).Slug);
        string jobsPage = await visitor.GetStringAsync("/categoria/" + snapshot.Find(Jobs).Slug);
        Assert.IsNotNull(CardOf(servicesPage, serviceTitle));
        Assert.IsNull(CardOf(servicesPage, jobTitle), "cada categoria mostra só os seus");
        Assert.IsNotNull(CardOf(jobsPage, jobTitle));

        // Detalhe
        string servicePage = Decode(await visitor.GetStringAsync(AdRoutes.Detail(service, serviceTitle)));
        string jobPage = Decode(await visitor.GetStringAsync(AdRoutes.Detail(job, jobTitle)));
        StringAssert.Contains(Visible(servicePage), "Serviços domésticos");
        Assert.IsFalse(Regex.IsMatch(Visible(Regex.Match(servicePage, @"<main[\s\S]*?</main>").Value), @"R\$"), "a página do Serviço não tem preço");
        StringAssert.Contains(Visible(jobPage), "Salário");
        StringAssert.Contains(Visible(jobPage), "2.800");
        StringAssert.Contains(Visible(jobPage), "Administrativo");
        Assert.IsFalse(jobPage.Contains("/fotos/", StringComparison.Ordinal), "a página da Vaga não tem galeria");
        Assert.AreEqual(0, Regex.Matches(jobPage, @"og:image").Count, "nem imagem de prévia");

        // Busca: os dois aparecem por texto; o Serviço sai da faixa de preço e vai para o fim de "Menor preço"
        string byText = await visitor.GetStringAsync("/busca?q=experiencia");
        Assert.IsNotNull(CardOf(byText, serviceTitle));
        Assert.IsNotNull(CardOf(byText, jobTitle));
        Assert.IsNull(CardOf(byText, "Rascunho com experiência"), "rascunho não aparece");
        string withBand = await visitor.GetStringAsync("/busca?q=experiencia&precoMin=1000");
        Assert.IsNull(CardOf(withBand, serviceTitle), "Serviço fica fora da faixa de preço");
        Assert.IsNotNull(CardOf(withBand, jobTitle));
        Assert.IsNotNull(CardOf(withBand, carTitle));
        string cheapFirst = await visitor.GetStringAsync("/busca?q=experiencia&ordem=menor-preco");
        CollectionAssert.AreEqual(new[] { jobTitle, carTitle, serviceTitle }, TitlesInOrder(cheapFirst, serviceTitle, jobTitle, carTitle), "Serviço no fim de Menor preço");
        string dearFirst = await visitor.GetStringAsync("/busca?q=experiencia&ordem=maior-preco");
        CollectionAssert.AreEqual(new[] { carTitle, jobTitle, serviceTitle }, TitlesInOrder(dearFirst, serviceTitle, jobTitle, carTitle), "Serviço no fim de Maior preço");

        // Favoritos e API: o fragmento traz os dois cards; a API tem preço nulo no Serviço e capa nula na Vaga
        string favorites = await visitor.GetStringAsync($"/favoritos/lista?ids={job},{service},{draft}");
        Assert.IsNotNull(CardOf(favorites, serviceTitle));
        Assert.IsNotNull(CardOf(favorites, jobTitle));
        Assert.IsNull(CardOf(favorites, "Rascunho com experiência"));
        using JsonDocument api = JsonDocument.Parse(await visitor.GetStringAsync($"/api/v1/ads?ids={service},{job},{draft}"));
        JsonElement[] items = [.. api.RootElement.GetProperty("items").EnumerateArray()];
        Assert.AreEqual(2, items.Length);
        Assert.AreEqual(JsonValueKind.Null, items[0].GetProperty("priceCents").ValueKind, "Serviço sem preço");
        Assert.AreNotEqual(JsonValueKind.Null, items[0].GetProperty("coverUrl").ValueKind, "Serviço tem capa");
        Assert.AreEqual(280_000, items[1].GetProperty("priceCents").GetInt64());
        Assert.AreEqual(JsonValueKind.Null, items[1].GetProperty("coverUrl").ValueKind, "Vaga sem capa");

        // Mapa: os três publicados, nunca o rascunho
        string[] locs = Locs(await visitor.GetStringAsync("/sitemap.xml"));
        Assert.IsTrue(locs.Any(l => l.Contains($"/anuncio/{car}/", StringComparison.Ordinal)));
        Assert.IsTrue(locs.Any(l => l.Contains($"/anuncio/{service}/", StringComparison.Ordinal)));
        Assert.IsTrue(locs.Any(l => l.Contains($"/anuncio/{job}/", StringComparison.Ordinal)));
        Assert.IsFalse(locs.Any(l => l.Contains($"/anuncio/{draft}/", StringComparison.Ordinal)));
        Assert.IsTrue(locs.Any(l => l.EndsWith("/categoria/" + snapshot.Find(Services).Slug, StringComparison.Ordinal)), "a categoria de Serviços tem anúncio");

        // O Serviço é arquivado e a Vaga é despublicada: saem de todos os lugares na hora
        await SetStatusAsync(connection, service, writer.Id, AdStatus.Archived);
        await SetStatusAsync(connection, job, writer.Id, AdStatus.Draft);

        string homeAfter = await visitor.GetStringAsync("/");
        string searchAfter = await visitor.GetStringAsync("/busca?q=experiencia");
        string favoritesAfter = await visitor.GetStringAsync($"/favoritos/lista?ids={job},{service}");
        using JsonDocument apiAfter = JsonDocument.Parse(await visitor.GetStringAsync($"/api/v1/ads?ids={service},{job},{car}"));
        string[] locsAfter = Locs(await visitor.GetStringAsync("/sitemap.xml"));
        string servicesPageAfter = await visitor.GetStringAsync("/categoria/" + snapshot.Find(Services).Slug);
        foreach ((string label, string html) in new[] { ("início", homeAfter), ("busca", searchAfter), ("favoritos", favoritesAfter), ("categoria de Serviços", servicesPageAfter) })
        {
            Assert.IsNull(CardOf(html, serviceTitle), label + ": o Serviço arquivado saiu");
            Assert.IsNull(CardOf(html, jobTitle), label + ": a Vaga despublicada saiu");
        }

        CollectionAssert.AreEqual(new[] { car }, apiAfter.RootElement.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToArray());
        Assert.IsFalse(locsAfter.Any(l => l.Contains($"/anuncio/{service}/", StringComparison.Ordinal) || l.Contains($"/anuncio/{job}/", StringComparison.Ordinal)), "o mapa também");
        Assert.IsFalse(locsAfter.Any(l => l.EndsWith("/categoria/" + snapshot.Find(Services).Slug, StringComparison.Ordinal)), "Serviços sem anúncio publicado sai do mapa");
        Assert.AreEqual(HttpStatusCode.NotFound, (await visitor.GetAsync(AdRoutes.Detail(service, serviceTitle))).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await visitor.GetAsync(AdRoutes.Detail(job, jobTitle))).StatusCode);
    }
}
