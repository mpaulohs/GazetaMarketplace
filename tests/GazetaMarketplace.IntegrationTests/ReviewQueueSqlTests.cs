using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// A fila de revisão (4.1) contra o SQL Server real: a consulta EF (junção com o autor, filtro por situação e ordem por data de envio e id) é traduzida para
/// T-SQL e devolve o mesmo que o SQLite dos testes de unidade, e a página da fila abre para o Administrador.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ReviewQueueSqlTests
#pragma warning restore CA1515
{
    private const string AdminEmail = "admin.fila@exemplo.com.br";
    private const string WriterEmail = "redatora.fila@exemplo.com.br";
    private const string Password = "Senha@Forte1";

    private static DateTime Day(int day) => new(2026, 9, day, 13, 0, 0, DateTimeKind.Utc);

    private static async Task<int> AddAdAsync(string connection, int author, string title, int category, byte status, DateTime sentAt)
    {
        Ad ad = Ad.CreateDraft(title, author);
        ad.SetCategory(category);
        ad.SetText(title, "Descrição");
        if (status != AdStatus.Draft)
        {
            ad.ApplyTransition(AdStatus.InReview, author, sentAt, null);
        }

        if (status == AdStatus.Published)
        {
            ad.ApplyTransition(AdStatus.Published, author, sentAt, null);
        }

        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        context.Ads.Add(ad);
        await context.SaveChangesAsync();
        return ad.Id;
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Fila_NoSqlServer_FiltraPorSituacao_OrdenaPorEnvioEDesempataPeloId_ComAutorECategoria()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        await factory.CreateUserAsync(AdminEmail, "Marcos Silva", Password, "Administrador");
        await factory.CreateUserAsync(WriterEmail, "Ana Souza", Password, "Redator");
        int author;
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            author = (await context.Users.SingleAsync(u => u.Email == WriterEmail)).Id;
        }

        int newest = await AddAdAsync(connection, author, "Mais novo", 33, AdStatus.InReview, Day(30));
        int tieSecond = await AddAdAsync(connection, author, "Empate B", 27, AdStatus.InReview, Day(28));
        int oldest = await AddAdAsync(connection, author, "Mais antigo", 86, AdStatus.InReview, Day(25));
        int tieFirst = await AddAdAsync(connection, author, "Empate A", 27, AdStatus.InReview, Day(28));
        await AddAdAsync(connection, author, "Rascunho", 33, AdStatus.Draft, Day(1));
        await AddAdAsync(connection, author, "Publicado", 33, AdStatus.Published, Day(2));

        using IServiceScope scope = factory.Services.CreateScope();
        System.Collections.Generic.IReadOnlyList<ReviewQueueItem> items = await scope.ServiceProvider.GetRequiredService<IReviewQueue>().ListAsync(CancellationToken.None);

        CollectionAssert.AreEqual(new[] { oldest, Math.Min(tieSecond, tieFirst), Math.Max(tieSecond, tieFirst), newest }, items.Select(i => i.Id).ToArray(), "data de envio e, no empate, o menor id");
        Assert.IsTrue(items.All(i => i.AuthorName == "Ana Souza"));
        Assert.AreEqual("Livros e revistas", items[0].CategoryName);
        Assert.AreEqual("Casas", items[1].CategoryName);
        Assert.AreEqual(Day(25), items[0].SentAt);

        using HttpClient browser = factory.CreateBrowser();
        Assert.AreEqual(HttpStatusCode.Redirect, (await browser.SignInAsync(AdminEmail, Password)).StatusCode);
        HttpResponseMessage page = await browser.GetAsync("/painel/anuncios/fila");
        string html = WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync());
        Assert.AreEqual(HttpStatusCode.OK, page.StatusCode);
        StringAssert.Contains(html, "4 anúncios aguardando revisão");
    }
}
