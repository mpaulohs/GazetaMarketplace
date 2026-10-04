using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Photos;
using ImageMagick;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Photos;

/// <summary>Tamanho, vazio, truncado e falha no meio da gravação: nada fica no disco quando a foto é recusada ou quando algo quebra.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class FailureTests
#pragma warning restore CA1515
{
    /// <summary>Armazenamento que grava o original de verdade e quebra ao gravar as versões.</summary>
    private sealed class BreaksOnVersions(IPhotoStorage inner) : IPhotoStorage
    {
        public Task SaveVersionsAsync(string storageKey, byte[] large, byte[] thumb, CancellationToken cancellationToken) =>
            throw new IOException("disco cheio (teste)");

        public Task<string> SaveOriginalAsync(byte[] content, PhotoFormat format, DateTime utcNow, CancellationToken cancellationToken) =>
            inner.SaveOriginalAsync(content, format, utcNow, cancellationToken);

        public Task<Stream> OpenAsync(string storageKey, PhotoSize size, CancellationToken cancellationToken) => inner.OpenAsync(storageKey, size, cancellationToken);

        public Task DeleteAsync(string storageKey, string originalKey, CancellationToken cancellationToken) => inner.DeleteAsync(storageKey, originalKey, cancellationToken);
    }

    /// <summary>Processador de mentira que aceita tudo, para testar só os limites da ingestão.</summary>
    private sealed class AcceptsAnything : IImageProcessor
    {
        public int Calls { get; private set; }

        public ProcessedImage Process(byte[] content, PhotoFormat format)
        {
            Calls++;
            return new ProcessedImage([1, 2, 3], [4, 5], 10, 10);
        }
    }

    [TestMethod]
    public async Task FalhaNoMeio_ApagaArquivosGravados()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        PhotoIngestion ingestion = new(holder.Processor, new BreaksOnVersions(holder.Storage), TimeProvider.System);

        await Assert.ThrowsExactlyAsync<IOException>(() => ingestion.IngestAsync(9, new MemoryStream(PhotoFixtures.Solid(MagickFormat.Jpeg, 200, 100)), CancellationToken.None));

        CollectionAssert.AreEqual(Array.Empty<string>(), holder.Files(), "o original que já tinha sido gravado foi apagado");
    }

    [TestMethod]
    public async Task FalhaAoGravarAsVersoes_NoDiscoDeVerdade_NaoDeixaUmaVersaoSo()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        string key = "11/" + Guid.NewGuid().ToString("N");
        // Um diretório no lugar do arquivo da miniatura faz a segunda gravação falhar depois de a primeira ter dado certo
        Directory.CreateDirectory(Path.Combine(holder.Folder, key + "_480.webp"));

        Exception error = await Assert.ThrowsAsync<Exception>(() => holder.Storage.SaveVersionsAsync(key, [1, 2, 3], [4, 5, 6], CancellationToken.None));
        Assert.IsTrue(error is IOException or UnauthorizedAccessException, "o erro original sobe, não o da limpeza: " + error.GetType().Name);

        Assert.IsFalse(File.Exists(Path.Combine(holder.Folder, key + "_1600.webp")), "a versão grande foi desfeita");
        Assert.IsFalse(Directory.EnumerateFiles(holder.Folder, "*.tmp", SearchOption.AllDirectories).Any());
    }

    [TestMethod]
    public async Task ArquivoIlegivel_NaoGravaNada_NemOOriginal()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        PhotoIngestion ingestion = new(holder.Processor, holder.Storage, TimeProvider.System);
        byte[] whole = PhotoFixtures.JpegWithGps(800, 600);
        byte[] truncated = whole[..(whole.Length / 3)];
        byte[] garbage = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        ValidationException cut = await Assert.ThrowsExactlyAsync<ValidationException>(() => ingestion.IngestAsync(1, new MemoryStream(truncated), CancellationToken.None));
        ValidationException broken = await Assert.ThrowsExactlyAsync<ValidationException>(() => ingestion.IngestAsync(1, new MemoryStream(garbage), CancellationToken.None));

        Assert.AreEqual("Não foi possível ler a foto. Tente outro arquivo", broken.Errors["file"][0]);
        Assert.AreEqual("Não foi possível ler a foto. Tente outro arquivo", cut.Errors["file"][0]);
        CollectionAssert.AreEqual(Array.Empty<string>(), holder.Files());
    }

    [TestMethod]
    [DataRow("Jpeg")]
    [DataRow("Png")]
    [DataRow("Gif")]
    [DataRow("WebP")]
    public async Task ArquivoCortadoNoMeio_EmQualquerFormato_ERecusado(string name)
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        PhotoIngestion ingestion = new(holder.Processor, holder.Storage, TimeProvider.System);
        byte[] whole = PhotoFixtures.Noisy(Enum.Parse<MagickFormat>(name));
        Assert.IsGreaterThan(1000, whole.Length, "a imagem de teste tem corpo para ser cortada");

        // Inteira passa; cortada em 60% (a conexão caiu) não pode virar uma foto pela metade
        StoredPhoto ok = await ingestion.IngestAsync(1, new MemoryStream(whole), CancellationToken.None);
        ValidationException cut = await Assert.ThrowsExactlyAsync<ValidationException>(() => ingestion.IngestAsync(1, new MemoryStream(whole[..(whole.Length * 6 / 10)]), CancellationToken.None));

        Assert.IsNotNull(ok);
        Assert.AreEqual(PhotoMessages.Unreadable, cut.Errors["file"][0]);
        Assert.HasCount(3, holder.Files(), "só a foto inteira deixou arquivos");
    }

    [TestMethod]
    public async Task DezMegabytesExatos_PassamEUmByteAMais_Nao()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        AcceptsAnything processor = new();
        PhotoIngestion ingestion = new(processor, holder.Storage, TimeProvider.System);

        StoredPhoto ok = await ingestion.IngestAsync(1, new MemoryStream(PhotoFixtures.JpegShapedBytes(PhotoLimits.MaxBytes)), CancellationToken.None);
        ValidationException tooBig = await Assert.ThrowsExactlyAsync<ValidationException>(
            () => ingestion.IngestAsync(1, new MemoryStream(PhotoFixtures.JpegShapedBytes(PhotoLimits.MaxBytes + 1)), CancellationToken.None));

        Assert.IsNotNull(ok);
        Assert.AreEqual(1, processor.Calls, "a foto grande demais nem chega ao processador");
        Assert.AreEqual("A foto excede o limite de 10 MB", tooBig.Errors["file"][0]);
    }

    [TestMethod]
    public async Task FluxoSemTamanhoConhecido_ELidoSoAteOLimite()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        PhotoIngestion ingestion = new(new AcceptsAnything(), holder.Storage, TimeProvider.System);
        await using NoLengthStream endless = new();

        ValidationException error = await Assert.ThrowsExactlyAsync<ValidationException>(() => ingestion.IngestAsync(1, endless, CancellationToken.None));

        Assert.AreEqual("A foto excede o limite de 10 MB", error.Errors["file"][0]);
        Assert.IsLessThan(PhotoLimits.MaxBytes + 200_000, endless.BytesServed, "parou de ler logo depois do limite, sem carregar o fluxo todo");
    }

    [TestMethod]
    public async Task DecodificacoesAoMesmoTempo_NuncaPassamDoLimite()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        SlowProcessor processor = new();
        PhotoIngestion ingestion = new(processor, holder.Storage, TimeProvider.System);

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => ingestion.IngestAsync(1, new MemoryStream(PhotoFixtures.JpegShapedBytes(100)), CancellationToken.None)));

        Assert.AreEqual(8, processor.Total);
        Assert.IsLessThanOrEqualTo(PhotoIngestion.MaxConcurrentProcessing, processor.MaxSeen, "no máximo 2 decodificações ao mesmo tempo, para a memória do ImageMagick não estourar");
        Assert.IsGreaterThan(0, processor.MaxSeen);
    }

    /// <summary>Processador lento que mede quantas chamadas estão em andamento ao mesmo tempo.</summary>
    private sealed class SlowProcessor : IImageProcessor
    {
        private int _running;
        private int _max;
        private int _total;

        public int MaxSeen => Volatile.Read(ref _max);

        public int Total => Volatile.Read(ref _total);

        public ProcessedImage Process(byte[] content, PhotoFormat format)
        {
            int now = Interlocked.Increment(ref _running);
            int seen;
            while (now > (seen = Volatile.Read(ref _max)) && Interlocked.CompareExchange(ref _max, now, seen) != seen)
            {
            }

            Thread.Sleep(80);
            Interlocked.Decrement(ref _running);
            Interlocked.Increment(ref _total);
            return new ProcessedImage([1], [2], 10, 10);
        }
    }

    [TestMethod]
    public async Task ArquivoVazio_ERecusadoComMensagemPropria()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        PhotoIngestion ingestion = new(new AcceptsAnything(), holder.Storage, TimeProvider.System);

        ValidationException error = await Assert.ThrowsExactlyAsync<ValidationException>(() => ingestion.IngestAsync(1, new MemoryStream(), CancellationToken.None));

        Assert.AreEqual("O arquivo está vazio", error.Errors["file"][0]);
    }

    /// <summary>Fluxo que não sabe o próprio tamanho e entrega bytes de JPEG sem parar.</summary>
    private sealed class NoLengthStream : Stream
    {
        public long BytesServed { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Array.Fill(buffer, (byte)0xFF, offset, count);
            BytesServed += count;
            return count;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            buffer.Span.Fill(0xFF);
            BytesServed += buffer.Length;
            return ValueTask.FromResult(buffer.Length);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
