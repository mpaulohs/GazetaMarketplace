using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Photos;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// A entrega de fotos contra o SQL Server real e o disco de verdade: o site inteiro (Program.cs) sobe com a pasta de fotos de um diretório temporário.
/// Prova a consulta (foto × anúncio) com as chaves estrangeiras reais e a regra "público só se publicado".
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PhotoDeliveryTests
#pragma warning restore CA1515
{
    private static byte[] Jpeg(uint width, uint height)
    {
        MagickRuntime.EnsureInitialized(Path.Combine(Path.GetTempPath(), "gazeta-int-magick"), "XC");
        using MagickImage image = new(MagickColors.SteelBlue, width, height);
        image.Format = MagickFormat.Jpeg;
        return image.ToByteArray();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task FotoDeAnuncioPublicado_EPublica_EDeRascunho_SoParaOAutor()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        string folder = Path.Combine(Path.GetTempPath(), "gazeta-int-fotos-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            MagickRuntime.EnsureInitialized(folder, "XC");
            using IntegrationWebFactory factory = new(connection, services => services.PostConfigure<PhotoStorageOptions>(o => o.BasePath = folder));
            await factory.CreateUserAsync("autor@exemplo.com.br", "Autor", "Senha@Forte1", "Redator");
            int author = (await factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>().Users.SingleAsync(u => u.Email == "autor@exemplo.com.br")).Id;

            int published = await AdData.AddDraftAsync(connection, author, title: "Publicado");
            int draft = await AdData.AddDraftAsync(connection, author, title: "Rascunho");
            await using (AppDbContext setup = SqlServerFixture.NewContext(connection))
            {
                Ad ad = await setup.Ads.SingleAsync(a => a.Id == published);
                ad.ApplyTransition(AdStatus.InReview, author, DateTime.UtcNow, null);
                ad.ApplyTransition(AdStatus.Published, author, DateTime.UtcNow, null);
                await setup.SaveChangesAsync();
            }

            (int PhotoId, StoredPhoto Stored) publishedPhoto = await AddPhotoAsync(factory, connection, published);
            (int PhotoId, StoredPhoto Stored) draftPhoto = await AddPhotoAsync(factory, connection, draft);
            using HttpClient anonymous = factory.CreateBrowser();

            using HttpResponseMessage open = await anonymous.GetAsync($"/fotos/{published}/{publishedPhoto.PhotoId}-480.webp");
            using HttpResponseMessage hidden = await anonymous.GetAsync($"/fotos/{draft}/{draftPhoto.PhotoId}-480.webp");
            using HttpResponseMessage swapped = await anonymous.GetAsync($"/fotos/{draft}/{publishedPhoto.PhotoId}-480.webp");

            Assert.AreEqual(HttpStatusCode.OK, open.StatusCode);
            Assert.AreEqual("public, max-age=31536000, immutable", open.Headers.GetValues("Cache-Control").Single());
            Assert.AreEqual(480u, new MagickImage(await open.Content.ReadAsByteArrayAsync()).Width);
            Assert.AreEqual(HttpStatusCode.NotFound, hidden.StatusCode);
            Assert.AreEqual(HttpStatusCode.NotFound, swapped.StatusCode, "foto de outro anúncio");

            // O autor logado vê o rascunho dele, sem cache público
            using HttpClient browser = factory.CreateBrowser();
            string page = await browser.GetStringAsync("/painel/entrar");
            string token = System.Text.RegularExpressions.Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
            using FormUrlEncodedContent form = new(new System.Collections.Generic.Dictionary<string, string>
            {
                ["Email"] = "autor@exemplo.com.br",
                ["Password"] = "Senha@Forte1",
                ["ReturnUrl"] = string.Empty,
                ["__RequestVerificationToken"] = token
            });
            using HttpResponseMessage signedIn = await browser.PostAsync("/painel/entrar", form);
            Assert.AreEqual(HttpStatusCode.Redirect, signedIn.StatusCode);
            using HttpResponseMessage mine = await browser.GetAsync($"/fotos/{draft}/{draftPhoto.PhotoId}-1600.webp");
            Assert.AreEqual(HttpStatusCode.OK, mine.StatusCode);
            CollectionAssert.AreEquivalent(new[] { "no-store", "private" }, mine.Headers.GetValues("Cache-Control").Single().Split(',', StringSplitOptions.TrimEntries));
        }
        finally
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    private static async Task<(int PhotoId, StoredPhoto Stored)> AddPhotoAsync(IntegrationWebFactory factory, string connection, int adId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        StoredPhoto stored = await scope.ServiceProvider.GetRequiredService<IPhotoIngestion>().IngestAsync(adId, new MemoryStream(Jpeg(1200, 800)), CancellationToken.None);
        await using AppDbContext context = SqlServerFixture.NewContext(connection);
        AdPhoto photo = new() { AdId = adId, SortOrder = 0, StorageKey = stored.StorageKey, OriginalKey = stored.OriginalKey, Width = stored.Width, Height = stored.Height, SizeBytes = stored.SizeBytes };
        context.AdPhotos.Add(photo);
        await context.SaveChangesAsync();
        return (photo.Id, stored);
    }
}
