using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Fields;
using GazetaMarketplace.Web.Models;
using GazetaMarketplace.Web.Tests.Ads;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GazetaMarketplace.Web.Tests.Review.ReviewSupport;

namespace GazetaMarketplace.Web.Tests.Detail;

/// <summary>As regras puras da página do anúncio: título do bloco de características, texto da meta description e a montagem única (<see cref="AdDetailFactory"/>) que a pré-visualização e a página pública compartilham.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class AdDetailRulesTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void TituloDasCaracteristicas_VeiculosTerrenoEOResto_PorGrupoDeCampos()
    {
        string[] vehicles = [FieldGroupKeys.Cars, FieldGroupKeys.Motorcycles, FieldGroupKeys.TrucksAndBuses, FieldGroupKeys.BoatsAndAircraft];
        foreach (string key in vehicles)
        {
            Assert.AreEqual("Características do veículo", AdSpecTitles.For(key, 33), key);
        }

        Assert.AreEqual("Características do terreno", AdSpecTitles.For(FieldGroupKeys.RealEstate, 30), "Terrenos, sítios e fazendas");
        foreach (int other in new[] { 26, 27, 31 })
        {
            Assert.AreEqual("Características", AdSpecTitles.For(FieldGroupKeys.RealEstate, other), $"Imóveis, categoria {other}");
        }

        Assert.AreEqual("Características", AdSpecTitles.For(FieldGroupKeys.RealEstate, null));
        string[] others = [.. typeof(FieldGroupKeys).GetFields().Select(f => (string)f.GetRawConstantValue()).Where(k => !vehicles.Contains(k) && k != FieldGroupKeys.RealEstate)];
        Assert.IsTrue(others.Length >= 13, "todos os outros grupos");
        foreach (string key in others)
        {
            Assert.AreEqual("Características", AdSpecTitles.For(key, 30), key + " não vira ficha de terreno só por estar na categoria 30");
        }
    }

    [TestMethod]
    public void MetaDescription_TextoPuroEmLinhaUnica_Ate160Caracteres()
    {
        Assert.AreEqual("Único dono, revisões feitas na concessionária.", AdMetaDescription.For("T", "  Único dono,\n\n  revisões\tfeitas   na concessionária.  ", "Campinas/SP"));
        Assert.AreEqual("Honda Civic 2018 — Campinas/SP", AdMetaDescription.For("Honda Civic 2018", null, "Campinas/SP"), "sem descrição: título e local");
        Assert.AreEqual("Honda Civic 2018", AdMetaDescription.For("Honda Civic 2018", "   ", null), "sem descrição e sem local: o título");

        string exact = new('a', 160);
        Assert.AreEqual(exact, AdMetaDescription.For("T", exact, null), "160 exatos passam inteiros");
    }

    [TestMethod]
    public void MetaDescription_Passou160_TerminaEmReticenciasSemPartirPalavraNemParDeSubstituicao()
    {
        string longText = string.Join(" ", Enumerable.Repeat("palavra", 40));

        string cut = AdMetaDescription.For("T", longText, null);

        Assert.IsTrue(cut.Length <= 160, $"{cut.Length} caracteres");
        Assert.IsTrue(cut.EndsWith('…'));
        Assert.IsTrue(cut[..^1].EndsWith("palavra", StringComparison.Ordinal), "termina numa palavra inteira");

        string emoji = new string('a', 158) + "😀😀😀";
        string withEmoji = AdMetaDescription.For("T", emoji, null);
        Assert.IsTrue(withEmoji.Length <= 160);
        for (int i = 0; i < withEmoji.Length; i++)
        {
            if (char.IsHighSurrogate(withEmoji[i]))
            {
                Assert.IsTrue(i + 1 < withEmoji.Length && char.IsLowSurrogate(withEmoji[i + 1]), "par de substituição inteiro");
            }
        }

        string noSpaces = new('x', 400);
        Assert.AreEqual(160, AdMetaDescription.For("T", noSpaces, null).Length, "sem espaços, corta em 159 e põe a reticência");
    }

    private static async Task<int> AddAdAsync(DraftSite site, byte status, int categoryId, Action<Ad> configure)
    {
        int author = await site.UserIdAsync(Ana);
        int admin = await site.UserIdAsync(PanelFixture.AdminEmail);
        Ad ad = Ad.CreateDraft("Anúncio compartilhado", author);
        ad.SetCategory(categoryId);
        ad.SetText("Anúncio compartilhado", "Descrição única.");
        ad.SetPrice(1_250_050);
        ad.SetLocation("13015100", "Campinas", "SP", false);
        configure(ad);
        DateTime when = new(2026, 9, 12, 15, 0, 0, DateTimeKind.Utc);
        ad.ApplyTransition(AdStatus.InReview, author, when, null);
        if (status == AdStatus.Published)
        {
            ad.ApplyTransition(AdStatus.Published, admin, when, null);
        }

        return await site.Harness.WithDbAsync(async db =>
        {
            db.Ads.Add(ad);
            await db.SaveChangesAsync();
            db.AdPhotos.Add(new AdPhoto { AdId = ad.Id, SortOrder = 0, StorageKey = $"fabrica-{Guid.NewGuid():N}.webp", Width = 800, Height = 600, SizeBytes = 100, CreatedAt = when });
            await db.SaveChangesAsync();
            return ad.Id;
        });
    }

    [TestMethod]
    public async Task Montagem_PreVisualizacaoEPaginaPublica_CompartilhamValorLocalDescricaoECaracteristicas()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAdAsync(site, AdStatus.Published, 33, ad => ad.SetAttributes(new AdAttributes().Set("brandId", 1).Set("modelId", 11).Set("modelYear", 2019).Set("km", 45000)));

        using IServiceScope scope = site.Harness.Factory.Services.CreateScope();
        AdDetailFactory factory = scope.ServiceProvider.GetRequiredService<AdDetailFactory>();
        Ad ad = await site.Harness.WithDbAsync(db => Task.FromResult(db.Ads.Single(a => a.Id == id)));

        AdDetail preview = await factory.CreateAsync(ad, 2, false, CancellationToken.None);
        AdDetail publicPage = await factory.CreateAsync(ad, 1, true, CancellationToken.None);

        Assert.AreEqual(preview.Body.Title, publicPage.Body.Title);
        Assert.AreEqual(preview.Body.Value.Text, publicPage.Body.Value.Text);
        Assert.AreEqual("R$ 12.500,50", publicPage.Body.Value.Text);
        Assert.AreEqual(preview.Body.Location, publicPage.Body.Location);
        Assert.AreEqual(preview.Body.Description, publicPage.Body.Description);
        CollectionAssert.AreEqual(preview.Body.Specs.Select(s => s.Label + s.Value).ToArray(), publicPage.Body.Specs.Select(s => s.Label + s.Value).ToArray());
        Assert.AreEqual("Características do veículo", publicPage.Body.SpecsTitle);
        Assert.AreEqual(preview.Body.SpecsTitle, publicPage.Body.SpecsTitle);
        Assert.AreEqual(preview.Photos.Count, publicPage.Photos.Count);
        Assert.AreEqual(1, publicPage.Photos.Count);
        Assert.AreEqual("Automóveis, Peças e Acessórios › Carros, vans e utilitários", publicPage.CategoryPath);
        Assert.AreEqual(2, preview.Body.HeadingLevel);
        Assert.AreEqual(1, publicPage.Body.HeadingLevel);
        Assert.IsNull(preview.Body.CategoryName, "a pré-visualização mostra o caminho em outro lugar");
        Assert.IsNull(preview.Body.PublishedOn);
        Assert.AreEqual("Carros, vans e utilitários", publicPage.Body.CategoryName);
        Assert.AreEqual("12/09/2026", publicPage.Body.PublishedOn);
    }

    [TestMethod]
    public async Task Montagem_AnuncioAindaNaoPublicado_NaoTemDataDePublicacao()
    {
        using DraftSite site = await DraftSite.StartAsync();
        int id = await AddAdAsync(site, AdStatus.InReview, 86, ad => ad.SetAttributes(new AdAttributes().Set("conditionId", 2)));
        using IServiceScope scope = site.Harness.Factory.Services.CreateScope();
        Ad ad = await site.Harness.WithDbAsync(db => Task.FromResult(db.Ads.Single(a => a.Id == id)));

        AdDetail detail = await scope.ServiceProvider.GetRequiredService<AdDetailFactory>().CreateAsync(ad, 1, true, CancellationToken.None);

        Assert.IsNull(detail.Body.PublishedOn);
    }

    [TestMethod]
    public async Task Montagem_CategoriaQueSumiuDaArvore_NaoQuebra_SemCaminhoENemNomeDeCategoria()
    {
        using DraftSite site = await DraftSite.StartAsync();
        Ad ad = Ad.CreateDraft("Sem categoria", await site.UserIdAsync(Ana));
        ad.SetText("Sem categoria", "Texto");
        using IServiceScope scope = site.Harness.Factory.Services.CreateScope();

        AdDetail detail = await scope.ServiceProvider.GetRequiredService<AdDetailFactory>().CreateAsync(ad, 1, true, CancellationToken.None);

        Assert.IsEmpty(detail.Path);
        Assert.IsNull(detail.CategoryPath);
        Assert.IsNull(detail.Body.CategoryName);
        Assert.AreEqual("Características", detail.Body.SpecsTitle);
    }
}
