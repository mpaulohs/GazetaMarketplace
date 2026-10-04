using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Photos;
using ImageMagick;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Photos;

/// <summary>Reprocessar uma foto a partir do original (manutenção, sem tela na v1) e o que acontece depois que a limpeza de 30 dias apagou o original.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class ReprocessarTests
#pragma warning restore CA1515
{
    private static async Task<(int PhotoId, StoredPhoto Stored)> IngestAsync(CleanupHarness h, MagickImageProcessor processor)
    {
        PhotoIngestion ingestion = new(processor, h.Storage, h.Time);
        StoredPhoto stored = await ingestion.IngestAsync(1, new MemoryStream(PhotoFixtures.Solid(MagickFormat.Jpeg, 2000, 1000)), CancellationToken.None);
        int photoId = await h.AddPhotoAsync(stored.StorageKey, stored.OriginalKey);
        return (photoId, stored);
    }

    private static Task ReprocessAsync(CleanupHarness h, MagickImageProcessor processor, int photoId) =>
        h.WithDbAsync(async db =>
        {
            await new PhotoReprocessing(db, h.Storage, processor).ReprocessAsync(photoId, CancellationToken.None);
            return 0;
        });

    [TestMethod]
    public async Task SemOriginal_FalhaComOriginalIndisponivel_Chave_Nula_ou_ArquivoAusente()
    {
        PhotoFixtures.Init();
        using CleanupHarness h = new();
        MagickImageProcessor processor = new(h.Options);
        int withoutKey = await h.AddPhotoAsync($"1/{CleanupHarness.NewGuid()}", null);
        int missingFile = await h.AddPhotoAsync($"1/{CleanupHarness.NewGuid()}", CleanupHarness.OriginalKey());

        ConflictException noKey = await Assert.ThrowsExactlyAsync<ConflictException>(() => ReprocessAsync(h, processor, withoutKey));
        ConflictException noFile = await Assert.ThrowsExactlyAsync<ConflictException>(() => ReprocessAsync(h, processor, missingFile));

        StringAssert.StartsWith(noKey.Message, "Original indisponível");
        StringAssert.StartsWith(noFile.Message, "Original indisponível");
    }

    [TestMethod]
    public async Task FotoInexistente_Devolve404()
    {
        PhotoFixtures.Init();
        using CleanupHarness h = new();

        await Assert.ThrowsExactlyAsync<NotFoundException>(() => ReprocessAsync(h, new MagickImageProcessor(h.Options), 12345));
    }

    [TestMethod]
    public async Task ComOriginal_RegeraAsDuasVersoes_EAtualizaAsMedidas_SemSobrarTemporario()
    {
        PhotoFixtures.Init();
        using CleanupHarness h = new();
        MagickImageProcessor processor = new(h.Options);
        (int photoId, StoredPhoto stored) = await IngestAsync(h, processor);
        string large = Path.Combine(h.Folder, stored.StorageKey + "_1600.webp");
        string thumb = Path.Combine(h.Folder, stored.StorageKey + "_480.webp");
        byte[] expectedLarge = await File.ReadAllBytesAsync(large);
        await File.WriteAllBytesAsync(large, [0, 0, 0]); // versões estragadas
        await File.WriteAllBytesAsync(thumb, [0, 0, 0]);
        await h.WithDbAsync(async db =>
        {
            AdPhoto row = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.AdPhotos, p => p.Id == photoId);
            row.Width = 1;
            row.Height = 1;
            row.SizeBytes = 1;
            await db.SaveChangesAsync();
            return 0;
        });

        await ReprocessAsync(h, processor, photoId);

        CollectionAssert.AreEqual(expectedLarge, await File.ReadAllBytesAsync(large), "a versão grande volta a ser a mesma do envio original");
        using MagickImage thumbImage = new(await File.ReadAllBytesAsync(thumb));
        Assert.AreEqual(480u, thumbImage.Width);
        AdPhoto after = await h.LoadPhotoAsync(photoId);
        Assert.AreEqual(1600, after.Width);
        Assert.AreEqual(800, after.Height);
        Assert.AreEqual(expectedLarge.Length, after.SizeBytes);
        Assert.AreEqual(0, Directory.GetFiles(Path.Combine(h.Folder, "1"), "*.tmp").Length, "nenhum temporário sobra");
    }

    [TestMethod]
    public async Task DepoisDaLimpezaDe30Dias_OReprocessamentoFalha_EAsVersoesContinuam()
    {
        PhotoFixtures.Init();
        using CleanupHarness h = new();
        MagickImageProcessor processor = new(h.Options);
        (int photoId, StoredPhoto stored) = await IngestAsync(h, processor);
        File.SetLastWriteTimeUtc(Path.Combine(h.Folder, stored.OriginalKey), h.Now - TimeSpan.FromDays(31));
        File.SetLastWriteTimeUtc(Path.Combine(h.Folder, stored.StorageKey + "_1600.webp"), h.Now - TimeSpan.FromDays(31));
        File.SetLastWriteTimeUtc(Path.Combine(h.Folder, stored.StorageKey + "_480.webp"), h.Now - TimeSpan.FromDays(31));

        await h.Service.RunOnceAsync(default);

        Assert.IsFalse(File.Exists(Path.Combine(h.Folder, stored.OriginalKey)), "o original saiu");
        Assert.IsTrue(File.Exists(Path.Combine(h.Folder, stored.StorageKey + "_1600.webp")), "as versões ficam, mesmo com 31 dias e porque têm registro");
        Assert.IsTrue(File.Exists(Path.Combine(h.Folder, stored.StorageKey + "_480.webp")));
        Assert.IsNull((await h.LoadPhotoAsync(photoId)).OriginalKey);
        ConflictException failure = await Assert.ThrowsExactlyAsync<ConflictException>(() => ReprocessAsync(h, processor, photoId));
        StringAssert.StartsWith(failure.Message, "Original indisponível");
    }
}
