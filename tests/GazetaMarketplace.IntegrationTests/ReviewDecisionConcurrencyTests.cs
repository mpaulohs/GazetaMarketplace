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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// A decisão da revisão (US-010-S07) contra o SQL Server real: com <c>RowVersion</c> de verdade, vários pedidos ao mesmo tempo disputam a passagem
/// Em revisão → Publicado (ou Rejeitado). Só um vence, só uma auditoria é gravada e nenhum pedido termina em erro 500; quem perde volta para a pré-visualização.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ReviewDecisionConcurrencyTests
#pragma warning restore CA1515
{
    private const string AdminEmail = "admin.revisao@exemplo.com.br";
    private const string Password = "Senha@Forte1";
    private const string Reason = "Fotos escuras; envie fotos com boa iluminação";

    private static async Task<int> AddInReviewAsync(string connection, int authorId, string title)
    {
        Ad ad = Ad.CreateDraft(title, authorId);
        ad.SetText(title, "Descrição completa");
        ad.SetCategory(86);
        ad.SetPrice(5_000);
        ad.SetLocation("13015100", "Campinas", "SP", false);
        ad.SetAttributes(new AdAttributes().Set("conditionId", 2));
        ad.ApplyTransition(AdStatus.InReview, authorId, DateTime.UtcNow, null);
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        context.Ads.Add(ad);
        await context.SaveChangesAsync();
        context.AdPhotos.Add(new AdPhoto { AdId = ad.Id, SortOrder = 0, StorageKey = $"{ad.Id}/{Guid.NewGuid():N}", Width = 10, Height = 10, SizeBytes = 3 });
        await context.SaveChangesAsync();
        return ad.Id;
    }

    private static async Task RunAsync(bool publish)
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        await factory.CreateUserAsync(AdminEmail, "Administradora da Revisão", Password, RoleNames.Administrator);
        await using (AppDbContext settings = SqlServerFixture.NewContext(connection))
        {
            settings.SiteSettings.Add(new SiteSetting { Key = SiteSettingKeys.Phone, Value = "11912345678" });
            await settings.SaveChangesAsync();
        }

        using HttpClient browser = factory.CreateBrowser();
        Assert.AreEqual(HttpStatusCode.Redirect, (await browser.SignInAsync(AdminEmail, Password)).StatusCode);
        int admin;
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            admin = (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.SingleAsync(u => u.Email == AdminEmail)).Id;
        }

        string form = await browser.GetStringAsync("/painel/anuncios/novo");
        string token = Regex.Match(form, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        string action = publish ? "ad.publish" : "ad.reject";
        byte expected = publish ? AdStatus.Published : AdStatus.Rejected;

        for (int round = 1; round <= 3; round++)
        {
            int adId = await AddInReviewAsync(connection, admin, $"Revisão em corrida {round}");
            string path = $"/painel/anuncios/{adId}/{(publish ? "publicar" : "rejeitar")}";

            HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
            {
                Dictionary<string, string> fields = new() { ["__RequestVerificationToken"] = token };
                if (!publish)
                {
                    fields["reason"] = Reason;
                }

                using FormUrlEncodedContent body = new(fields);
                return await browser.PostAsync(path, body);
            })));

            string codes = $"rodada {round}: " + string.Join(",", responses.Select(r => (int)r.StatusCode));
            Assert.IsTrue(responses.All(r => r.StatusCode == HttpStatusCode.Redirect), codes);
            // Quem venceu volta para a fila; quem perdeu ou clicou de novo volta para a pré-visualização com o aviso
            Assert.IsTrue(responses.All(r => r.Destination() == "/painel/anuncios/fila" || r.Destination() == $"/painel/anuncios/{adId}/pre-visualizacao"), codes);
            Assert.IsTrue(responses.Any(r => r.Destination() == "/painel/anuncios/fila"), codes + ": ninguém venceu");

            Ad ad = await AdData.LoadAsync(connection, adId);
            Assert.AreEqual(expected, ad.Status, codes);
            await using AppDbContext check = SqlServerFixture.NewContext(connection);
            int audits = await check.AuditEntries.CountAsync(e => e.Action == action && e.TargetId == adId.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(1, audits, $"rodada {round}: uma única auditoria {action}");
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public Task QuatroPublicacoesAoMesmoTempo_UmaPassagem_UmaAuditoria_SemErro() => RunAsync(publish: true);

    [TestMethod]
    [TestCategory("Integration")]
    public Task QuatroRejeicoesAoMesmoTempo_UmaPassagem_UmaAuditoria_SemErro() => RunAsync(publish: false);
}
