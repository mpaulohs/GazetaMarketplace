using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Settings;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// Checkpoint 4 no SQL Server real, com os dois papéis e dois Administradores de verdade (contas e sessões diferentes): o ciclo de vida inteiro do anúncio (enviar, rejeitar, reenviar,
/// publicar, despublicar, reenviar, publicar, arquivar) deixa em <c>AuditEntries</c> exatamente uma linha por ação, com o ator e as situações; pedidos negados não gravam nada; e
/// quando dois Administradores decidem o mesmo anúncio ao mesmo tempo só um vence, uma só auditoria é gravada, ninguém recebe erro 500 e quem perde lê "por outro administrador".
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class Checkpoint4LifecycleTests
#pragma warning restore CA1515
{
    private const string Password = "Senha@Forte1";
    private const string WriterEmail = "ana.ciclo@exemplo.com.br";
    private const string AdminAEmail = "carla.ciclo@exemplo.com.br";
    private const string AdminBEmail = "marcos.ciclo@exemplo.com.br";
    private const string Reason = "Fotos escuras; envie fotos com boa iluminação";

    private sealed record Site(string Connection, IntegrationWebFactory Factory, HttpClient Writer, HttpClient AdminA, HttpClient AdminB, int WriterId, int AdminAId, int AdminBId) : IDisposable
    {
        public void Dispose()
        {
            Writer.Dispose();
            AdminA.Dispose();
            AdminB.Dispose();
            Factory.Dispose();
        }
    }

    private static async Task<Site> StartAsync()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        IntegrationWebFactory factory = new(connection);
        AppUser writer = await factory.CreateUserAsync(WriterEmail, "Ana Souza", Password, RoleNames.Writer);
        AppUser adminA = await factory.CreateUserAsync(AdminAEmail, "Carla Admin", Password, RoleNames.Administrator);
        AppUser adminB = await factory.CreateUserAsync(AdminBEmail, "Marcos Admin", Password, RoleNames.Administrator);
        await using (AppDbContext context = SqlServerFixture.NewContext(connection))
        {
            context.SiteSettings.Add(new SiteSetting { Key = SiteSettingKeys.Phone, Value = "11912345678" });
            await context.SaveChangesAsync();
        }

        HttpClient writerBrowser = factory.CreateBrowser();
        HttpClient adminABrowser = factory.CreateBrowser();
        HttpClient adminBBrowser = factory.CreateBrowser();
        foreach ((HttpClient client, string email) in new[] { (writerBrowser, WriterEmail), (adminABrowser, AdminAEmail), (adminBBrowser, AdminBEmail) })
        {
            Assert.AreEqual(HttpStatusCode.Redirect, (await client.SignInAsync(email, Password)).StatusCode, email);
        }

        return new Site(connection, factory, writerBrowser, adminABrowser, adminBBrowser, writer.Id, adminA.Id, adminB.Id);
    }

    /// <summary>Um rascunho completo (passa em todas as pendências) com uma linha de foto, do autor dado.</summary>
    private static async Task<int> AddCompleteDraftAsync(string connection, int authorId, string title)
    {
        Ad ad = Ad.CreateDraft(title, authorId);
        ad.SetText(title, "Descrição completa");
        ad.SetCategory(86);
        ad.SetPrice(5_000);
        ad.SetLocation("13015100", "Campinas", "SP", false);
        ad.SetAttributes(new AdAttributes().Set("conditionId", 2));
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        context.Ads.Add(ad);
        await context.SaveChangesAsync();
        context.AdPhotos.Add(new AdPhoto { AdId = ad.Id, SortOrder = 0, StorageKey = $"{ad.Id}/{Guid.NewGuid():N}", Width = 10, Height = 10, SizeBytes = 3 });
        await context.SaveChangesAsync();
        return ad.Id;
    }

    private static async Task<int> AddInReviewAsync(string connection, int authorId, string title)
    {
        int id = await AddCompleteDraftAsync(connection, authorId, title);
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        Ad ad = await context.Ads.SingleAsync(a => a.Id == id);
        ad.ApplyTransition(AdStatus.InReview, authorId, DateTime.UtcNow, null);
        await context.SaveChangesAsync();
        return id;
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string path, params (string Name, string Value)[] fields) =>
        client.PostFormAsync("/painel/anuncios/novo", path, fields.ToDictionary(f => f.Name, f => f.Value));

    private static async Task<List<AuditEntry>> AuditsAsync(string connection, int adId)
    {
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        string target = adId.ToString(CultureInfo.InvariantCulture);
        return await context.AuditEntries.AsNoTracking().Where(e => e.TargetType == "Ad" && e.TargetId == target).OrderBy(e => e.Id).ToListAsync();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CicloDeVidaCompleto_DoisPapeis_UmaAuditoriaPorAcao_ComAtorESituacoes_ENegadosNaoGravam()
    {
        using Site site = await StartAsync();
        int adId = await AddCompleteDraftAsync(site.Connection, site.WriterId, "Livro do ciclo");
        string id = adId.ToString(CultureInfo.InvariantCulture);

        // Rascunho -> Em revisão (Redator)
        Assert.AreEqual("/painel/anuncios", (await PostAsync(site.Writer, $"/painel/anuncios/{adId}/enviar/confirmar")).Destination());
        // Em revisão -> Rejeitado (Administrador A, com motivo)
        Assert.AreEqual("/painel/anuncios/fila", (await PostAsync(site.AdminA, $"/painel/anuncios/{adId}/rejeitar", ("reason", "  " + Reason + "  "))).Destination());
        string afterReject = WebUtility.HtmlDecode(await site.Writer.GetStringAsync("/painel/anuncios"));
        StringAssert.Contains(afterReject, "Motivo da rejeição: " + Reason);
        StringAssert.Matches(afterReject, new Regex(@">Livro do ciclo</a>[\s\S]*?<td[^>]*>Rejeitado</td>"));
        // Rejeitado -> Em revisão (Redator reenvia) limpa o motivo do anúncio
        Assert.AreEqual("/painel/anuncios", (await PostAsync(site.Writer, $"/painel/anuncios/{adId}/enviar/confirmar")).Destination());
        Ad resubmitted = await AdData.LoadAsync(site.Connection, adId);
        Assert.AreEqual(AdStatus.InReview, resubmitted.Status);
        Assert.IsNull(resubmitted.RejectionReason, "reenviar limpa o motivo no anúncio; o histórico fica na auditoria");
        Assert.IsNull(resubmitted.RejectedAt);
        // Em revisão -> Publicado (Administrador A)
        Assert.AreEqual("/painel/anuncios/fila", (await PostAsync(site.AdminA, $"/painel/anuncios/{adId}/publicar")).Destination());
        StringAssert.Contains(WebUtility.HtmlDecode(await site.Writer.GetStringAsync("/painel/anuncios")), "Publicado");
        StringAssert.Matches(WebUtility.HtmlDecode(await site.AdminB.GetStringAsync("/painel/anuncios")), new Regex(@">Livro do ciclo</a>\s*</th>\s*<td[^>]*>Ana Souza</td>"));
        // Publicado -> Rascunho (Administrador B despublica; o Redator volta a editar e reenvia)
        Assert.AreEqual($"/painel/anuncios/{adId}/editar", (await PostAsync(site.AdminB, $"/painel/anuncios/{adId}/despublicar")).Destination());
        Assert.AreEqual("/painel/anuncios", (await PostAsync(site.Writer, $"/painel/anuncios/{adId}/enviar/confirmar")).Destination());
        // Em revisão -> Publicado (Administrador B) -> Arquivado (Administrador A)
        Assert.AreEqual("/painel/anuncios/fila", (await PostAsync(site.AdminB, $"/painel/anuncios/{adId}/publicar")).Destination());
        Assert.AreEqual("/painel/anuncios", (await PostAsync(site.AdminA, $"/painel/anuncios/{adId}/arquivar")).Destination());

        List<AuditEntry> audits = await AuditsAsync(site.Connection, adId);
        (string Action, int Actor, string Previous, string Next)[] expected =
        [
            ("ad.submit", site.WriterId, "Rascunho", "Em revisão"),
            ("ad.reject", site.AdminAId, "Em revisão", "Rejeitado — motivo: " + Reason),
            ("ad.submit", site.WriterId, "Rejeitado", "Em revisão"),
            ("ad.publish", site.AdminAId, "Em revisão", "Publicado"),
            ("ad.unpublish", site.AdminBId, "Publicado", "Rascunho"),
            ("ad.submit", site.WriterId, "Rascunho", "Em revisão"),
            ("ad.publish", site.AdminBId, "Em revisão", "Publicado"),
            ("ad.archive", site.AdminAId, "Publicado", "Arquivado")
        ];
        CollectionAssert.AreEqual(
            expected.Select(e => $"{e.Action}|{e.Actor}|{e.Previous}|{e.Next}").ToArray(),
            audits.Where(a => a.Action.StartsWith("ad.", StringComparison.Ordinal) && a.Action != "ad.create" && a.Action != "ad.update").Select(a => $"{a.Action}|{a.ActorId}|{a.PreviousValue}|{a.NewValue}").ToArray(),
            "uma linha por ação, na ordem, com o ator e as situações");
        Assert.IsTrue(audits.All(a => a.Result == AuditResult.Success));
        Ad archived = await AdData.LoadAsync(site.Connection, adId);
        Assert.AreEqual(AdStatus.Archived, archived.Status);
        Assert.IsNotNull(archived.ArchivedAt);

        // Pedidos negados não gravam nada: o Redator tentando decidir, retirar ou publicar, e pedidos sem o token antiforgery
        int before = (await AuditsAsync(site.Connection, adId)).Count;
        foreach (string action in new[] { "publicar", "rejeitar", "despublicar", "arquivar" })
        {
            HttpResponseMessage denied = await PostAsync(site.Writer, $"/painel/anuncios/{id}/{action}", ("reason", "x"));
            Assert.AreEqual(HttpStatusCode.Redirect, denied.StatusCode, action);
            StringAssert.StartsWith(denied.Destination(), "/painel/acesso-negado", action);
            using FormUrlEncodedContent noToken = new(new Dictionary<string, string> { ["reason"] = "x" });
            Assert.AreEqual(HttpStatusCode.BadRequest, (await site.AdminA.PostAsync($"/painel/anuncios/{id}/{action}", noToken)).StatusCode, action + " sem token");
        }

        Assert.AreEqual(before, (await AuditsAsync(site.Connection, adId)).Count, "negados não gravam auditoria");
        Assert.AreEqual(AdStatus.Archived, (await AdData.LoadAsync(site.Connection, adId)).Status);
    }

    private static async Task RaceAsync(bool publish)
    {
        using Site site = await StartAsync();
        string action = publish ? "ad.publish" : "ad.reject";
        byte expected = publish ? AdStatus.Published : AdStatus.Rejected;
        string verb = publish ? "publicar" : "rejeitar";
        string[] tokens =
        [
            Regex.Match(await site.AdminA.GetStringAsync("/painel/anuncios/novo"), @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value,
            Regex.Match(await site.AdminB.GetStringAsync("/painel/anuncios/novo"), @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value
        ];
        HttpClient[] admins = [site.AdminA, site.AdminB];
        int[] adminIds = [site.AdminAId, site.AdminBId];

        for (int round = 1; round <= 3; round++)
        {
            int adId = await AddInReviewAsync(site.Connection, site.WriterId, $"Disputa dos administradores {round}");
            // 4 pedidos de cada Administrador, de duas sessões, todos ao mesmo tempo
            HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(async () =>
            {
                int who = i % 2;
                Dictionary<string, string> fields = new() { ["__RequestVerificationToken"] = tokens[who] };
                if (!publish)
                {
                    fields["reason"] = Reason;
                }

                using FormUrlEncodedContent body = new(fields);
                return await admins[who].PostAsync($"/painel/anuncios/{adId}/{verb}", body);
            })));

            string codes = $"rodada {round}: " + string.Join(",", responses.Select(r => (int)r.StatusCode));
            Assert.IsTrue(responses.All(r => r.StatusCode == HttpStatusCode.Redirect), codes);
            Ad ad = await AdData.LoadAsync(site.Connection, adId);
            Assert.AreEqual(expected, ad.Status, codes);
            List<AuditEntry> audits = [.. (await AuditsAsync(site.Connection, adId)).Where(a => a.Action == action)];
            Assert.HasCount(1, audits, $"rodada {round}: uma única auditoria {action}");
            int winner = publish ? ad.PublishedById!.Value : ad.RejectedById!.Value;
            Assert.AreEqual(winner, audits[0].ActorId, "a auditoria é de quem venceu");
            Assert.IsTrue(adminIds.Contains(winner));

            // Quem perdeu a disputa (a outra sessão) lê a mensagem da SPEC na pré-visualização
            HttpClient loser = admins[adminIds[0] == winner ? 1 : 0];
            string preview = WebUtility.HtmlDecode(await loser.GetStringAsync($"/painel/anuncios/{adId}/pre-visualizacao"));
            StringAssert.Contains(preview, publish ? "Este anúncio já foi publicado por outro administrador" : "Este anúncio já foi rejeitado por outro administrador", $"rodada {round}");
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public Task DoisAdministradores_PublicamOMesmoAnuncioAoMesmoTempo_UmVence_UmaAuditoria_QuemPerdeLePorOutroAdministrador() => RaceAsync(publish: true);

    [TestMethod]
    [TestCategory("Integration")]
    public Task DoisAdministradores_RejeitamOMesmoAnuncioAoMesmoTempo_UmVence_UmaAuditoria_QuemPerdeLePorOutroAdministrador() => RaceAsync(publish: false);
}
