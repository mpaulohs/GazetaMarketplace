using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// O clique duplo em "Enviar para revisão" (US-009-S05) contra o SQL Server real: com <c>RowVersion</c> de verdade, dois pedidos ao mesmo tempo disputam a passagem
/// Rascunho → Em revisão. Só um passa; o outro vê o anúncio já enviado. O SQLite dos testes de unidade serializa tudo e não prova a corrida.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class SubmitForReviewConcurrencyTests
#pragma warning restore CA1515
{
    private const string Email = "autora.envio@exemplo.com.br";
    private const string Password = "Senha@Forte1";

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

    [TestMethod]
    [TestCategory("Integration")]
    public async Task QuatroPedidosDeConfirmacaoAoMesmoTempo_UmaPassagem_UmaAuditoria_SemErro()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        using IntegrationWebFactory factory = new(connection);
        await factory.CreateUserAsync(Email, "Autora do Envio", Password, "Redator");
        using HttpClient browser = factory.CreateBrowser();
        Assert.AreEqual(HttpStatusCode.Redirect, (await browser.SignInAsync(Email, Password)).StatusCode);
        int author;
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            author = (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.SingleAsync(u => u.Email == Email)).Id;
        }

        string form = await browser.GetStringAsync("/painel/anuncios/novo");
        string token = Regex.Match(form, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;

        for (int round = 1; round <= 3; round++)
        {
            int adId = await AddCompleteDraftAsync(connection, author, "Envio em corrida " + round);

            HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
            {
                using FormUrlEncodedContent body = new(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
                return await browser.PostAsync($"/painel/anuncios/{adId}/enviar/confirmar", body);
            })));

            Assert.IsTrue(responses.All(r => r.StatusCode == HttpStatusCode.Redirect && r.Headers.Location!.OriginalString.EndsWith("/painel/anuncios", StringComparison.Ordinal)),
                $"rodada {round}: " + string.Join(",", responses.Select(r => (int)r.StatusCode)));
            Ad ad = await AdData.LoadAsync(connection, adId);
            Assert.AreEqual(AdStatus.InReview, ad.Status);
            await using AppDbContext check = SqlServerFixture.NewContext(connection);
            int audits = await check.AuditEntries.CountAsync(e => e.Action == "ad.submit" && e.TargetId == adId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Assert.AreEqual(1, audits, $"rodada {round}: uma única auditoria ad.submit");
        }
    }
}
