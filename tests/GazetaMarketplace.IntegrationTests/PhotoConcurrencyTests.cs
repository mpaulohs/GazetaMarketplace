using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Photos;
using ImageMagick;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// As fotos do anúncio contra o SQL Server real (tarefa 3.5): a posição única de cada foto, o limite que vale mesmo com envios ao mesmo tempo, e a prova de que o envio
/// trava a linha do anúncio (<c>UPDLOCK</c>). O SQLite dos testes de unidade grava uma transação por vez e não tem essa dica, então só aqui a corrida é real.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PhotoConcurrencyTests
#pragma warning restore CA1515
{
    private const string Email = "autor.fotos@exemplo.com.br";
    private const string Password = "Senha@Forte1";

    private sealed class Site : IDisposable
    {
        public required string Connection { get; init; }

        public required string Folder { get; init; }

        public required IntegrationWebFactory Factory { get; init; }

        public required HttpClient Browser { get; init; }

        public required string Token { get; init; }

        public required int AuthorId { get; init; }

        public static async Task<Site> StartAsync()
        {
            MagickRuntime.EnsureInitialized(Path.Combine(Path.GetTempPath(), "gazeta-int-magick"), "XC");
            string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
            string folder = Path.Combine(Path.GetTempPath(), "gazeta-int-fotos-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            IntegrationWebFactory factory = new(connection, services => services.PostConfigure<PhotoStorageOptions>(o => o.BasePath = folder));
            await factory.CreateUserAsync(Email, "Autor das Fotos", Password, "Redator");
            HttpClient browser = factory.CreateBrowser();
            HttpResponseMessage signedIn = await browser.SignInAsync(Email, Password);
            Assert.AreEqual(HttpStatusCode.Redirect, signedIn.StatusCode);
            string panel = await browser.GetStringAsync("/painel/anuncios");
            string token = Regex.Match(panel, @"name=""request-verification-token""[^>]*content=""([^""]+)""").Groups[1].Value;
            int author;
            using (IServiceScope scope = factory.Services.CreateScope())
            {
                author = (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.SingleAsync(u => u.Email == Email)).Id;
            }

            return new Site { Connection = connection, Folder = folder, Factory = factory, Browser = browser, Token = token, AuthorId = author };
        }

        public async Task<int> NewDraftAsync(string title = "Rascunho com fotos") => await AdData.AddDraftAsync(Connection, AuthorId, title: title);

        public async Task<HttpResponseMessage> UploadAsync(int adId)
        {
            using MultipartFormDataContent form = new();
            form.Add(new ByteArrayContent(Jpeg()), "file", "foto.jpg");
            using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/ads/{adId}/photos") { Content = form };
            request.Headers.Add("RequestVerificationToken", Token);
            return await Browser.SendAsync(request);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url)
        {
            using HttpRequestMessage request = new(method, url);
            request.Headers.Add("RequestVerificationToken", Token);
            return await Browser.SendAsync(request);
        }

        public async Task<List<AdPhoto>> RowsAsync(int adId)
        {
            await using AppDbContext context = SqlServerFixture.NewContext(Connection);
            return await context.AdPhotos.AsNoTracking().Where(p => p.AdId == adId).OrderBy(p => p.SortOrder).ToListAsync();
        }

        public void Dispose()
        {
            Browser.Dispose();
            Factory.Dispose();
            try
            {
                Directory.Delete(Folder, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    private static byte[] Jpeg()
    {
        using MagickImage image = new(MagickColors.SteelBlue, 120, 80);
        image.Format = MagickFormat.Jpeg;
        return image.ToByteArray();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task OitoEnviosAoMesmoTempo_NoMesmoAnuncio_GravamPosicoesUnicasDe0a7()
    {
        using Site site = await Site.StartAsync();

        for (int round = 1; round <= 3; round++)
        {
            int adId = await site.NewDraftAsync("Corrida " + round);
            HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => site.UploadAsync(adId))));

            Assert.IsTrue(responses.All(r => r.StatusCode == HttpStatusCode.Created), "rodada " + round + ": " + string.Join(",", responses.Select(r => (int)r.StatusCode)));
            List<AdPhoto> rows = await site.RowsAsync(adId);
            CollectionAssert.AreEqual(Enumerable.Range(0, 8).ToArray(), rows.Select(r => r.SortOrder).ToArray(), "rodada " + round + ": as posições são 0 a 7, sem repetir nem pular");
            Assert.AreEqual(8, rows.Select(r => r.StorageKey).Distinct().Count());
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task SeisEnviosAoMesmoTempo_EmAnuncioCom18Fotos_SoDoisEntram_ETotalFicaEm20()
    {
        using Site site = await Site.StartAsync();
        int adId = await site.NewDraftAsync();
        await using (AppDbContext setup = SqlServerFixture.NewContext(site.Connection))
        {
            for (int i = 0; i < 18; i++)
            {
                setup.AdPhotos.Add(new AdPhoto { AdId = adId, SortOrder = i, StorageKey = $"{adId}/{Guid.NewGuid():N}", Width = 1, Height = 1, SizeBytes = 1 });
            }

            await setup.SaveChangesAsync();
        }

        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Task.Run(() => site.UploadAsync(adId))));

        Assert.AreEqual(2, responses.Count(r => r.StatusCode == HttpStatusCode.Created), "cabem exatamente duas");
        Assert.AreEqual(4, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict), "as outras quatro recebem 409");
        List<AdPhoto> rows = await site.RowsAsync(adId);
        CollectionAssert.AreEqual(Enumerable.Range(0, 20).ToArray(), rows.Select(r => r.SortOrder).ToArray());
        string[] leftover = [.. Directory.EnumerateFiles(site.Folder, "*", SearchOption.AllDirectories).Where(f => !f.Contains("_magick", StringComparison.Ordinal))];
        Assert.AreEqual(2 * 3, leftover.Length, "as quatro recusadas não deixaram arquivo: só as duas fotos gravadas (duas versões e o original cada)");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task OEnvioTravaALinhaDoAnuncio_UPDLOCK_EEsperaQuemJaEstaComEla()
    {
        using Site site = await Site.StartAsync();
        int adId = await site.NewDraftAsync();

        // Outra sessão segura o bloqueio de atualização na linha do anúncio, como um envio em andamento seguraria
        await using SqlConnection holder = new(site.Connection);
        await holder.OpenAsync();
        await using SqlTransaction transaction = holder.BeginTransaction();
        await using (SqlCommand hold = new("SELECT Id FROM Ads WITH (UPDLOCK, ROWLOCK) WHERE Id = @id", holder, transaction))
        {
            hold.Parameters.AddWithValue("@id", adId);
            await hold.ExecuteScalarAsync();
        }

        Task<HttpResponseMessage> upload = Task.Run(() => site.UploadAsync(adId));
        await Task.Delay(TimeSpan.FromSeconds(4)); // tempo de sobra para processar a foto e chegar ao bloqueio

        Assert.IsFalse(upload.IsCompleted, "o envio espera o bloqueio da linha do anúncio");
        Assert.AreEqual(0, (await site.RowsAsync(adId)).Count, "e nada foi gravado enquanto espera");

        await transaction.CommitAsync();
        using HttpResponseMessage response = await upload.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, "liberado o bloqueio, o envio termina");
        Assert.AreEqual(1, (await site.RowsAsync(adId)).Count);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task CapaERemocao_ReordenamSemBuracos_ERespeitamAsRegrasDoBanco()
    {
        using Site site = await Site.StartAsync();
        int adId = await site.NewDraftAsync();
        List<int> ids = [];
        for (int i = 0; i < 4; i++)
        {
            using HttpResponseMessage created = await site.UploadAsync(adId);
            Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
            ids.Add((await site.RowsAsync(adId)).Last().Id);
        }

        using HttpResponseMessage cover = await site.SendAsync(HttpMethod.Post, $"/api/v1/ads/{adId}/photos/{ids[3]}/cover");
        Assert.AreEqual(HttpStatusCode.NoContent, cover.StatusCode);
        List<AdPhoto> afterCover = await site.RowsAsync(adId);
        CollectionAssert.AreEqual(new[] { ids[3], ids[0], ids[1], ids[2] }, afterCover.Select(r => r.Id).ToArray());
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, afterCover.Select(r => r.SortOrder).ToArray());

        using HttpResponseMessage delete = await site.SendAsync(HttpMethod.Delete, $"/api/v1/ads/{adId}/photos/{ids[0]}");
        Assert.AreEqual(HttpStatusCode.NoContent, delete.StatusCode);
        List<AdPhoto> afterDelete = await site.RowsAsync(adId);
        CollectionAssert.AreEqual(new[] { ids[3], ids[1], ids[2] }, afterDelete.Select(r => r.Id).ToArray());
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, afterDelete.Select(r => r.SortOrder).ToArray(), "a remoção deixa as posições sem buraco");
    }
}
