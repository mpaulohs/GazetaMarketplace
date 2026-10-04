using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Photos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>
/// A limpeza das fotos (tarefa 3.6) contra o SQL Server real: a anulação de <c>OriginalKey</c> em lotes com <c>IN (…)</c> (o SQLite dos testes de unidade não prova o
/// limite de parâmetros do SQL Server) e a varredura completa com arquivos de data antiga.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PhotoCleanupTests
#pragma warning restore CA1515
{
    private static string Guid32() => Guid.NewGuid().ToString("N");

    private static string Write(string folder, string relative, TimeSpan age)
    {
        string full = Path.Combine(folder, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, [1, 2, 3]);
        File.SetLastWriteTimeUtc(full, DateTime.UtcNow - age);
        return relative;
    }

    private static OriginalsCleanupService NewService(string connection, string folder)
    {
        ServiceCollection services = new();
        services.AddScoped(_ => SqlServerFixture.NewContext(connection));
        IOptions<PhotoStorageOptions> options = Options.Create(new PhotoStorageOptions { BasePath = folder });
        return new OriginalsCleanupService(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            new FileSystemPhotoStorage(options), options, TimeProvider.System, NullLogger<OriginalsCleanupService>.Instance);
    }

    private static string NewFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "gazeta-int-limpeza-" + Guid32());
        Directory.CreateDirectory(folder);
        return folder;
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task MilEDuzentasChaves_SaoAnuladasEmLotes_ESoAsDosArquivosApagados()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        string folder = NewFolder();
        try
        {
            int author = await AdData.AddUserAsync(connection);
            int adId = await AdData.AddDraftAsync(connection, author);
            string[] oldKeys = [.. Enumerable.Range(0, 1200).Select(_ => $"_originals/2026-06/{Guid32()}.jpg")];
            string youngKey = $"_originals/2026-10/{Guid32()}.jpg";
            foreach (string key in oldKeys)
            {
                Write(folder, key, TimeSpan.FromDays(60));
            }

            Write(folder, youngKey, TimeSpan.FromDays(2));
            await using (AppDbContext setup = SqlServerFixture.NewContext(connection))
            {
                int order = 0;
                foreach (string key in oldKeys.Append(youngKey))
                {
                    setup.AdPhotos.Add(new AdPhoto { AdId = adId, SortOrder = order++, StorageKey = $"{adId}/{Guid32()}", OriginalKey = key, Width = 10, Height = 10, SizeBytes = 3 });
                }

                await setup.SaveChangesAsync();
            }

            PhotoCleanupResult result = await NewService(connection, folder).RunOnceAsync(default);

            Assert.AreEqual(1200, result.OriginalsDeleted);
            Assert.AreEqual(1200, result.OriginalKeysCleared);
            await using AppDbContext check = SqlServerFixture.NewContext(connection);
            string[] remaining = await check.AdPhotos.AsNoTracking().Where(p => p.OriginalKey != null).Select(p => p.OriginalKey).ToArrayAsync();
            CollectionAssert.AreEqual(new[] { youngKey }, remaining, "só a chave do original de 2 dias continua");
            Assert.IsTrue(File.Exists(Path.Combine(folder, youngKey)));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task VarreduraCompleta_ApagaOriginaisAntigosEOrfaos_ePoupaOResto()
    {
        string connection = await SqlServerFixture.CreateMigratedDatabaseAsync();
        string folder = NewFolder();
        try
        {
            int author = await AdData.AddUserAsync(connection);
            int adId = await AdData.AddDraftAsync(connection, author);
            string registered = $"{adId}/{Guid32()}";
            string oldOriginal = Write(folder, $"_originals/2026-07/{Guid32()}.jpg", TimeSpan.FromDays(45));
            string removedPhotoOriginal = Write(folder, $"_originals/2026-07/{Guid32()}.png", TimeSpan.FromDays(33)); // foto removida: sem linha
            string youngOriginal = Write(folder, $"_originals/2026-10/{Guid32()}.jpg", TimeSpan.FromDays(3));
            string registeredLarge = Write(folder, registered + "_1600.webp", TimeSpan.FromDays(400));
            string registeredThumb = Write(folder, registered + "_480.webp", TimeSpan.FromDays(400));
            string orphanVersion = Write(folder, $"{adId}/{Guid32()}_1600.webp", TimeSpan.FromHours(30));
            string freshOrphan = Write(folder, $"{adId}/{Guid32()}_480.webp", TimeSpan.FromMinutes(20)); // envio em andamento
            string stalledTemp = Write(folder, $"{adId}/{Guid32()}_1600.webp.{Guid32()}.tmp", TimeSpan.FromDays(2));
            string magick = Write(folder, "_magick/0123456789abcdef/policy.xml", TimeSpan.FromDays(900));
            await using (AppDbContext setup = SqlServerFixture.NewContext(connection))
            {
                setup.AdPhotos.Add(new AdPhoto { AdId = adId, SortOrder = 0, StorageKey = registered, OriginalKey = oldOriginal, Width = 10, Height = 10, SizeBytes = 3 });
                await setup.SaveChangesAsync();
            }

            PhotoCleanupResult result = await NewService(connection, folder).RunOnceAsync(default);

            foreach (string gone in new[] { oldOriginal, removedPhotoOriginal, orphanVersion, stalledTemp })
            {
                Assert.IsFalse(File.Exists(Path.Combine(folder, gone)), gone + " devia ter sido apagado");
            }

            foreach (string kept in new[] { youngOriginal, registeredLarge, registeredThumb, freshOrphan, magick })
            {
                Assert.IsTrue(File.Exists(Path.Combine(folder, kept)), kept + " devia ficar");
            }

            Assert.AreEqual(2, result.OriginalsDeleted);
            Assert.AreEqual(1, result.OriginalKeysCleared, "só a foto que ainda tinha registro tinha chave para anular");
            Assert.AreEqual(2, result.OrphansDeleted);
            await using AppDbContext check = SqlServerFixture.NewContext(connection);
            Assert.IsNull((await check.AdPhotos.AsNoTracking().SingleAsync(p => p.StorageKey == registered)).OriginalKey);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
