using System;
using GazetaMarketplace.Core.Photos;
using GazetaMarketplace.Performance;
using GazetaMarketplace.Web.Tests.Photos;
using ImageMagick;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Performance;

/// <summary>
/// NFR-05, o elo que o navegador de teste não prova (as fotos do E2E são minúsculas): o peso da lista e do detalhe é "o que não é foto" mais as fotos. O E2E prova o primeiro termo (cabe na folga) e que a lista só baixa
/// a miniatura de 480 px; aqui se prova o segundo. Uma foto de 4000 × 3000 px com grão em cada pixel (o pior caso que o limite de 10 MB deixa passar) vira uma miniatura e uma versão grande que cabem no que sobra do
/// orçamento: 24 miniaturas na lista, e a versão grande mais a faixa de até 20 miniaturas no detalhe.
/// </summary>
[TestClass]
public sealed class PhotoWeightBudgetTests
{
    /// <summary>
    /// O pior caso que o limite de 10 MB deixa passar: 4000 × 3000 px com detalhe fino que <b>sobrevive à redução</b> (grão aplicado na imagem já grande é calculado pela média e some na miniatura; a foto ficava com 13 KB e o teste não provava o limite de 74 KB).
    /// Não há fotografia de câmera no repositório; uma de verdade (HEIC de iPhone, JPEG grande de Android) é conferida à mão no <c>/verify</c>.
    /// </summary>
    private static byte[] GrainyCameraPhoto()
    {
        // Grão de sensor de foto noturna (cada canal varia ±32 de 255 em torno do cinza) em 500 × 375, ampliado oito vezes sem suavizar: a miniatura de 480 px volta a ter um ponto de grão por pixel. Ruído total (0 a 255)
        // deu 101 KB na miniatura, acima dos 74 KB, mas isso não é uma foto: nenhuma câmera produz isso
        byte[] noise = new byte[500 * 375 * 3];
        new Random(20261006).NextBytes(noise);
        using MagickImage image = new(noise, new PixelReadSettings(500, 375, StorageType.Char, PixelMapping.RGB));
        image.Evaluate(Channels.All, EvaluateOperator.Divide, 4);
        image.Evaluate(Channels.All, EvaluateOperator.Add, 96 * Quantum.Max / 255.0);
        image.Sample(4000, 3000);
        image.Format = MagickFormat.Jpeg;
        foreach (uint quality in new uint[] { 92, 85, 75, 65, 55 })
        {
            image.Quality = quality;
            byte[] bytes = image.ToByteArray();
            if (bytes.Length < PhotoLimits.MaxBytes)
            {
                return bytes;
            }
        }

        throw new InvalidOperationException("a foto de teste não coube nos 10 MB nem com qualidade 55");
    }

    [TestMethod]
    public void AGrainyCameraPhoto_FitsTheListAndDetailBudgets_AsAThumbnailAndAsTheLargeVersion()
    {
        Budgets budgets = Budgets.Current;
        using PhotoFixtures.MagickImageProcessorHolder holder = PhotoFixtures.NewProcessor();
        byte[] source = GrainyCameraPhoto();
        Assert.IsLessThan(PhotoLimits.MaxBytes, source.Length, "a foto de teste é aceita pelo limite de 10 MB");

        ProcessedImage result = holder.Processor.Process(source, PhotoFormat.Jpeg);

        using MagickImage thumb = new(result.Thumb);
        using MagickImage large = new(result.Large);
        long listPerCover = (budgets.ListPageWeightMaxBytes - budgets.NonImageAllowanceBytes) / budgets.ListCardCount;
        long detailRoom = budgets.DetailPageWeightMaxBytes - budgets.NonImageAllowanceBytes - ((long)budgets.GalleryMaxPhotos * result.Thumb.Length);
        Console.WriteLine($"FOTO: miniatura {result.Thumb.Length / 1024.0:F0} KB (cabe {listPerCover / 1024} KB por capa na lista) · versão grande {result.Large.Length / 1024.0:F0} KB (cabe {detailRoom / 1024} KB no detalhe, já com {budgets.GalleryMaxPhotos} miniaturas)");
        Assert.IsGreaterThan(listPerCover / 4, result.Thumb.Length, "o pior caso precisa pesar de verdade (ao menos um quarto do que cabe por capa); se não pesa, o teste não prova o limite");
        Assert.AreEqual(480u, thumb.Width, "a lista usa a miniatura de 480 px");
        Assert.AreEqual((uint)PhotoLimits.LargeWidth, large.Width, "o detalhe usa a versão grande de 1600 px");
        Assert.IsLessThanOrEqualTo(listPerCover, result.Thumb.Length, "24 miniaturas desse tamanho estouram o orçamento da lista");
        Assert.IsLessThanOrEqualTo(detailRoom, result.Large.Length, "a versão grande mais 20 miniaturas estouram o orçamento do detalhe");
    }

    [TestMethod]
    public void TheListBudget_LeavesRoomForTheCovers_AndTheDetailBudgetForTheGallery()
    {
        Budgets budgets = Budgets.Current;

        Assert.IsGreaterThan(budgets.NonImageAllowanceBytes * 4, budgets.ListPageWeightMaxBytes, "a folga do que não é foto é pequena perto do orçamento da lista");
        Assert.IsGreaterThan(budgets.ListPageWeightMaxBytes, budgets.DetailPageWeightMaxBytes, "o detalhe pesa mais que a lista (NFR-05: 3 MB contra 2 MB)");
        Assert.AreEqual(24, budgets.ListCardCount);
        Assert.AreEqual(20, budgets.GalleryMaxPhotos);
    }
}
