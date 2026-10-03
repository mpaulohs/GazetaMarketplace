using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Security;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class BodyLimitTests
#pragma warning restore CA1515
{
    private const int OneMegabyte = 1024 * 1024;

    private static ByteArrayContent Body(int bytes) => new(new byte[bytes]);

    [TestMethod]
    public async Task CorpoAcimaDe1Mb_Devolve413_ExcetoNoEnvioDeFoto()
    {
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage withinLimit = await client.PostAsync("/api/v1/teste/corpo", Body(OneMegabyte));
        HttpResponseMessage above = await client.PostAsync("/api/v1/teste/corpo", Body(OneMegabyte + 1));
        HttpResponseMessage photo = await client.PostAsync("/api/v1/teste/foto", Body(2 * OneMegabyte));

        Assert.AreEqual(HttpStatusCode.OK, withinLimit.StatusCode);
        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, above.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, photo.StatusCode, "o envio de foto tem limite próprio (11 MB)");
    }
}
