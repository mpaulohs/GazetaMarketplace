using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Photos;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Photos;

/// <summary>A limpeza diária (tarefa 3.6, ADR-005): originais com mais de 30 dias, arquivos órfãos com mais de 24 horas, e nada além disso.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CleanupTests
#pragma warning restore CA1515
{
    private static readonly TimeSpan Day = TimeSpan.FromDays(1);

    private static string Version(string adFolder = "7", string size = "1600") => $"{adFolder}/{CleanupHarness.NewGuid()}_{size}.webp";

    [TestMethod]
    public async Task ApagaSoOriginaisComMaisDeTrintaDias()
    {
        using CleanupHarness h = new();
        string young = h.Write(CleanupHarness.OriginalKey(), Day * 29);
        string exactly = h.Write(CleanupHarness.OriginalKey(), Day * 30);
        string justOver = h.Write(CleanupHarness.OriginalKey(), (Day * 30) + TimeSpan.FromMinutes(1));
        string old = h.Write(CleanupHarness.OriginalKey("2026-06"), Day * 90);

        PhotoCleanupResult result = await h.Service.RunOnceAsync(default);

        Assert.IsTrue(h.Exists(young), "29 dias: fica");
        Assert.IsTrue(h.Exists(exactly), "exatamente 30 dias ainda não é 'mais de 30'");
        Assert.IsFalse(h.Exists(justOver), "30 dias e 1 minuto: sai");
        Assert.IsFalse(h.Exists(old), "90 dias: sai (um atraso não perde nada)");
        Assert.AreEqual(2, result.OriginalsDeleted);
    }

    [TestMethod]
    public async Task OriginalDeFotoRemovida_SemLinhaNoBanco_TambemEApagadoPelaIdade()
    {
        using CleanupHarness h = new();
        string orphanOriginal = h.Write(CleanupHarness.OriginalKey(), Day * 31); // a foto foi removida (D3): o original ficou sem registro

        await h.Service.RunOnceAsync(default);

        Assert.IsFalse(h.Exists(orphanOriginal));
    }

    [TestMethod]
    public async Task AnulaOriginalKey_SoDoArquivoApagado_ELeavaOutrasLinhasEmPaz()
    {
        using CleanupHarness h = new();
        string oldKey = h.Write(CleanupHarness.OriginalKey(), Day * 40);
        string youngKey = h.Write(CleanupHarness.OriginalKey(), Day * 5);
        int oldPhoto = await h.AddPhotoAsync($"7/{CleanupHarness.NewGuid()}", oldKey);
        int youngPhoto = await h.AddPhotoAsync($"7/{CleanupHarness.NewGuid()}", youngKey);
        int withoutOriginal = await h.AddPhotoAsync($"7/{CleanupHarness.NewGuid()}", null);

        PhotoCleanupResult result = await h.Service.RunOnceAsync(default);

        Assert.IsNull((await h.LoadPhotoAsync(oldPhoto)).OriginalKey, "o original apagado perde a chave");
        Assert.AreEqual(youngKey, (await h.LoadPhotoAsync(youngPhoto)).OriginalKey, "o de 5 dias continua");
        Assert.IsNull((await h.LoadPhotoAsync(withoutOriginal)).OriginalKey);
        Assert.AreEqual(1, result.OriginalKeysCleared);
    }

    [TestMethod]
    public async Task VersoesWebp_NuncaSaoApagadas_NemComDoisAnos_SeTemRegistro()
    {
        using CleanupHarness h = new();
        string large = Version();
        string thumb = large.Replace("_1600", "_480", StringComparison.Ordinal);
        h.Write(large, Day * 730);
        h.Write(thumb, Day * 730);
        await h.AddPhotoAsync(large[..large.LastIndexOf('_')], null);

        PhotoCleanupResult result = await h.Service.RunOnceAsync(default);

        Assert.IsTrue(h.Exists(large));
        Assert.IsTrue(h.Exists(thumb));
        Assert.AreEqual(0, result.OrphansDeleted);
    }

    [TestMethod]
    public async Task VersaoSemRegistro_SoEApagadaDepoisDe24Horas()
    {
        using CleanupHarness h = new();
        string older = h.Write(Version(), TimeSpan.FromHours(25));
        string olderThumb = h.Write(Version("8", "480"), TimeSpan.FromHours(30));
        string inProgress = h.Write(Version("9"), TimeSpan.FromHours(23)); // um envio em andamento: gravou os arquivos e ainda vai registrar
        string exactly = h.Write(Version("10"), TimeSpan.FromHours(24));

        PhotoCleanupResult result = await h.Service.RunOnceAsync(default);

        Assert.IsFalse(h.Exists(older));
        Assert.IsFalse(h.Exists(olderThumb));
        Assert.IsTrue(h.Exists(inProgress), "23 horas: dentro da carência");
        Assert.IsTrue(h.Exists(exactly), "exatamente 24 horas ainda está dentro da carência");
        Assert.AreEqual(2, result.OrphansDeleted);
    }

    [TestMethod]
    public async Task ArquivoTemporarioAntigo_SaiComA_CarenciaDe24Horas_EOResto_Fica()
    {
        using CleanupHarness h = new();
        string versionTemp = h.Write($"7/{CleanupHarness.NewGuid()}_1600.webp.{CleanupHarness.NewGuid()}.tmp", TimeSpan.FromHours(25));
        string originalTemp = h.Write($"_originals/2026-08/{CleanupHarness.NewGuid()}.jpg.{CleanupHarness.NewGuid()}.tmp", TimeSpan.FromHours(48));
        string freshTemp = h.Write($"7/{CleanupHarness.NewGuid()}_480.webp.{CleanupHarness.NewGuid()}.tmp", TimeSpan.FromMinutes(5)); // gravação em curso

        PhotoCleanupResult result = await h.Service.RunOnceAsync(default);

        Assert.IsFalse(h.Exists(versionTemp));
        Assert.IsFalse(h.Exists(originalTemp));
        Assert.IsTrue(h.Exists(freshTemp));
        Assert.AreEqual(2, result.OrphansDeleted);
    }

    [TestMethod]
    public async Task NuncaTocaEmMagickNemEmArquivoForaDoFormatoDoSite()
    {
        using CleanupHarness h = new();
        TimeSpan ancient = Day * 800;
        string[] strangers =
        [
            h.Write("_magick/0123456789abcdef/policy.xml", ancient),
            h.Write("_magick/0123456789abcdef/delegates.xml", ancient),
            h.Write("leia-me.txt", ancient),
            h.Write("7/anotacoes.txt", ancient),
            h.Write("abc/" + CleanupHarness.NewGuid() + "_1600.webp", ancient), // pasta que não é um id de anúncio
            h.Write("7/" + CleanupHarness.NewGuid().ToUpperInvariant() + "_1600.webp", ancient), // GUID em maiúsculas: não é o nome do site
            h.Write("7/" + CleanupHarness.NewGuid() + "_800.webp", ancient), // tamanho que o site não gera
            h.Write("_originals/lixo/" + CleanupHarness.NewGuid() + ".jpg", ancient), // pasta que não é um mês
            h.Write("_originals/2026-08/sem-guid.jpg", ancient),
            h.Write("_originals/2026-08/" + CleanupHarness.NewGuid() + ".exe", ancient),
        ];

        PhotoCleanupResult result = await h.Service.RunOnceAsync(default);

        foreach (string path in strangers)
        {
            Assert.IsTrue(h.Exists(path), $"{path} não é um arquivo do site e nunca é apagado");
        }

        Assert.AreEqual(0, result.OriginalsDeleted + result.OrphansDeleted);
    }

    [TestMethod]
    public async Task FalhaDeEscritaEmUmArquivo_NaoInterrompeOsOutros_ENaoAnulaAChaveDele()
    {
        string stuckKey = CleanupHarness.OriginalKey();
        using CleanupHarness harness = new(wrap: inner => new FlakyMaintenance(inner, stuckKey));
        harness.Write(stuckKey, Day * 40);
        string fine = harness.Write(CleanupHarness.OriginalKey(), Day * 40);
        string orphan = harness.Write(Version(), Day * 2);
        int stuckPhoto = await harness.AddPhotoAsync($"7/{CleanupHarness.NewGuid()}", stuckKey);

        PhotoCleanupResult result = await harness.Service.RunOnceAsync(default);

        Assert.IsTrue(harness.Exists(stuckKey), "o arquivo preso fica para amanhã");
        Assert.IsFalse(harness.Exists(fine));
        Assert.IsFalse(harness.Exists(orphan));
        Assert.AreEqual(1, result.Failures);
        Assert.AreEqual(stuckKey, (await harness.LoadPhotoAsync(stuckPhoto)).OriginalKey, "a chave só é anulada se o arquivo foi mesmo apagado");
        Assert.IsTrue(harness.Logs.Entries.Any(e => e.Level == LogLevel.Warning && e.Message.Contains(stuckKey, StringComparison.Ordinal)), "e o motivo vai para o log");
    }

    [TestMethod]
    public async Task Log_TemUmRegistroPorArquivoApagado_EOTotalNoFim()
    {
        using CleanupHarness h = new();
        string original = h.Write(CleanupHarness.OriginalKey(), Day * 45);
        string orphan = h.Write(Version(), Day * 3);
        int photo = await h.AddPhotoAsync($"7/{CleanupHarness.NewGuid()}", original);

        await h.Service.RunOnceAsync(default);

        string[] messages = [.. h.Logs.Entries.Where(e => e.Level == LogLevel.Information).Select(e => e.Message)];
        Assert.IsTrue(messages.Any(m => m.StartsWith("Original apagado", StringComparison.Ordinal) && m.Contains(original, StringComparison.Ordinal)), "uma linha para o original");
        Assert.IsTrue(messages.Any(m => m.StartsWith("Arquivo órfão apagado", StringComparison.Ordinal) && m.Contains(orphan, StringComparison.Ordinal)), "uma linha para o órfão");
        string total = messages[^1];
        StringAssert.StartsWith(total, "Limpeza das fotos:");
        StringAssert.Contains(total, "1 originais apagados (1 OriginalKey anuladas)");
        StringAssert.Contains(total, "1 arquivos órfãos apagados");
        Assert.IsNull((await h.LoadPhotoAsync(photo)).OriginalKey);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("/pasta/que/nao/existe/gazeta-fotos")]
    public async Task SemPastaDeFotosConfigurada_PulaComAviso_SemFalhar(string basePath)
    {
        using CleanupHarness h = new(basePath: basePath);

        PhotoCleanupResult result = await h.Service.RunOnceAsync(default);

        Assert.AreEqual(new PhotoCleanupResult(0, 0, 0, 0, 0), result);
        Assert.IsTrue(h.Logs.Entries.Any(e => e.Level == LogLevel.Warning && e.Message.Contains("pulada", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Anulacao_VaiEmLotesDe500_UmaInstrucaoPorLote()
    {
        using CleanupHarness h = new();
        (string StorageKey, string OriginalKey)[] photos = [.. Enumerable.Range(0, 1200).Select(_ => ($"7/{CleanupHarness.NewGuid()}", CleanupHarness.OriginalKey()))];
        foreach ((_, string originalKey) in photos)
        {
            h.Write(originalKey, Day * 60);
        }

        await h.AddPhotosAsync(photos);
        int before = h.Recorder.Commands.Count;

        PhotoCleanupResult result = await h.Service.RunOnceAsync(default);

        Assert.AreEqual(1200, result.OriginalsDeleted);
        Assert.AreEqual(1200, result.OriginalKeysCleared, "todas as chaves foram anuladas");
        int updates = h.Recorder.Commands.Skip(before).Count(c => c.Contains("UPDATE", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(3, updates, "1200 chaves em lotes de 500: 500 + 500 + 200, uma instrução UPDATE cada");
        Assert.AreEqual(0, await h.WithDbAsync(async db => await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.AdPhotos, p => p.OriginalKey != null)));
    }

    [TestMethod]
    public async Task PastaDeMesVazia_SoSaiNoDiaSeguinte_ENuncaAPastaOriginals()
    {
        using CleanupHarness h = new();
        string original = h.Write(CleanupHarness.OriginalKey("2026-07"), Day * 40);
        string emptiedFolder = Path.GetDirectoryName(Path.Combine(h.Folder, original))!;
        string oldEmpty = Path.Combine(h.Folder, "_originals", "2026-05");
        Directory.CreateDirectory(oldEmpty);
        Directory.SetLastWriteTimeUtc(oldEmpty, h.Now - Day * 10);

        PhotoCleanupResult first = await h.Service.RunOnceAsync(default);

        Assert.IsFalse(Directory.Exists(oldEmpty), "pasta vazia antiga sai");
        Assert.IsTrue(Directory.Exists(emptiedFolder), "a pasta que acabou de ficar vazia espera a carência (um envio pode estar chegando)");
        Assert.AreEqual(1, first.FoldersRemoved);

        h.Time.Advance(Day * 2);
        PhotoCleanupResult second = await h.Service.RunOnceAsync(default);

        Assert.IsFalse(Directory.Exists(emptiedFolder), "no dia seguinte sai");
        Assert.IsTrue(Directory.Exists(Path.Combine(h.Folder, "_originals")), "a raiz de originais fica");
        Assert.AreEqual(1, second.FoldersRemoved);
    }

    [TestMethod]
    public async Task RodaUmMinutoDepoisDaPartida_EDepoisACada24Horas()
    {
        using CleanupHarness h = new();
        string first = h.Write(CleanupHarness.OriginalKey(), Day * 40);
        await h.Service.StartAsync(default);
        try
        {
            await WaitUntilAsync(() => h.Time.TimerCount == 1, h); // o serviço armou o temporizador de 1 minuto
            h.Time.Advance(TimeSpan.FromSeconds(59));
            await Task.Delay(300);
            Assert.IsTrue(h.Exists(first), "antes de 1 minuto nada roda");

            h.Time.Advance(TimeSpan.FromSeconds(1));
            await WaitUntilAsync(() => !h.Exists(first), h);
            await WaitUntilAsync(() => h.Time.TimerCount == 1, h); // e armou o de 24 horas

            string second = h.Write(CleanupHarness.OriginalKey(), Day * 40);
            h.Time.Advance(Day - TimeSpan.FromSeconds(1));
            await Task.Delay(300);
            Assert.IsTrue(h.Exists(second), "23 h 59 min 59 s depois da primeira rodada ainda não rodou");

            h.Time.Advance(TimeSpan.FromSeconds(1));
            await WaitUntilAsync(() => !h.Exists(second), h);
        }
        finally
        {
            await h.Service.StopAsync(default);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CleanupHarness h)
    {
        for (int i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(50);
        }

        Assert.IsTrue(condition(), "a limpeza não rodou a tempo. Log: " + string.Join(" | ", h.Logs.Entries.Select(e => e.Message)));
    }
}
