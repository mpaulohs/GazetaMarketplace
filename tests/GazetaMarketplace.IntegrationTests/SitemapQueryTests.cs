using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Seo;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Infrastructure.Seo;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// O mapa do site (NFR-21; tarefa 5.6) no T-SQL de verdade: só anúncios publicados (rascunho, em revisão, rejeitado, arquivado e despublicado ficam de fora), do mais novo ao mais antigo, só as
/// categorias com anúncio publicado, o limite pedido, e o arquivo inteiro servido pelo site: o anúncio arquivado sai do mapa na hora.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SitemapQueryTests
#pragma warning restore CA1515
{
    private const int Cars = 33;
    private const int Motorcycles = 36;
    private const int General = 86;

    private static readonly DateTime Base = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private static Ad Build(int author, string title, byte status, DateTime at, int category)
    {
        Ad ad = Ad.CreateDraft(title, author);
        ad.SetCategory(category);
        ad.SetPrice(5_000_00);
        ad.SetLocation("13015100", "Campinas", "SP", false);
        switch (status)
        {
            case AdStatus.InReview:
                ad.ApplyTransition(AdStatus.InReview, author, at, null);
                break;
            case AdStatus.Published:
                ad.ApplyTransition(AdStatus.InReview, author, at, null);
                ad.ApplyTransition(AdStatus.Published, author, at, null);
                break;
            case AdStatus.Rejected:
                ad.ApplyTransition(AdStatus.InReview, author, at, null);
                ad.ApplyTransition(AdStatus.Rejected, author, at, "Fotos escuras");
                break;
            case AdStatus.Archived:
                ad.ApplyTransition(AdStatus.Archived, author, at, null);
                break;
        }

        return ad;
    }

    private static async Task<int> AddAsync(string connection, int author, string title, byte status, int category = General, DateTime? at = null)
    {
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        Ad ad = Build(author, title, status, at ?? Base, category);
        context.Ads.Add(ad);
        await context.SaveChangesAsync();
        return ad.Id;
    }

    private static SitemapReadRepository Repository(string connection) => new(new SqlConnection(connection));

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Anuncios_SoPublicados_RascunhoRevisaoRejeitadoArquivadoEDespublicadoFicamDeFora_NoMaisNovoPrimeiro()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        foreach ((string title, byte status) in new[] { ("Rascunho", AdStatus.Draft), ("Em revisão", AdStatus.InReview), ("Rejeitado", AdStatus.Rejected), ("Arquivado", AdStatus.Archived) })
        {
            await AddAsync(connection, author, title, status, at: Base.AddDays(100));
        }

        int unpublished = await AddAsync(connection, author, "Despublicado", AdStatus.Published);
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            Ad ad = await context.Ads.SingleAsync(a => a.Id == unpublished);
            ad.ApplyTransition(AdStatus.Draft, author, Base, null);
            await context.SaveChangesAsync();
        }

        int old = await AddAsync(connection, author, "Mais antigo", AdStatus.Published, at: Base.AddDays(1));
        int recent = await AddAsync(connection, author, "Mais novo", AdStatus.Published, at: Base.AddDays(5));

        IReadOnlyList<SitemapAd> ads = await Repository(connection).PublishedAdsAsync(50, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { recent, old }, ads.Select(a => a.Id).ToArray());
        Assert.AreEqual("Mais novo", ads[0].Title);
        Assert.AreEqual(Base.AddDays(5), ads[0].PublishedAt);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Anuncios_RespeitaOLimitePedido_ELimiteZeroNaoConsulta()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        for (int i = 1; i <= 5; i++)
        {
            await AddAsync(connection, author, $"Anúncio {i}", AdStatus.Published, at: Base.AddDays(i));
        }

        SitemapReadRepository repository = Repository(connection);

        Assert.HasCount(3, await repository.PublishedAdsAsync(3, CancellationToken.None));
        Assert.HasCount(0, await repository.PublishedAdsAsync(0, CancellationToken.None));
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Categorias_SoAsQueTemAnuncioPublicado_SemRepetir()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        await AddAsync(connection, author, "Carro 1", AdStatus.Published, Cars);
        await AddAsync(connection, author, "Carro 2", AdStatus.Published, Cars);
        await AddAsync(connection, author, "Moto só em revisão", AdStatus.InReview, Motorcycles);
        await AddAsync(connection, author, "Livro arquivado", AdStatus.Archived, General);

        IReadOnlyList<int> categories = await Repository(connection).PublishedCategoryIdsAsync(CancellationToken.None);

        CollectionAssert.AreEqual(new[] { Cars }, categories.ToArray());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ArquivoInteiro_ListaSoPublicados_ArquivarTiraDoMapaNaHora_ERobotsApontaOMapa()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        AppUser writer = await factory.CreateUserAsync("mapa.redatora@exemplo.com.br", "Ana Souza", "Senha@Forte1", RoleNames.Writer);
        int car = await AddAsync(connection, writer.Id, "Honda Civic 2018", AdStatus.Published, Cars, Base.AddDays(2));
        int book = await AddAsync(connection, writer.Id, "Livro de Direito", AdStatus.Published, General, Base.AddDays(3));
        await AddAsync(connection, writer.Id, "Carro ainda em revisão", AdStatus.InReview, Motorcycles, Base.AddDays(4));
        await AddAsync(connection, writer.Id, "Carro rascunho", AdStatus.Draft, Motorcycles, Base.AddDays(5));
        using HttpClient visitor = factory.CreateBrowser();

        HttpResponseMessage first = await visitor.GetAsync("/sitemap.xml");
        string[] before = Locs(XDocument.Parse(await first.Content.ReadAsStringAsync()));

        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            Ad ad = await context.Ads.SingleAsync(a => a.Id == book);
            ad.ApplyTransition(AdStatus.Archived, writer.Id, Base.AddDays(6), null);
            await context.SaveChangesAsync();
        }

        string[] after = Locs(XDocument.Parse(await visitor.GetStringAsync("/sitemap.xml")));
        string robots = await visitor.GetStringAsync("/robots.txt");

        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
        Assert.IsTrue(before.Any(l => l.Contains($"/anuncio/{car}/honda-civic-2018", StringComparison.Ordinal)));
        Assert.IsTrue(before.Any(l => l.Contains($"/anuncio/{book}/livro-de-direito", StringComparison.Ordinal)));
        Assert.IsFalse(before.Any(l => l.Contains("revisao", StringComparison.Ordinal) || l.Contains("rascunho", StringComparison.Ordinal)), "só publicados");
        Assert.IsFalse(after.Any(l => l.Contains($"/anuncio/{book}/", StringComparison.Ordinal)), "arquivado sai do mapa na hora");
        Assert.IsTrue(after.Any(l => l.Contains($"/anuncio/{car}/", StringComparison.Ordinal)));
        Assert.IsTrue(after.Length < before.Length);
        StringAssert.Contains(robots, "Sitemap: ");
        StringAssert.Contains(robots, "/sitemap.xml");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Volume_1500Anuncios_MapaInteiroEmMenosDe3Segundos_SemRepetirEndereco()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        AppUser writer = await factory.CreateUserAsync("mapa.volume@exemplo.com.br", "Ana Souza", "Senha@Forte1", RoleNames.Writer);
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            for (int i = 1; i <= 1500; i++)
            {
                context.Ads.Add(Build(writer.Id, $"Anúncio de volume {i}", AdStatus.Published, Base.AddMinutes(i), i % 2 == 0 ? Cars : General));
            }

            await context.SaveChangesAsync();
        }

        using HttpClient visitor = factory.CreateBrowser();
        Stopwatch watch = Stopwatch.StartNew();
        string body = await visitor.GetStringAsync("/sitemap.xml");
        watch.Stop();

        string[] locs = Locs(XDocument.Parse(body));
        Assert.IsTrue(locs.Length >= 1501, locs.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.AreEqual(locs.Length, locs.Distinct().Count(), "nenhum endereço repetido");
        Assert.IsLessThan(3000, watch.ElapsedMilliseconds, "o mapa de 1.500 anúncios leva " + watch.ElapsedMilliseconds + " ms");
    }

    private static string[] Locs(XDocument xml) => [.. xml.Root!.Elements(Ns + "url").Select(u => u.Element(Ns + "loc")!.Value)];
}
