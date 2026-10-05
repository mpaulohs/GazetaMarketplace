using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Search;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Ads;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// A lista de anúncios do painel (US-012) no T-SQL de verdade (ADR-004): o repositório Dapper com o filtro de autoria, a situação, os arquivados escondidos, a busca por título
/// (<c>CHARINDEX</c> sobre <c>TitleSearch</c>, sem curinga), a paginação, a ordem estável e o tempo limite; e a página inteira (serviço, repositório e tela) para o Redator e o Administrador.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PanelAdListQueryTests
#pragma warning restore CA1515
{
    private const string Password = "Senha@Forte1";
    private static int _users;

    private static async Task<int> AddUserAsync(string connection, string name)
    {
        int n = Interlocked.Increment(ref _users);
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        AppUser user = new() { UserName = $"lista{n}@exemplo.com.br", NormalizedUserName = $"LISTA{n}@EXEMPLO.COM.BR", Email = $"lista{n}@exemplo.com.br", FullName = name };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>Grava um anúncio na situação pedida pelas passagens do Apêndice A e, se pedido, fixa a data de "alterado em".</summary>
    private static async Task<int> AddAdAsync(string connection, int authorId, string title, byte status = AdStatus.Draft, string reason = null, DateTime? changedAt = null, int? category = 86)
    {
        Ad ad = Ad.CreateDraft(title, authorId);
        ad.SetCategory(category);
        DateTime now = DateTime.UtcNow;
        switch (status)
        {
            case AdStatus.InReview:
                ad.ApplyTransition(AdStatus.InReview, authorId, now, null);
                break;
            case AdStatus.Published:
                ad.ApplyTransition(AdStatus.InReview, authorId, now, null);
                ad.ApplyTransition(AdStatus.Published, authorId, now, null);
                break;
            case AdStatus.Rejected:
                ad.ApplyTransition(AdStatus.InReview, authorId, now, null);
                ad.ApplyTransition(AdStatus.Rejected, authorId, now, reason ?? "Fotos escuras");
                break;
            case AdStatus.Archived:
                ad.ApplyTransition(AdStatus.Archived, authorId, now, null);
                break;
        }

        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            context.Ads.Add(ad);
            await context.SaveChangesAsync();
        }

        if (changedAt is { } when)
        {
            Assert.AreEqual(0, await AdData.TryExecuteAsync(connection, "UPDATE Ads SET UpdatedAt = @d, CreatedAt = @d WHERE Id = @id", ("@d", when), ("@id", ad.Id)));
        }

        return ad.Id;
    }

    private static PanelAdListReadRepository Repository(string connection) => new(new SqlConnection(connection));

    private static PanelAdListQuery Query(int? author = null, byte? status = null, string search = "", int page = 1, int size = 20) =>
        new(author, status, ExcludeArchived: status != AdStatus.Archived, Normalizer.Normalize(search), page, size);

    private static async Task<List<string>> TitlesAsync(string connection, PanelAdListQuery query) =>
        [.. (await Repository(connection).ListAsync(query, CancellationToken.None)).Rows.Select(r => r.Title)];

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US012S01_S02_Autoria_RedatorSoRecebeAsProprias_AdministradorTodasComONomeDoAutor()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int ana = await AddUserAsync(connection, "Ana Souza");
        int bruno = await AddUserAsync(connection, "Bruno Lima");
        foreach ((string title, byte status) in new[] { ("A1", AdStatus.Draft), ("A2", AdStatus.Draft), ("A3", AdStatus.InReview), ("A4", AdStatus.Rejected), ("A5", AdStatus.Published) })
        {
            await AddAdAsync(connection, ana, title, status);
        }

        foreach (string title in new[] { "B1", "B2", "B3" })
        {
            await AddAdAsync(connection, bruno, title);
        }

        PanelAdListRows mine = await Repository(connection).ListAsync(Query(author: ana), CancellationToken.None);
        PanelAdListRows all = await Repository(connection).ListAsync(Query(), CancellationToken.None);

        Assert.AreEqual(5, mine.Total);
        CollectionAssert.AreEquivalent(new[] { "A1", "A2", "A3", "A4", "A5" }, mine.Rows.Select(r => r.Title).ToArray());
        Assert.IsTrue(mine.Rows.All(r => r.AuthorId == ana && r.AuthorName == "Ana Souza"));
        Assert.AreEqual(8, all.Total);
        CollectionAssert.AreEquivalent(new[] { "Ana Souza", "Bruno Lima" }, all.Rows.Select(r => r.AuthorName).Distinct().ToArray());
        Assert.AreEqual("Bruno Lima", all.Rows.Single(r => r.Title == "B2").AuthorName);
        PanelAdListRow rejected = mine.Rows.Single(r => r.Title == "A4");
        Assert.AreEqual(AdStatus.Rejected, rejected.Status);
        Assert.AreEqual("Fotos escuras", rejected.RejectionReason);
        Assert.AreEqual(86, rejected.CategoryId);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Paginacao_PaginaNoLimiteDoPainel_NoSqlServer_VoltaVaziaSemErro()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int ana = await AddUserAsync(connection, "Ana Souza");
        await AddAdAsync(connection, ana, "Único", AdStatus.Draft);

        PanelAdListRows beyond = await Repository(connection).ListAsync(Query(page: PanelAdListFilters.MaxPage), CancellationToken.None);

        Assert.AreEqual(1, beyond.Total, "o total continua certo");
        Assert.IsEmpty(beyond.Rows, "a página além do fim vem vazia; o serviço então mostra a última");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US012S03_FiltroPorSituacao_EArquivadosEscondidosAteFiltrarPorArquivado()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int ana = await AddUserAsync(connection, "Ana Souza");
        foreach ((string title, byte status) in new[] { ("Rascunho", AdStatus.Draft), ("Revisão", AdStatus.InReview), ("Publicado", AdStatus.Published), ("Rejeitado 1", AdStatus.Rejected), ("Rejeitado 2", AdStatus.Rejected), ("Arquivado", AdStatus.Archived) })
        {
            await AddAdAsync(connection, ana, title, status);
        }

        PanelAdListRows rejected = await Repository(connection).ListAsync(Query(status: AdStatus.Rejected), CancellationToken.None);
        PanelAdListRows archived = await Repository(connection).ListAsync(Query(status: AdStatus.Archived), CancellationToken.None);
        PanelAdListRows standard = await Repository(connection).ListAsync(Query(), CancellationToken.None);

        Assert.AreEqual(2, rejected.Total);
        Assert.IsTrue(rejected.Rows.All(r => r.Status == AdStatus.Rejected));
        Assert.AreEqual(1, archived.Total);
        Assert.AreEqual("Arquivado", archived.Rows.Single().Title);
        Assert.AreEqual(5, standard.Total, "a lista padrão esconde os arquivados");
        Assert.IsFalse(standard.Rows.Any(r => r.Status == AdStatus.Archived));
        Assert.AreEqual(1, await Repository(connection).CountByStatusAsync(AdStatus.InReview, CancellationToken.None));
        Assert.AreEqual(1, await Repository(connection).CountByStatusAsync(AdStatus.Archived, CancellationToken.None), "a contagem da fila é de qualquer autor");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US012S04_Busca_SemAcentoESemDiferencaDeMaiusculas_ECuringasViramTexto()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int ana = await AddUserAsync(connection, "Ana Souza");
        foreach (string title in new[] { "Honda Civic 2018", "Casa com quintal", "Pré-venda Cação", "Desconto 100% hoje", "Peça_ab", "Kit [novo]", "Pao de ló" })
        {
            await AddAdAsync(connection, ana, title);
        }

        CollectionAssert.AreEqual(new[] { "Honda Civic 2018" }, (await TitlesAsync(connection, Query(search: "civic"))).ToArray(), "S04: 'civic' mostra só o Honda Civic");
        CollectionAssert.AreEqual(new[] { "Honda Civic 2018" }, (await TitlesAsync(connection, Query(search: "  HONDA   CÍVIC "))).ToArray(), "maiúsculas, acento e espaços repetidos");
        CollectionAssert.AreEqual(new[] { "Pré-venda Cação" }, (await TitlesAsync(connection, Query(search: "cacao"))).ToArray(), "o título tem acento, o termo não");
        CollectionAssert.AreEqual(new[] { "Pré-venda Cação" }, (await TitlesAsync(connection, Query(search: "Pré-venda CAÇÃO"))).ToArray(), "o termo tem acento");
        CollectionAssert.AreEqual(new[] { "Desconto 100% hoje" }, (await TitlesAsync(connection, Query(search: "%"))).ToArray(), "% é texto, não curinga");
        CollectionAssert.AreEqual(new[] { "Peça_ab" }, (await TitlesAsync(connection, Query(search: "_"))).ToArray(), "_ é texto, não curinga");
        CollectionAssert.AreEqual(new[] { "Kit [novo]" }, (await TitlesAsync(connection, Query(search: "["))).ToArray(), "[ é texto, não classe de caracteres");
        CollectionAssert.AreEqual(new[] { "Pao de ló" }, (await TitlesAsync(connection, Query(search: "ao de l"))).ToArray());
        Assert.IsEmpty(await TitlesAsync(connection, Query(search: "zzz")));
        Assert.AreEqual(7, (await Repository(connection).ListAsync(Query(search: ""), CancellationToken.None)).Total, "sem termo não filtra");
        Assert.IsEmpty(await TitlesAsync(connection, Query(search: "x'; DROP TABLE Ads; --")), "texto de ataque é só texto");
        Assert.AreEqual(7, (await Repository(connection).ListAsync(Query(), CancellationToken.None)).Total, "a tabela continua inteira");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task US012S07_Paginacao_45Anuncios_20_20_5_SemRepetirNemPerder_ComOTotalEmTodasAsPaginas()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int ana = await AddUserAsync(connection, "Ana Souza");
        for (int i = 1; i <= 45; i++)
        {
            await AddAdAsync(connection, ana, $"Anúncio {i:00}", changedAt: new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc).AddMinutes(i));
        }

        PanelAdListRows first = await Repository(connection).ListAsync(Query(author: ana, page: 1), CancellationToken.None);
        PanelAdListRows second = await Repository(connection).ListAsync(Query(author: ana, page: 2), CancellationToken.None);
        PanelAdListRows third = await Repository(connection).ListAsync(Query(author: ana, page: 3), CancellationToken.None);
        PanelAdListRows beyond = await Repository(connection).ListAsync(Query(author: ana, page: 4), CancellationToken.None);

        CollectionAssert.AreEqual(new[] { 20, 20, 5, 0 }, new[] { first.Rows.Count, second.Rows.Count, third.Rows.Count, beyond.Rows.Count });
        Assert.IsTrue(new[] { first, second, third, beyond }.All(p => p.Total == 45), "o total vale para todas as páginas");
        List<string> everything = first.Rows.Concat(second.Rows).Concat(third.Rows).Select(r => r.Title).ToList();
        Assert.AreEqual(45, everything.Distinct().Count(), "nenhum anúncio repetido nem perdido");
        Assert.AreEqual("Anúncio 45", everything[0], "do alterado mais recentemente ao mais antigo");
        Assert.AreEqual("Anúncio 01", everything[^1]);
        Assert.AreEqual("Anúncio 05", third.Rows[0].Title);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Ordem_ComAMesmaDataDeAlteracao_DesempataPeloIdDoMaisNovo()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int ana = await AddUserAsync(connection, "Ana Souza");
        DateTime same = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
        List<int> ids = [];
        foreach (string title in new[] { "Primeiro", "Segundo", "Terceiro", "Quarto" })
        {
            ids.Add(await AddAdAsync(connection, ana, title, changedAt: same));
        }

        PanelAdListRows rows = await Repository(connection).ListAsync(Query(), CancellationToken.None);

        CollectionAssert.AreEqual(ids.AsEnumerable().Reverse().ToArray(), rows.Rows.Select(r => r.Id).ToArray());
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ConsultaDoRedator_UsaOIndiceDeAutorSituacaoEData()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        int ana = await AddUserAsync(connection, "Ana Souza");
        // Volume suficiente para o otimizador escolher: 4.000 anúncios, 100 deles da Ana
        int other = await AddUserAsync(connection, "Outro Autor");
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            context.Ads.AddRange(Enumerable.Range(1, 4000).Select(n => Ad.CreateDraft($"Volume {n}", n % 40 == 0 ? ana : other)));
            await context.SaveChangesAsync();
        }

        await Repository(connection).ListAsync(Query(author: ana), CancellationToken.None);

        // Os planos em cache são do servidor inteiro: sem o filtro por banco, a execução mais recente podia ser a de outro teste (tabela pequena, outro plano) e o teste falhava de vez em quando
        string plan = await ScalarAsync(connection, @"
            SELECT TOP (1) CAST(p.query_plan AS NVARCHAR(MAX))
            FROM sys.dm_exec_query_stats s
            CROSS APPLY sys.dm_exec_sql_text(s.sql_handle) t
            CROSS APPLY sys.dm_exec_query_plan(s.plan_handle) p
            WHERE t.text LIKE '%INNER JOIN AspNetUsers%' AND t.text LIKE '%a.AuthorId = @AuthorId%'
              AND EXISTS (SELECT 1 FROM sys.dm_exec_plan_attributes(s.plan_handle) pa WHERE pa.attribute = 'dbid' AND pa.value = DB_ID())
            ORDER BY s.last_execution_time DESC");

        StringAssert.Contains(plan, "IX_Ads_AuthorId_Status_UpdatedAt", "o filtro de autoria usa o índice de autor");
        Assert.IsFalse(plan.Contains("Table Scan", StringComparison.Ordinal), "sem varredura inteira da tabela");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task PaginaInteira_Redator_VeSoOsProprios_Administrador_VeTodosComAutor_ArquivadosEscondidosAteFiltrar()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        AppUser ana = await factory.CreateUserAsync("ana.lista@exemplo.com.br", "Ana Souza", Password, RoleNames.Writer);
        AppUser bruno = await factory.CreateUserAsync("bruno.lista@exemplo.com.br", "Bruno Lima", Password, RoleNames.Writer);
        await factory.CreateUserAsync("admin.lista@exemplo.com.br", "Marcos Silva", Password, RoleNames.Administrator);
        await AddAdAsync(connection, ana.Id, "Honda Civic 2018", AdStatus.Published);
        await AddAdAsync(connection, ana.Id, "Moto para retirar peças", AdStatus.Rejected, "Fotos escuras; envie fotos com boa iluminação");
        await AddAdAsync(connection, ana.Id, "Anúncio arquivado da Ana", AdStatus.Archived);
        await AddAdAsync(connection, bruno.Id, "Casa com quintal", AdStatus.InReview);
        using HttpClient writer = factory.CreateBrowser();
        using HttpClient admin = factory.CreateBrowser();
        Assert.AreEqual(HttpStatusCode.Redirect, (await writer.SignInAsync("ana.lista@exemplo.com.br", Password)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Redirect, (await admin.SignInAsync("admin.lista@exemplo.com.br", Password)).StatusCode);

        string mine = WebUtility.HtmlDecode(await writer.GetStringAsync("/painel/anuncios"));
        string everything = WebUtility.HtmlDecode(await admin.GetStringAsync("/painel/anuncios"));
        string archived = WebUtility.HtmlDecode(await writer.GetStringAsync("/painel/anuncios?situacao=arquivado"));
        string search = WebUtility.HtmlDecode(await admin.GetStringAsync("/painel/anuncios?q=CASA"));

        StringAssert.Contains(mine, "Honda Civic 2018");
        StringAssert.Contains(mine, "Moto para retirar peças");
        StringAssert.Contains(mine, "Motivo da rejeição: Fotos escuras; envie fotos com boa iluminação");
        Assert.IsFalse(mine.Contains("Casa com quintal", StringComparison.Ordinal), "o Redator não vê o anúncio do outro");
        Assert.IsFalse(mine.Contains("Anúncio arquivado da Ana", StringComparison.Ordinal), "arquivado escondido na lista padrão");
        Assert.AreEqual(2, Regex.Matches(mine, @"<tr data-ad-id=").Count);
        StringAssert.Contains(everything, "Casa com quintal");
        StringAssert.Matches(everything, new Regex(@">Casa com quintal</a>\s*</th>\s*<td[^>]*>Bruno Lima</td>\s*<td[^>]*>Livros e revistas</td>\s*<td[^>]*>Em revisão</td>"));
        Assert.AreEqual(3, Regex.Matches(everything, @"<tr data-ad-id=").Count);
        StringAssert.Matches(everything, new Regex(@"Fila de revisão \(1\)"));
        StringAssert.Contains(archived, "Anúncio arquivado da Ana");
        Assert.AreEqual(1, Regex.Matches(archived, @"<tr data-ad-id=").Count);
        Assert.AreEqual(1, Regex.Matches(search, @"<tr data-ad-id=").Count);
        StringAssert.Contains(search, "Casa com quintal");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task PaginaInteira_45Anuncios_TresPaginas_20_20_5()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        AppUser ana = await factory.CreateUserAsync("ana.paginas@exemplo.com.br", "Ana Souza", Password, RoleNames.Writer);
        for (int i = 1; i <= 45; i++)
        {
            await AddAdAsync(connection, ana.Id, $"Anúncio {i:00}", changedAt: new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc).AddMinutes(i));
        }

        using HttpClient writer = factory.CreateBrowser();
        Assert.AreEqual(HttpStatusCode.Redirect, (await writer.SignInAsync("ana.paginas@exemplo.com.br", Password)).StatusCode);

        string first = WebUtility.HtmlDecode(await writer.GetStringAsync("/painel/anuncios"));
        string third = WebUtility.HtmlDecode(await writer.GetStringAsync("/painel/anuncios?pagina=3"));
        string beyond = WebUtility.HtmlDecode(await writer.GetStringAsync("/painel/anuncios?pagina=99"));

        Assert.AreEqual(20, Regex.Matches(first, @"<tr data-ad-id=").Count);
        StringAssert.Contains(first, "45 anúncios");
        StringAssert.Contains(first, "Anúncio 45");
        Assert.AreEqual(5, Regex.Matches(third, @"<tr data-ad-id=").Count);
        StringAssert.Contains(third, "Anúncio 05");
        StringAssert.Contains(third, "Anúncio 01");
        Assert.AreEqual(5, Regex.Matches(beyond, @"<tr data-ad-id=").Count, "página além do fim mostra a última");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task ConsultaQueEstouraOTempo_Devolve503SemPilha_ENaoDeixaOSiteForaDoAr()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        await factory.CreateUserAsync("admin.tempo@exemplo.com.br", "Marcos Silva", Password, RoleNames.Administrator);
        using HttpClient admin = factory.CreateBrowser();
        Assert.AreEqual(HttpStatusCode.Redirect, (await admin.SignInAsync("admin.tempo@exemplo.com.br", Password)).StatusCode);

        // Outra conexão segura a tabela inteira trancada; a leitura espera, passa dos 10 s do tempo limite e o site responde com o erro da tela
        await using SqlConnection blocker = new(connection);
        await blocker.OpenAsync();
        await using SqlTransaction transaction = blocker.BeginTransaction();
        await using (SqlCommand hold = new("SELECT COUNT(*) FROM Ads WITH (TABLOCKX, HOLDLOCK)", blocker, transaction))
        {
            await hold.ExecuteScalarAsync();
        }

        Stopwatch clock = Stopwatch.StartNew();
        HttpResponseMessage response = await admin.GetAsync("/painel/anuncios");
        clock.Stop();
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        await transaction.RollbackAsync();

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.IsGreaterThanOrEqualTo(9, (int)clock.Elapsed.TotalSeconds, $"esperou o tempo limite de {PanelAdListReadRepository.CommandTimeoutSeconds} s (levou {clock.Elapsed.TotalSeconds:0.0} s)");
        Assert.IsLessThan(20, (int)clock.Elapsed.TotalSeconds, "o tempo limite não passa de 10 s (com folga)");
        StringAssert.Contains(html, "Não foi possível carregar os anúncios.");
        StringAssert.Contains(html, "Tentar novamente");
        foreach (string leak in new[] { "SqlException", "Timeout expired", "Microsoft.Data.SqlClient", "   at " })
        {
            Assert.IsFalse(html.Contains(leak, StringComparison.Ordinal), "sem detalhe técnico: " + leak);
        }

        Assert.AreEqual(HttpStatusCode.OK, (await admin.GetAsync("/painel/anuncios")).StatusCode, "solto o bloqueio, a lista volta");
    }

    private static async Task<string> ScalarAsync(string connection, string sql)
    {
        await using SqlConnection sqlConnection = new(connection);
        await sqlConnection.OpenAsync();
        await using SqlCommand command = new(sql, sqlConnection);
        return (await command.ExecuteScalarAsync()) as string;
    }
}
