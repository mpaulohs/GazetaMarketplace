using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Photos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Photos;

/// <summary>AR-05: a biblioteca nativa que lê HEIC pode faltar na hospedagem. O site então recusa o HEIC com uma mensagem clara em vez de quebrar.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class HeicTests
#pragma warning restore CA1515
{
    /// <summary>Processador que se comporta como a biblioteca sem o componente de HEIC: lê os outros formatos e recusa o HEIC.</summary>
    private sealed class WithoutHeic(IImageProcessor inner) : IImageProcessor
    {
        public ProcessedImage Process(byte[] content, PhotoFormat format) =>
            format == PhotoFormat.Heic
                ? throw new ValidationException(new System.Collections.Generic.Dictionary<string, string[]> { [PhotoMessages.Field] = [PhotoMessages.HeicUnreadable] })
                : inner.Process(content, format);
    }

    [TestMethod]
    public async Task BibliotecaNativaAusente_DevolveMensagemClara()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        PhotoIngestion ingestion = new(new WithoutHeic(holder.Processor), holder.Storage, TimeProvider.System);

        ValidationException error = await Assert.ThrowsExactlyAsync<ValidationException>(() => ingestion.IngestAsync(1, new MemoryStream(PhotoFixtures.Heic()), CancellationToken.None));

        Assert.AreEqual("Não foi possível converter esta foto HEIC. Envie-a em JPG ou PNG", error.Errors["file"][0]);
        CollectionAssert.AreEqual(Array.Empty<string>(), holder.Files(), "nada é gravado quando o HEIC é recusado");
    }

    [TestMethod]
    public async Task HeicComConteudoQuebrado_TemAMensagemDeHeic_NaoAGenerica()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        PhotoIngestion ingestion = new(holder.Processor, holder.Storage, TimeProvider.System);
        byte[] whole = PhotoFixtures.Heic();

        ValidationException cut = await Assert.ThrowsExactlyAsync<ValidationException>(() => ingestion.IngestAsync(1, new MemoryStream(whole[..(whole.Length / 2)]), CancellationToken.None));
        // Assinatura de HEIC e lixo depois
        byte[] fake = [.. whole[..24], .. Encoding.ASCII.GetBytes("isto não é um HEIC de verdade, só tem a caixa ftyp")];
        ValidationException broken = await Assert.ThrowsExactlyAsync<ValidationException>(() => ingestion.IngestAsync(1, new MemoryStream(fake), CancellationToken.None));

        Assert.AreEqual(PhotoMessages.HeicUnreadable, cut.Errors["file"][0]);
        Assert.AreEqual(PhotoMessages.HeicUnreadable, broken.Errors["file"][0]);
        CollectionAssert.AreEqual(Array.Empty<string>(), holder.Files());
    }

    [TestMethod]
    public async Task HeicValido_ChegaAoDisco_ComAsDuasVersoesEOOriginalHeic()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        PhotoIngestion ingestion = new(holder.Processor, holder.Storage, TimeProvider.System);

        StoredPhoto stored = await ingestion.IngestAsync(21, new MemoryStream(PhotoFixtures.Heic()), CancellationToken.None);

        StringAssert.EndsWith(stored.OriginalKey, ".heic");
        Assert.AreEqual(64, stored.Width);
        Assert.HasCount(3, holder.Files());
    }
}
