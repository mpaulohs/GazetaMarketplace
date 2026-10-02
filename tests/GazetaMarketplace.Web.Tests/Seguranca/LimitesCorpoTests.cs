using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Seguranca;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class LimitesCorpoTests
#pragma warning restore CA1515
{
    private const int UmMegabyte = 1024 * 1024;

    private static ByteArrayContent Corpo(int bytes) => new(new byte[bytes]);

    [TestMethod]
    public async Task CorpoAcimaDe1Mb_Devolve413_ExcetoNoEnvioDeFoto()
    {
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage dentroDoLimite = await cliente.PostAsync("/api/v1/teste/corpo", Corpo(UmMegabyte));
        HttpResponseMessage acima = await cliente.PostAsync("/api/v1/teste/corpo", Corpo(UmMegabyte + 1));
        HttpResponseMessage foto = await cliente.PostAsync("/api/v1/teste/foto", Corpo(2 * UmMegabyte));

        Assert.AreEqual(HttpStatusCode.OK, dentroDoLimite.StatusCode);
        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, acima.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, foto.StatusCode, "o envio de foto tem limite próprio (11 MB)");
    }
}
