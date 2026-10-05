using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
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
/// A vitrine pública (US-001) no T-SQL de verdade (ADR-004): só publicados, ordem por data de publicação, o limite de 12, as categorias descendentes, a capa certa, a paginação
/// e o plano de execução; e as duas páginas inteiras (serviço, repositório e tela) para um visitante sem login.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ShowcaseQueryTests
#pragma warning restore CA1515
{
    private const int Cars = 33;
    private const int Motorcycles = 36;
    private const int General = 86;
    private static int _users;
    private static int _photos;

    private static readonly DateTime Base = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    private static async Task<int> AddUserAsync(string connection)
    {
        int n = Interlocked.Increment(ref _users);
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        AppUser user = new() { UserName = $"vitrine{n}@exemplo.com.br", NormalizedUserName = $"VITRINE{n}@EXEMPLO.COM.BR", Email = $"vitrine{n}@exemplo.com.br", FullName = $"Autora {n}" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    private static Ad Build(int author, string title, byte status, DateTime publishedAt, int? category, long? price = 5_000_00)
    {
        Ad ad = Ad.CreateDraft(title, author);
        ad.SetCategory(category);
        ad.SetPrice(price);
        ad.SetLocation("13015100", "Campinas", "SP", false);
        switch (status)
        {
            case AdStatus.InReview:
                ad.ApplyTransition(AdStatus.InReview, author, publishedAt, null);
                break;
            case AdStatus.Published:
                ad.ApplyTransition(AdStatus.InReview, author, publishedAt, null);
                ad.ApplyTransition(AdStatus.Published, author, publishedAt, null);
                break;
            case AdStatus.Rejected:
                ad.ApplyTransition(AdStatus.InReview, author, publishedAt, null);
                ad.ApplyTransition(AdStatus.Rejected, author, publishedAt, "Fotos escuras");
                break;
            case AdStatus.Archived:
                ad.ApplyTransition(AdStatus.Archived, author, publishedAt, null);
                break;
        }

        return ad;
    }

    private static async Task<int> AddAsync(string connection, int author, string title, byte status = AdStatus.Published, DateTime? publishedAt = null, int? category = General)
    {
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        Ad ad = Build(author, title, status, publishedAt ?? Base, category);
        context.Ads.Add(ad);
        await context.SaveChangesAsync();
        return ad.Id;
    }

    private static async Task<int> AddPhotoAsync(string connection, int adId, int sortOrder, int width = 1600, int height = 1200)
    {
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        AdPhoto photo = new() { AdId = adId, SortOrder = sortOrder, StorageKey = $"vitrine-{Interlocked.Increment(ref _photos)}-{Guid.NewGuid():N}.webp", Width = width, Height = height, SizeBytes = 1000, CreatedAt = Base };
        context.AdPhotos.Add(photo);
        await context.SaveChangesAsync();
        return photo.Id;
    }

    private static ShowcaseReadRepository Repository(string connection) => new(new SqlConnection(connection));

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Recentes_SoPublicados_NaOrdemDaPublicacao_NoMaximo12_EmpateDesempataPeloIdMaisNovo()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AddUserAsync(connection);
        foreach ((string title, byte status) in new[] { ("Rascunho", AdStatus.Draft), ("Em revisão", AdStatus.InReview), ("Rejeitado", AdStatus.Rejected), ("Arquivado", AdStatus.Archived) })
        {
            await AddAsync(connection, author, title, status, Base.AddDays(100));
        }

        // Publicado e depois despublicado (volta a Rascunho): não aparece
        int unpublished = await AddAsync(connection, author, "Despublicado");
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            Ad ad = await context.Ads.SingleAsync(a => a.Id == unpublished);
            ad.ApplyTransition(AdStatus.Draft, author, Base, null);
            await context.SaveChangesAsync();
        }

        List<int> published = [];
        for (int day = 1; day <= 14; day++)
        {
            published.Add(await AddAsync(connection, author, $"Publicado {day:00}", publishedAt: Base.AddDays(day)));
        }

        int tieOld = await AddAsync(connection, author, "Empate A", publishedAt: Base.AddDays(30));
        int tieNew = await AddAsync(connection, author, "Empate B", publishedAt: Base.AddDays(30));

        IReadOnlyList<ShowcaseRow> recent = await Repository(connection).RecentAsync(12, CancellationToken.None);

        Assert.HasCount(12, recent);
        CollectionAssert.AreEqual(
            new[] { tieNew, tieOld, published[13], published[12], published[11], published[10], published[9], published[8], published[7], published[6], published[5], published[4] },
            recent.Select(r => r.Id).ToArray(),
            "o mais novo primeiro; no empate de data, o de maior id");
        Assert.IsTrue(recent.All(r => r.Title.StartsWith("Publicado", StringComparison.Ordinal) || r.Title.StartsWith("Empate", StringComparison.Ordinal)), "nenhuma outra situação");
        Assert.AreEqual(3, (await Repository(connection).RecentAsync(3, CancellationToken.None)).Count, "o limite pedido vale");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Categoria_AnunciosDeTodasAsCategoriasPedidas_SoPublicados_ComTotalEPaginacao()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AddUserAsync(connection);
        int[] cars = new int[5];
        for (int i = 0; i < cars.Length; i++)
        {
            cars[i] = await AddAsync(connection, author, $"Carro {i}", publishedAt: Base.AddDays(i), category: Cars);
        }

        int moto = await AddAsync(connection, author, "Moto", publishedAt: Base.AddDays(10), category: Motorcycles);
        await AddAsync(connection, author, "Livro", publishedAt: Base.AddDays(11), category: General);
        await AddAsync(connection, author, "Carro em revisão", AdStatus.InReview, Base.AddDays(12), Cars);
        await AddAsync(connection, author, "Carro arquivado", AdStatus.Archived, Base.AddDays(13), Cars);

        ShowcaseRows carsOnly = await Repository(connection).ByCategoryAsync([Cars], 1, 24, CancellationToken.None);
        ShowcaseRows both = await Repository(connection).ByCategoryAsync([Cars, Motorcycles], 1, 24, CancellationToken.None);
        ShowcaseRows firstPage = await Repository(connection).ByCategoryAsync([Cars, Motorcycles], 1, 4, CancellationToken.None);
        ShowcaseRows secondPage = await Repository(connection).ByCategoryAsync([Cars, Motorcycles], 2, 4, CancellationToken.None);
        ShowcaseRows beyond = await Repository(connection).ByCategoryAsync([Cars, Motorcycles], 9, 4, CancellationToken.None);

        Assert.AreEqual(5, carsOnly.Total, "só os publicados da categoria");
        CollectionAssert.AreEqual(new[] { cars[4], cars[3], cars[2], cars[1], cars[0] }, carsOnly.Rows.Select(r => r.Id).ToArray());
        Assert.AreEqual(6, both.Total);
        Assert.AreEqual(moto, both.Rows[0].Id, "a mais recente das duas categorias vem primeiro");
        CollectionAssert.AreEqual(new[] { moto, cars[4], cars[3], cars[2] }, firstPage.Rows.Select(r => r.Id).ToArray());
        CollectionAssert.AreEqual(new[] { cars[1], cars[0] }, secondPage.Rows.Select(r => r.Id).ToArray());
        Assert.AreEqual(6, secondPage.Total);
        Assert.IsEmpty(beyond.Rows);
        Assert.AreEqual(6, beyond.Total, "além do fim: sem linhas, com o total certo (o serviço então mostra a última)");
        Assert.AreEqual(0, (await Repository(connection).ByCategoryAsync([General + 1000], 1, 24, CancellationToken.None)).Total);
        Assert.AreEqual(0, (await Repository(connection).ByCategoryAsync([Cars], ShowcaseFilters.MaxPage, 24, CancellationToken.None)).Rows.Count, "a página no limite não estoura");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Capa_EAFotoDeMenorPosicao_SemFotoVemVazia_EPosicaoRepetidaNaoDuplicaOAnuncio()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AddUserAsync(connection);
        int withPhotos = await AddAsync(connection, author, "Com fotos", publishedAt: Base.AddDays(3));
        await AddPhotoAsync(connection, withPhotos, 2, 800, 600);
        int cover = await AddPhotoAsync(connection, withPhotos, 0, 1600, 900);
        await AddPhotoAsync(connection, withPhotos, 1, 700, 700);
        int none = await AddAsync(connection, author, "Sem fotos", publishedAt: Base.AddDays(2));
        int repeated = await AddAsync(connection, author, "Posição repetida", publishedAt: Base.AddDays(1));
        int firstOfTwo = await AddPhotoAsync(connection, repeated, 0);
        await AddPhotoAsync(connection, repeated, 0);

        IReadOnlyList<ShowcaseRow> recent = await Repository(connection).RecentAsync(12, CancellationToken.None);

        Assert.HasCount(3, recent, "uma linha por anúncio, mesmo com posições repetidas");
        ShowcaseRow a = recent.Single(r => r.Id == withPhotos);
        Assert.AreEqual((cover, 1600, 900), (a.CoverPhotoId, a.CoverWidth, a.CoverHeight), "a capa é a foto de posição 0");
        Assert.IsNull(recent.Single(r => r.Id == none).CoverPhotoId);
        Assert.AreEqual(firstOfTwo, recent.Single(r => r.Id == repeated).CoverPhotoId, "no empate de posição, a de menor id");
        ShowcaseRows byCategory = await Repository(connection).ByCategoryAsync([General], 1, 24, CancellationToken.None);
        Assert.AreEqual(3, byCategory.Rows.Count);
        Assert.AreEqual(3, byCategory.Total, "o total não conta as fotos");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Linha_TrazOCardELocal_ENadaDoAutor()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AddUserAsync(connection);
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            Ad ad = Build(author, "Honda Civic", AdStatus.Published, Base, Cars, 6_200_000);
            ad.SetAttributes(new AdAttributes().Set("km", 45000));
            context.Ads.Add(ad);
            await context.SaveChangesAsync();
        }

        ShowcaseRow row = (await Repository(connection).RecentAsync(12, CancellationToken.None)).Single();

        Assert.AreEqual(("Honda Civic", Cars, 6_200_000L, "Campinas", "SP"), (row.Title, row.CategoryId, row.PriceCents, row.City, row.Uf));
        StringAssert.Contains(row.Attributes, "\"km\"");
        CollectionAssert.AreEquivalent(
            new[] { "Id", "Title", "CategoryId", "PriceCents", "Attributes", "City", "Uf", "CoverPhotoId", "CoverWidth", "CoverHeight" },
            typeof(ShowcaseRow).GetProperties().Select(p => p.Name).Where(n => n != "EqualityContract").ToArray(),
            "a linha da vitrine não tem autor, e-mail nem datas de decisão");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task PlanoDeExecucao_ComMuitosAnuncios_AsDuasConsultasUsamOIndiceDeSituacao()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int author = await AddUserAsync(connection);
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            context.Ads.AddRange(Enumerable.Range(1, 6000).Select(n => Build(author, $"Volume {n}", n % 3 == 0 ? AdStatus.Published : AdStatus.Draft, Base.AddMinutes(n), n % 2 == 0 ? Cars : General)));
            await context.SaveChangesAsync();
        }

        await Repository(connection).RecentAsync(12, CancellationToken.None);
        await Repository(connection).ByCategoryAsync([Cars], 1, 24, CancellationToken.None);

        string recentPlan = await PlanAsync(connection, "%OUTER APPLY%", "%CategoryIds%");
        string categoryPlan = await PlanAsync(connection, "%CategoryIds%OUTER APPLY%", "%COUNT(*)%");

        StringAssert.Contains(recentPlan, "IX_Ads_Status_PublishedAt_Id", "mais recentes: lê o índice de situação e data de publicação, já na ordem pedida");
        Assert.IsFalse(Regex.IsMatch(recentPlan, "PhysicalOp=\"(Clustered Index Scan|Index Scan|Table Scan)\""), "mais recentes: sem varredura de tabela nem de índice");
        Assert.IsFalse(recentPlan.Contains("PhysicalOp=\"Sort\"", StringComparison.Ordinal), "mais recentes: sem ordenar à parte (o índice já entrega a ordem)");
        StringAssert.Contains(categoryPlan, "IX_Ads_Status_CategoryId_PublishedAt", "categoria: usa o índice de situação, categoria e data de publicação");
        Assert.IsFalse(categoryPlan.Contains("Clustered Index Scan", StringComparison.Ordinal), "categoria: sem varredura da tabela inteira");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task PaginasInteiras_Visitante_VePublicadosDaCategoriaEDescendentes_ComOEstadoVazioEO404()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        AppUser writer = await factory.CreateUserAsync("vitrine.redatora@exemplo.com.br", "Ana Souza", "Senha@Forte1", RoleNames.Writer);
        int car = await AddAsync(connection, writer.Id, "Honda Civic 2018", publishedAt: Base.AddDays(2), category: Cars);
        await AddPhotoAsync(connection, car, 0);
        await AddAsync(connection, writer.Id, "Moto CG 160", publishedAt: Base.AddDays(3), category: Motorcycles);
        await AddAsync(connection, writer.Id, "Livro de Direito", publishedAt: Base.AddDays(4), category: General);
        await AddAsync(connection, writer.Id, "Carro ainda em revisão", AdStatus.InReview, Base.AddDays(5), Cars);
        await AddAsync(connection, writer.Id, "Carro arquivado", AdStatus.Archived, Base.AddDays(6), Cars);
        using HttpClient visitor = factory.CreateBrowser();

        string home = await visitor.GetStringAsync("/");
        string automobiles = await visitor.GetStringAsync("/categoria/" + SlugOf(home, "Automóveis, Peças e Acessórios"));
        string motos = await visitor.GetStringAsync("/categoria/" + SlugOf(automobiles, "Motos"));
        HttpResponseMessage services = await visitor.GetAsync("/categoria/" + SlugOf(home, "Serviços"));
        HttpResponseMessage unknown = await visitor.GetAsync("/categoria/nao-existe-mais");

        foreach (string title in new[] { "Honda Civic 2018", "Moto CG 160", "Livro de Direito" })
        {
            StringAssert.Contains(home, title);
        }

        Assert.IsFalse(home.Contains("Carro ainda em revisão", StringComparison.Ordinal) || home.Contains("Carro arquivado", StringComparison.Ordinal), "só publicados");
        StringAssert.Contains(home, "src=\"/fotos/" + car);
        StringAssert.Contains(automobiles, "Honda Civic 2018");
        StringAssert.Contains(automobiles, "Moto CG 160");
        Assert.IsFalse(automobiles.Contains("Livro de Direito", StringComparison.Ordinal), "outra categoria principal");
        StringAssert.Contains(motos, "Moto CG 160");
        Assert.IsFalse(motos.Contains("Honda Civic 2018", StringComparison.Ordinal), "a subcategoria mostra só os anúncios dela");
        Assert.AreEqual(HttpStatusCode.OK, services.StatusCode);
        StringAssert.Contains(await services.Content.ReadAsStringAsync(), "Ainda não há anúncios nesta categoria");
        Assert.AreEqual(HttpStatusCode.NotFound, unknown.StatusCode);
        StringAssert.Contains(await unknown.Content.ReadAsStringAsync(), "Categoria não encontrada");
    }

    private static string SlugOf(string html, string name)
    {
        // O Razor escreve letras acentuadas como entidades; o texto é decodificado antes de procurar o nome
        Match match = Regex.Match(WebUtility.HtmlDecode(html), @"href=""/categoria/([^""]+)""[^>]*>(?:(?!</a>)[\s\S])*?" + Regex.Escape(name) + @"(?:(?!</a>)[\s\S])*?</a>");
        Assert.IsTrue(match.Success, "link da categoria " + name);
        return match.Groups[1].Value;
    }

    private static async Task<string> PlanAsync(string connection, string mustContain, string mustNotContain)
    {
        string sql = $@"
            SELECT TOP (1) CAST(p.query_plan AS NVARCHAR(MAX))
            FROM sys.dm_exec_query_stats s
            CROSS APPLY sys.dm_exec_sql_text(s.sql_handle) t
            CROSS APPLY sys.dm_exec_query_plan(s.plan_handle) p
            WHERE t.text LIKE '{mustContain}' AND t.text LIKE '%a.PublishedAt DESC%' AND t.text NOT LIKE '%dm_exec_query_stats%' AND EXISTS (SELECT 1 FROM sys.dm_exec_plan_attributes(s.plan_handle) pa WHERE pa.attribute = 'dbid' AND pa.value = DB_ID()) AND t.text NOT LIKE '{mustNotContain}'
            ORDER BY s.last_execution_time DESC";
        await using SqlConnection sqlConnection = new(connection);
        await sqlConnection.OpenAsync();
        await using SqlCommand command = new(sql, sqlConnection);
        return (await command.ExecuteScalarAsync()) as string ?? string.Empty;
    }
}
