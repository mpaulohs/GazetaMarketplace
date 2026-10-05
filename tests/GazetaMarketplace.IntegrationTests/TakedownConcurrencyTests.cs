using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// Despublicar e arquivar (US-011) contra o SQL Server real: com <c>RowVersion</c> de verdade, vários pedidos ao mesmo tempo disputam a mesma passagem. Só um vence,
/// só uma auditoria é gravada e nenhum pedido termina em erro 500; quem perde vê a frase da situação.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class TakedownConcurrencyTests
#pragma warning restore CA1515
{
    private const string AdminEmail = "admin.retirada@exemplo.com.br";
    private const string Password = "Senha@Forte1";

    private static async Task<int> AddPublishedAsync(string connection, int authorId, string title)
    {
        Ad ad = Ad.CreateDraft(title, authorId);
        ad.SetText(title, "Descrição completa");
        ad.SetCategory(86);
        ad.SetPrice(5_000);
        ad.SetLocation("13015100", "Campinas", "SP", false);
        ad.SetAttributes(new AdAttributes().Set("conditionId", 2));
        ad.ApplyTransition(AdStatus.InReview, authorId, DateTime.UtcNow, null);
        ad.ApplyTransition(AdStatus.Published, authorId, DateTime.UtcNow, null);
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        context.Ads.Add(ad);
        await context.SaveChangesAsync();
        return ad.Id;
    }

    /// <param name="unpublishers">Quantos pedidos de despublicar disputam o anúncio.</param>
    /// <param name="archivers">Quantos pedidos de arquivar disputam o mesmo anúncio.</param>
    private static async Task RunAsync(int unpublishers, int archivers)
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        await factory.CreateUserAsync(AdminEmail, "Administrador da Retirada", Password, RoleNames.Administrator);
        using HttpClient browser = factory.CreateBrowser();
        Assert.AreEqual(HttpStatusCode.Redirect, (await browser.SignInAsync(AdminEmail, Password)).StatusCode);
        int admin;
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            admin = (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.SingleAsync(u => u.Email == AdminEmail)).Id;
        }

        string form = await browser.GetStringAsync("/painel/anuncios/novo");
        string token = Regex.Match(form, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

        for (int round = 1; round <= 3; round++)
        {
            int adId = await AddPublishedAsync(connection, admin, $"Retirada em corrida {round}");
            List<string> paths = [.. Enumerable.Repeat($"/painel/anuncios/{adId}/despublicar", unpublishers), .. Enumerable.Repeat($"/painel/anuncios/{adId}/arquivar", archivers)];

            HttpResponseMessage[] responses = await Task.WhenAll(paths.Select(path => Task.Run(async () =>
            {
                using FormUrlEncodedContent body = new(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
                return await browser.PostAsync(path, body);
            })));

            string codes = $"rodada {round}: " + string.Join(",", responses.Select(r => (int)r.StatusCode));
            Assert.IsTrue(responses.All(r => r.StatusCode == HttpStatusCode.Redirect), codes);

            Ad ad = await AdData.LoadAsync(connection, adId);
            await using AppDbContext check = SqlServerFixture.NewContext(connection);
            string target = adId.ToString(CultureInfo.InvariantCulture);
            int unpublishAudits = await check.AuditEntries.CountAsync(e => e.Action == "ad.unpublish" && e.TargetId == target);
            int archiveAudits = await check.AuditEntries.CountAsync(e => e.Action == "ad.archive" && e.TargetId == target);
            if (archivers == 0)
            {
                Assert.AreEqual(AdStatus.Draft, ad.Status, codes);
                Assert.AreEqual(1, unpublishAudits, $"rodada {round}: uma única auditoria ad.unpublish");
            }
            else
            {
                // Arquivar vale a partir de Publicado e de Rascunho: o anúncio sempre termina Arquivado, com uma só auditoria de arquivar
                Assert.AreEqual(AdStatus.Archived, ad.Status, codes);
                Assert.AreEqual(1, archiveAudits, $"rodada {round}: uma única auditoria ad.archive");
                Assert.IsLessThanOrEqualTo(1, unpublishAudits, $"rodada {round}: despublicar grava no máximo uma auditoria");
            }
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public Task QuatroDespublicacoesAoMesmoTempo_UmaPassagem_UmaAuditoria_SemErro() => RunAsync(unpublishers: 4, archivers: 0);

    [TestMethod]
    [TestCategory("Integration")]
    public Task QuatroArquivamentosAoMesmoTempo_UmaPassagem_UmaAuditoria_SemErro() => RunAsync(unpublishers: 0, archivers: 4);

    [TestMethod]
    [TestCategory("Integration")]
    public Task DespublicarEArquivarAoMesmoTempo_TerminaArquivado_ComUmaAuditoriaDeArquivar_SemErro() => RunAsync(unpublishers: 2, archivers: 2);
}
