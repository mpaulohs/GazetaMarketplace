using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Ads;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// Os favoritos (US-005) no T-SQL de verdade: a busca por ids devolve só anúncios publicados (rascunho, em revisão, rejeitado, arquivado e despublicado ficam de fora),
/// a API e a página devolvem na ordem pedida, e mais de 100 ids é recusado antes de consultar.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class FavoritesQueryTests
#pragma warning restore CA1515
{
    private static readonly DateTime Base = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    private static async Task<int> AddAsync(string connection, int author, string title, byte status)
    {
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        Ad ad = Ad.CreateDraft(title, author);
        ad.SetCategory(86);
        ad.SetPrice(5_000_00);
        ad.SetLocation("13015100", "Campinas", "SP", false);
        switch (status)
        {
            case AdStatus.InReview:
                ad.ApplyTransition(AdStatus.InReview, author, Base, null);
                break;
            case AdStatus.Published:
                ad.ApplyTransition(AdStatus.InReview, author, Base, null);
                ad.ApplyTransition(AdStatus.Published, author, Base, null);
                break;
            case AdStatus.Rejected:
                ad.ApplyTransition(AdStatus.InReview, author, Base, null);
                ad.ApplyTransition(AdStatus.Rejected, author, Base, "Fotos escuras");
                break;
            case AdStatus.Archived:
                ad.ApplyTransition(AdStatus.Archived, author, Base, null);
                break;
        }

        context.Ads.Add(ad);
        await context.SaveChangesAsync();
        return ad.Id;
    }

    private static async Task<List<int>> SeedAsync(string connection, int author, IEnumerable<string> titles)
    {
        List<int> ids = [];
        foreach (string title in titles)
        {
            ids.Add(await AddAsync(connection, author, title, AdStatus.Published));
        }

        return ids;
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ByIds_SoPublicados_RascunhoRevisaoRejeitadoArquivadoEDespublicadoFicamDeFora()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        int published = await AddAsync(connection, author, "Publicado", AdStatus.Published);
        int draft = await AddAsync(connection, author, "Rascunho", AdStatus.Draft);
        int review = await AddAsync(connection, author, "Em revisão", AdStatus.InReview);
        int rejected = await AddAsync(connection, author, "Rejeitado", AdStatus.Rejected);
        int archived = await AddAsync(connection, author, "Arquivado", AdStatus.Archived);
        int unpublished = await AddAsync(connection, author, "Despublicado", AdStatus.Published);
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            Ad ad = await context.Ads.SingleAsync(a => a.Id == unpublished);
            ad.ApplyTransition(AdStatus.Draft, author, Base, null);
            await context.SaveChangesAsync();
        }

        IReadOnlyList<ShowcaseRow> rows = await new ShowcaseReadRepository(new SqlConnection(connection))
            .ByIdsAsync([published, draft, review, rejected, archived, unpublished, 999_999], CancellationToken.None);

        CollectionAssert.AreEqual(new[] { published }, rows.Select(r => r.Id).ToArray());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ByIds_Com100Ids_Devolve100_ECapaDaPrimeiraFoto()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AdData.AddUserAsync(connection);
        List<int> ids = await SeedAsync(connection, author, Enumerable.Range(1, 100).Select(n => $"Anúncio {n}"));
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            context.AdPhotos.Add(new AdPhoto { AdId = ids[0], SortOrder = 0, StorageKey = $"fav-{Guid.NewGuid():N}.webp", Width = 1600, Height = 1200, SizeBytes = 1000, CreatedAt = Base });
            await context.SaveChangesAsync();
        }

        IReadOnlyList<ShowcaseRow> rows = await new ShowcaseReadRepository(new SqlConnection(connection)).ByIdsAsync(ids, CancellationToken.None);

        Assert.HasCount(100, rows);
        Assert.IsNotNull(rows.Single(r => r.Id == ids[0]).CoverPhotoId, "a primeira foto é a capa");
        Assert.IsNull(rows.Single(r => r.Id == ids[1]).CoverPhotoId, "sem foto, sem capa");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Api_DevolveNaOrdemPedida_SoPublicados_ERecusaMaisDe100()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        AppUser writer = await factory.CreateUserAsync("favoritos.redatora@exemplo.com.br", "Ana Souza", "Senha@Forte1", RoleNames.Writer);
        int a = await AddAsync(connection, writer.Id, "Anúncio A", AdStatus.Published);
        int b = await AddAsync(connection, writer.Id, "Anúncio B", AdStatus.Published);
        int c = await AddAsync(connection, writer.Id, "Anúncio C", AdStatus.Published);
        int draft = await AddAsync(connection, writer.Id, "Rascunho", AdStatus.Draft);
        using HttpClient visitor = factory.CreateBrowser();

        HttpResponseMessage ok = await visitor.GetAsync($"/api/v1/ads?ids={c},{draft},{a},{b},{a}");
        HttpResponseMessage tooMany = await visitor.GetAsync("/api/v1/ads?ids=" + string.Join(',', Enumerable.Range(1, 101)));
        HttpResponseMessage exactly100 = await visitor.GetAsync("/api/v1/ads?ids=" + string.Join(',', Enumerable.Range(1, 100)));

        Assert.AreEqual(HttpStatusCode.OK, ok.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await ok.Content.ReadAsStringAsync());
        CollectionAssert.AreEqual(new[] { c, a, b }, body.RootElement.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToArray(), "na ordem pedida, sem o rascunho e sem repetir");
        Assert.AreEqual(3, body.RootElement.GetProperty("totalCount").GetInt32());
        Assert.AreEqual(HttpStatusCode.BadRequest, tooMany.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, exactly100.StatusCode);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task PaginaDeLista_MostraOsCardsPublicadosNaOrdemPedida_ComORemover()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        AppUser writer = await factory.CreateUserAsync("favoritos.lista@exemplo.com.br", "Ana Souza", "Senha@Forte1", RoleNames.Writer);
        int a = await AddAsync(connection, writer.Id, "Primeiro anúncio", AdStatus.Published);
        int b = await AddAsync(connection, writer.Id, "Segundo anúncio", AdStatus.Published);
        int archived = await AddAsync(connection, writer.Id, "Anúncio arquivado", AdStatus.Archived);
        using HttpClient visitor = factory.CreateBrowser();

        string html = WebUtility.HtmlDecode(await visitor.GetStringAsync($"/favoritos/lista?ids={b},{archived},{a}"));

        Assert.IsTrue(html.IndexOf("Segundo anúncio", StringComparison.Ordinal) < html.IndexOf("Primeiro anúncio", StringComparison.Ordinal), "na ordem pedida");
        Assert.IsFalse(html.Contains("Anúncio arquivado", StringComparison.Ordinal), "arquivado não aparece");
        StringAssert.Contains(html, "data-favorite-remove");
    }
}
