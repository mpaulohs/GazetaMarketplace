using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Infrastructure.Photos;
using ImageMagick;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Photos;

/// <summary>RC-2 e RC-4: limites de recurso, decodificadores desligados, formato imposto na leitura e caminhos confinados à pasta de fotos.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class PhotosSecurityTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void ImagemAcimaDoLimiteDePixels_E_Recusada_AntesDeDecodificar()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        byte[] big = PhotoFixtures.ZeroPng(8000, 8000); // 64 milhões de pixels em poucos KB: uma bomba de descompressão
        Assert.IsLessThan(400_000, big.Length, "o arquivo é pequeno; os pixels é que são muitos");

        System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        ValidationException error = Assert.ThrowsExactly<ValidationException>(() => holder.Processor.Process(big, PhotoFormat.Png));
        clock.Stop();

        Assert.AreEqual("A foto tem dimensões grandes demais", error.Errors["file"][0]);
        Assert.IsLessThan(2000, clock.ElapsedMilliseconds, "a recusa vem do cabeçalho, sem gastar tempo decodificando");
    }

    [TestMethod]
    public void CincoMilhoesDePixelsNoLimite_Passam_EUmPixelAMais_Nao()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();

        ProcessedImage atTheLimit = holder.Processor.Process(PhotoFixtures.ZeroPng(10_000, 5_000), PhotoFormat.Png);
        ValidationException over = Assert.ThrowsExactly<ValidationException>(() => holder.Processor.Process(PhotoFixtures.ZeroPng(10_001, 5_000), PhotoFormat.Png));

        Assert.AreEqual(1600, atTheLimit.Width);
        Assert.AreEqual("A foto tem dimensões grandes demais", over.Errors["file"][0]);
    }

    [TestMethod]
    public void LadoAcimaDe20MilPixels_ERecusado_MesmoComPoucosPixelsNoTotal()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();

        ValidationException error = Assert.ThrowsExactly<ValidationException>(() => holder.Processor.Process(PhotoFixtures.ZeroPng(20_001, 1), PhotoFormat.Png));
        ProcessedImage atTheEdge = holder.Processor.Process(PhotoFixtures.ZeroPng(20_000, 1), PhotoFormat.Png);

        // O próprio decodificador de PNG já recusa o lado acima do limite de recurso, então a mensagem pode ser a de "dimensões" ou a de "não foi possível ler"; a recusa é o que importa
        CollectionAssert.Contains(new[] { PhotoMessages.TooManyPixels, PhotoMessages.Unreadable }, error.Errors["file"][0]);
        Assert.AreEqual(1600, atTheEdge.Width, "20.000 de lado ainda passa");
    }

    [TestMethod]
    [DataRow("SVG")]
    [DataRow("MVG")]
    [DataRow("MSL")]
    [DataRow("URL")]
    [DataRow("HTTP")]
    [DataRow("HTTPS")]
    [DataRow("FTP")]
    [DataRow("TEXT")]
    [DataRow("EPHEMERAL")]
    [DataRow("MSVG")]
    [DataRow("PS")]
    [DataRow("PDF")]
    public void DecodificadoresNaoUsados_EstaoDesligados(string coder)
    {
        PhotoFixtures.Init();

        // Pelo prefixo do decodificador (o mesmo que um nome de arquivo malicioso usaria), num arquivo que existe: a política recusa antes de ler o conteúdo.
        // Um decodificador que esta compilação do ImageMagick nem tem (FTP, HTTPS) também é recusa ("sem delegado").
        string path = Path.Combine(Path.GetTempPath(), "gazeta-politica-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(path, "<svg xmlns='http://www.w3.org/2000/svg' width='10' height='10'><rect width='10' height='10'/></svg>");
        try
        {
            MagickException error = Assert.Throws<MagickException>(() => new MagickImage(coder + ":" + path).Dispose(), $"o decodificador {coder} deveria estar desligado");
            Assert.IsTrue(error is MagickPolicyErrorException or MagickMissingDelegateErrorException, $"{coder}: {error.GetType().Name}");
            if (coder is "SVG" or "MVG" or "MSL" or "MSVG" or "TEXT" or "PS" or "PDF")
            {
                Assert.IsInstanceOfType<MagickPolicyErrorException>(error, $"{coder} existe na compilação e é a política que o recusa");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void DecodificadoresDeTexto_EstaoDesligados()
    {
        PhotoFixtures.Init();

        Assert.ThrowsExactly<MagickPolicyErrorException>(() => new MagickImage("label:texto").Dispose());
        Assert.ThrowsExactly<MagickPolicyErrorException>(() => new MagickImage("caption:texto").Dispose());
        Assert.Throws<MagickException>(() => new MagickImage("@/etc/passwd").Dispose()); // o "@arquivo" (lista de arquivos) não abre nada
    }

    [TestMethod]
    public void OsCincoFormatos_ContinuamLigados_NaPoliticaDeProducao()
    {
        PhotoFixtures.Init();

        foreach (MagickFormat format in new[] { MagickFormat.Jpeg, MagickFormat.Png, MagickFormat.Gif, MagickFormat.WebP })
        {
            using MagickImage image = new(PhotoFixtures.Solid(format, 20, 20), new MagickReadSettings { Format = format });
            Assert.AreEqual(20u, image.Width, format.ToString());
        }
    }

    [TestMethod]
    public void ArquivoSvgOuMvgComExtensaoJpg_E_Recusado_ENaoEhDecodificadoComoSvg()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        // Assinatura de JPEG na frente e SVG/MVG depois: o formato é imposto como JPEG, então o conteúdo nunca vai para um decodificador de SVG
        byte[] svg = [0xFF, 0xD8, 0xFF, .. Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg' width='10' height='10'><rect width='10' height='10'/></svg>")];
        byte[] mvg = [0xFF, 0xD8, 0xFF, .. Encoding.UTF8.GetBytes("push graphic-context\nviewbox 0 0 10 10\nimage over 0,0 0,0 'url(http://127.0.0.1:9/x)'\npop graphic-context")];

        ValidationException fromSvg = Assert.ThrowsExactly<ValidationException>(() => holder.Processor.Process(svg, PhotoFormat.Jpeg));
        ValidationException fromMvg = Assert.ThrowsExactly<ValidationException>(() => holder.Processor.Process(mvg, PhotoFormat.Jpeg));

        Assert.AreEqual(PhotoMessages.Unreadable, fromSvg.Errors["file"][0]);
        Assert.AreEqual(PhotoMessages.Unreadable, fromMvg.Errors["file"][0]);
    }

    [TestMethod]
    public void Politica_EmbutidaNegaTudoEPermiteSoOsCincoFormatos()
    {
        XDocument policy = XDocument.Parse(MagickRuntime.PolicyXml());
        var coders = policy.Descendants("policy").Where(p => (string)p.Attribute("domain") == "coder").ToList();

        Assert.AreEqual("none", (string)coders.First().Attribute("rights"), "a primeira regra nega tudo");
        Assert.AreEqual("*", (string)coders.First().Attribute("pattern"));
        string allowed = (string)coders.Single(c => (string)c.Attribute("pattern") != "*").Attribute("pattern");
        CollectionAssert.AreEquivalent(new[] { "JPEG", "JPG", "PNG", "GIF", "WEBP", "HEIC", "HEIF" }, allowed.Trim('{', '}').Split(','));
        foreach (string forbidden in new[] { "MVG", "MSL", "SVG", "URL", "HTTP", "TEXT", "EPHEMERAL" })
        {
            StringAssert.DoesNotMatch(allowed, new System.Text.RegularExpressions.Regex(@"\b" + forbidden + @"\b"));
        }

        Assert.IsTrue(policy.Descendants("policy").Any(p => (string)p.Attribute("domain") == "delegate" && (string)p.Attribute("rights") == "none"), "delegados externos desligados");
        Assert.IsTrue(policy.Descendants("policy").Any(p => (string)p.Attribute("domain") == "path" && (string)p.Attribute("pattern") == "@*"), "@arquivo desligado");
    }

    [TestMethod]
    public void PastaDeConfiguracao_LevaOHashDaPolitica_EUmaPoliticaNovaNuncaReusaAPastaAntiga()
    {
        string policy = MagickRuntime.PolicyXml();

        string current = MagickRuntime.ConfigFolder("/dados/fotos", policy);
        string looser = MagickRuntime.ConfigFolder("/dados/fotos", policy.Replace("{JPEG", "{SVG,JPEG", StringComparison.Ordinal));

        Assert.AreEqual(current, MagickRuntime.ConfigFolder("/dados/fotos", policy), "a mesma política cai sempre na mesma pasta");
        Assert.AreNotEqual(current, looser, "outra política, outra pasta (o ImageMagick não sobrescreve arquivos existentes)");
        StringAssert.StartsWith(current, "/dados/fotos/_magick/".Replace('/', Path.DirectorySeparatorChar));
    }

    [TestMethod]
    public void LimitesDeRecursoDoMagick_FicamLigados()
    {
        PhotoFixtures.Init();

        Assert.AreEqual(MagickRuntime.MemoryLimitBytes, ResourceLimits.Memory);
        Assert.AreEqual(MagickRuntime.TimeLimitSeconds, ResourceLimits.Time);
        Assert.AreEqual((ulong)PhotoLimits.MaxSide, ResourceLimits.Width);
        Assert.AreEqual((ulong)PhotoLimits.MaxSide, ResourceLimits.Height);
    }

    // ---------- RC-4: caminhos ----------

    [TestMethod]
    [DataRow("../x")]
    [DataRow("1/../../etc/passwd")]
    [DataRow("1/../2/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [DataRow("/etc/passwd")]
    [DataRow("1/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [DataRow("1/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/../x")]
    [DataRow("1\\aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [DataRow("_originals/2026-10/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [DataRow("0/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [DataRow("1/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.webp")]
    [DataRow("")]
    public async Task ChaveForaDoFormato_ERecusada_SemTocarNoDisco(string key)
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();

        Assert.ThrowsExactly<ArgumentException>(() => holder.Storage.OpenAsync(key, PhotoSize.Large, CancellationToken.None).GetAwaiter().GetResult());
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => holder.Storage.SaveVersionsAsync(key, [1], [2], CancellationToken.None));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => holder.Storage.DeleteAsync(key, null, CancellationToken.None));
        CollectionAssert.AreEqual(Array.Empty<string>(), holder.Files());
    }

    [TestMethod]
    [DataRow("../../etc/passwd")]
    [DataRow("_originals/../../x.jpg")]
    [DataRow("_originals/2026-10/../../../x.jpg")]
    [DataRow("1/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa_1600.webp")]
    [DataRow("_originals/2026-13/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg")]
    [DataRow("_originals/2026-10/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.exe")]
    public async Task ChaveDeOriginalForaDoFormato_ERecusada(string originalKey)
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => holder.Storage.DeleteAsync(null, originalKey, CancellationToken.None));
    }

    [TestMethod]
    public async Task OsArquivosGravadosFicamDentroDaPastaBase_ENuncaTemONomeEnviado()
    {
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        PhotoIngestion ingestion = new(holder.Processor, holder.Storage, TimeProvider.System);

        // O nome enviado nem chega à ingestão: só o conteúdo. Os nomes no disco são ids numéricos e GUIDs gerados
        await ingestion.IngestAsync(12, new MemoryStream(PhotoFixtures.Solid(MagickFormat.Png, 30, 30)), CancellationToken.None);

        string root = Path.GetFullPath(holder.Folder);
        foreach (string file in Directory.EnumerateFiles(holder.Folder, "*", SearchOption.AllDirectories))
        {
            Assert.IsTrue(Path.GetFullPath(file).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal));
        }

        foreach (string relative in holder.Files())
        {
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(relative,
                @"^(12/[0-9a-f]{32}_(1600|480)\.webp|_originals/\d{4}-\d{2}/[0-9a-f]{32}\.png)$"), relative);
        }
    }

    [TestMethod]
    public async Task PastaBaseNaoConfigurada_DaErroClaro()
    {
        PhotoFixtures.Init();
        FileSystemPhotoStorage storage = new(Microsoft.Extensions.Options.Options.Create(new GazetaMarketplace.Core.Configuration.PhotoStorageOptions()));

        InvalidOperationException error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => storage.SaveVersionsAsync("1/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", [1], [2], CancellationToken.None));

        StringAssert.Contains(error.Message, "PhotoStorage:BasePath");
    }
}
