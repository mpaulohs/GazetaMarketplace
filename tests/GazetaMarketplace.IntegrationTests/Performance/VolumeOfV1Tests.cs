using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Performance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests.Performance;

/// <summary>
/// NFR-04 (e a metade de servidor das NFR-01 e NFR-05): com o volume da v1 (cerca de 200 anúncios ativos de vários tipos, com fotos, em várias cidades) num SQL Server de verdade, a busca e o detalhe respondem
/// em menos de 500 ms em 95% das requisições. Mede o tempo do servidor (o site inteiro em processo, banco em contêiner), depois de aquecer; os números ficam na saída do teste e o limite vem de <c>budgets.json</c>.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class VolumeOfV1Tests
#pragma warning restore CA1515
{
    private const int Cars = 33;
    private const int Books = 86;
    private const int Services = 66;
    private const int Jobs = 96;

    private static readonly DateTime Base = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly (string City, string Uf, string Cep)[] Places = [("Campinas", "SP", "13015100"), ("São Paulo", "SP", "01310100"), ("Rio de Janeiro", "RJ", "20040020"), ("Belo Horizonte", "MG", "30130010"), ("Curitiba", "PR", "80010000")];
    private static readonly string[] Words = ["Honda Civic", "Livro de Direito", "Diarista", "Pizzaiolo", "Bicicleta", "Notebook", "Sofá de couro", "Violão", "Geladeira", "Moto 150"];

    private static async Task<List<(int Id, string Title)>> SeedAsync(string connection, int author, int count)
    {
        Random random = new(42);
        int[] categories = [Cars, Books, Services, Jobs];
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        for (int i = 0; i < count; i++)
        {
            int category = categories[i % categories.Length];
            (string city, string uf, string cep) = Places[i % Places.Length];
            string title = $"{Words[i % Words.Length]} {i} com experiência";
            Ad ad = Ad.CreateDraft(title, author);
            ad.SetCategory(category);
            ad.SetText(title, $"Descrição de {title}. " + string.Concat(Enumerable.Repeat("Texto do anúncio com detalhes do estado, da entrega e da forma de pagamento. ", 6)));
            ad.SetPrice(category is Services or Jobs ? null : random.Next(5_000, 9_000_000));
            ad.SetLocation(cep, city, uf, false);
            if (category == Books)
            {
                ad.SetAttributes(new AdAttributes().Set("conditionId", 2));
            }

            DateTime when = Base.AddMinutes(i);
            ad.ApplyTransition(AdStatus.InReview, author, when, null);
            ad.ApplyTransition(AdStatus.Published, author, when, null);
            context.Ads.Add(ad);
        }

        await context.SaveChangesAsync();
        List<Ad> saved = [.. context.ChangeTracker.Entries<Ad>().Select(e => e.Entity)];
        foreach (Ad ad in saved.Where(a => a.CategoryId != Jobs))
        {
            // Vaga não tem foto; os demais têm de 1 a 5 (a de 1600 px que a 3.4 grava)
            for (int p = 0; p < 1 + (ad.Id % 5); p++)
            {
                context.AdPhotos.Add(new AdPhoto { AdId = ad.Id, SortOrder = p, StorageKey = $"vol-{Guid.NewGuid():N}.webp", Width = 1600, Height = 1200, SizeBytes = 120_000, CreatedAt = Base });
            }
        }

        await context.SaveChangesAsync();
        return [.. saved.Select(a => (a.Id, a.Title))];
    }

    private static double Percentile(List<double> samples, double p)
    {
        double[] sorted = [.. samples.OrderBy(x => x)];
        return sorted[(int)Math.Ceiling(p * sorted.Length) - 1];
    }

    private static async Task<List<double>> MeasureAsync(HttpClient client, IReadOnlyList<string> urls, int warmup, int measured)
    {
        for (int i = 0; i < warmup; i++)
        {
            using HttpResponseMessage response = await client.GetAsync(urls[i % urls.Count]);
            _ = await response.Content.ReadAsStringAsync();
        }

        List<double> samples = [];
        for (int i = 0; i < measured; i++)
        {
            string url = urls[i % urls.Count];
            long start = Stopwatch.GetTimestamp();
            using HttpResponseMessage response = await client.GetAsync(url);
            string body = await response.Content.ReadAsStringAsync();
            samples.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, url);
            Assert.IsNotEmpty(body, url);
        }

        return samples;
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task SearchAndDetail_WithAbout200ActiveAds_P95Under500ms()
    {
        Budgets budgets = Budgets.Current;
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        List<(int Id, string Title)> ads = await SeedAsync(connection, author, budgets.VolumeActiveAds);
        // O limite global de pedidos por minuto (100 por IP) é do site e tem teste próprio; aqui são 420 pedidos seguidos de um só endereço e o que se mede é o servidor
        await using IntegrationWebFactory factory = new(connection, extraConfiguration: new Dictionary<string, string> { ["RateLimiting:GlobalPerMinute"] = "100000" });
        using HttpClient visitor = factory.CreateBrowser();
        CategoryTreeSnapshot tree = await factory.Services.GetRequiredService<ICategoryTree>().GetAsync(default);
        string books = tree.Find(Books).Slug;
        string cars = tree.Find(Cars).Slug;

        string[] searches =
        [
            "/busca", "/busca?q=livro", "/busca?q=experiencia", "/busca?q=civic&uf=SP", "/busca?uf=SP&ordem=menor-preco", "/busca?uf=RJ&ordem=maior-preco&pagina=2",
            "/busca?precoMin=1000&precoMax=60000&ordem=menor-preco", "/busca?categoria=" + books, "/busca?categoria=" + cars + "&uf=SP", "/busca?q=diarista&uf=SP&cidade=Campinas",
            "/", "/categoria/" + books, "/categoria/" + cars + "?pagina=2"
        ];
        string[] details = [.. ads.Select(a => AdRoutes.Detail(a.Id, a.Title))];

        List<double> search = await MeasureAsync(visitor, searches, budgets.WarmupRequests, budgets.MeasuredRequests);
        List<double> detail = await MeasureAsync(visitor, details, budgets.WarmupRequests, budgets.MeasuredRequests);

        double searchP95 = Percentile(search, 0.95);
        double detailP95 = Percentile(detail, 0.95);
        string numbers = $"VOLUME: {ads.Count} anúncios publicados · busca/início/categoria p50={Percentile(search, 0.5):F1} ms p95={searchP95:F1} ms máx={search.Max():F1} ms ({search.Count} pedidos) · detalhe p50={Percentile(detail, 0.5):F1} ms p95={detailP95:F1} ms máx={detail.Max():F1} ms ({detail.Count} pedidos) · limite p95 {budgets.ServerP95MaxMs} ms";
        Console.WriteLine(numbers);
        Assert.AreEqual(budgets.VolumeActiveAds, ads.Count, "o volume de teste é o do orçamento");
        Assert.IsLessThan(budgets.ServerP95MaxMs, searchP95, "p95 da busca (e de início e categoria). " + numbers);
        Assert.IsLessThan(budgets.ServerP95MaxMs, detailP95, "p95 do detalhe. " + numbers);
    }
}
