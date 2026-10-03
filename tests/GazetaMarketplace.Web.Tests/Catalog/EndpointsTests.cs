using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Catalog;

/// <summary>Os quatro endpoints do catálogo de veículos (contrato em architecture/api/openapi.yaml): ordem, borda, erros e o tipo (car/moto) em toda consulta.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class EndpointsTests
#pragma warning restore CA1515
{
    private static WebFactory NewFactory() => new(withDatabase: true, seed: context => SmallCatalog.Seed(context));

    private static async Task<(HttpStatusCode Status, JsonElement Body, HttpResponseMessage Response)> GetAsync(WebFactory factory, string url)
    {
        // Sem login e sem cookie: os endpoints são públicos
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync(url);
        return (response.StatusCode, JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement, response);
    }

    private static string[] Names(JsonElement array) => [.. array.EnumerateArray().Select(e => e.GetProperty("name").GetString())];

    private static int[] Ids(JsonElement array) => [.. array.EnumerateArray().Select(e => e.GetProperty("id").GetInt32())];

    [TestMethod]
    public async Task Marcas_PorTipo_EmOrdemAlfabetica()
    {
        using WebFactory factory = NewFactory();

        (HttpStatusCode status, JsonElement cars, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/brands?kind=car");
        (_, JsonElement motos, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/brands?kind=moto");

        Assert.AreEqual(HttpStatusCode.OK, status);
        CollectionAssert.AreEqual(new[] { "Audi", "Honda", "Toyota" }, Names(cars), "alfabética, e não pela ordem dos ids (Honda 1, Toyota 2, Audi 3)");
        CollectionAssert.AreEqual(new[] { 3, 1, 2 }, Ids(cars));
        CollectionAssert.AreEqual(new[] { "BMW", "Honda" }, Names(motos));
    }

    [TestMethod]
    public async Task Respostas_SaoPublicas_ECacheaveisPor10Minutos()
    {
        using WebFactory factory = NewFactory();

        (HttpStatusCode status, _, HttpResponseMessage response) = await GetAsync(factory, "/api/v1/vehicle-catalog/brands?kind=car");

        Assert.AreEqual(HttpStatusCode.OK, status, "nenhum login");
        Assert.IsTrue(response.Headers.CacheControl.Public);
        Assert.AreEqual(System.TimeSpan.FromMinutes(10), response.Headers.CacheControl.MaxAge);
        StringAssert.StartsWith(response.Content.Headers.ContentType.MediaType, "application/json");
    }

    [TestMethod]
    [DataRow("/api/v1/vehicle-catalog/brands")]
    [DataRow("/api/v1/vehicle-catalog/brands?kind=")]
    [DataRow("/api/v1/vehicle-catalog/brands?kind=bike")]
    [DataRow("/api/v1/vehicle-catalog/brands?kind=CAR")]
    [DataRow("/api/v1/vehicle-catalog/brands/1/models")]
    [DataRow("/api/v1/vehicle-catalog/models/11/years?kind=truck")]
    [DataRow("/api/v1/vehicle-catalog/models/11/years/2019/versions")]
    public async Task SemTipoOuComTipoInvalido_Devolve400_ComOCampoNoErro(string url)
    {
        using WebFactory factory = NewFactory();

        (HttpStatusCode status, JsonElement body, _) = await GetAsync(factory, url);

        Assert.AreEqual(HttpStatusCode.BadRequest, status);
        Assert.AreEqual("VALIDATION_ERROR", body.GetProperty("code").GetString());
        Assert.IsTrue(body.GetProperty("errors").TryGetProperty("kind", out _));
    }

    [TestMethod]
    public async Task Modelos_DaHondaDeCarro_NaoMisturamComOsDaHondaDeMoto()
    {
        using WebFactory factory = NewFactory();

        (_, JsonElement cars, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/brands/1/models?kind=car");
        (_, JsonElement motos, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/brands/1/models?kind=moto");

        CollectionAssert.AreEqual(new[] { "City", "Civic", "Fit" }, Names(cars));
        CollectionAssert.AreEqual(new[] { "CG 160" }, Names(motos));
    }

    [TestMethod]
    public async Task Modelos_DeMarcaInexistente_Devolve404()
    {
        using WebFactory factory = NewFactory();

        (HttpStatusCode status, JsonElement body, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/brands/99/models?kind=car");

        Assert.AreEqual(HttpStatusCode.NotFound, status);
        Assert.AreEqual("NOT_FOUND", body.GetProperty("code").GetString());
        Assert.AreEqual(404, body.GetProperty("status").GetInt32());
    }

    [TestMethod]
    public async Task Modelos_DeMarcaQueSoExisteNoOutroTipo_Devolve404()
    {
        using WebFactory factory = NewFactory();

        // A marca 3 (Audi) só existe nos carros
        (HttpStatusCode cars, _, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/brands/3/models?kind=car");
        (HttpStatusCode motos, _, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/brands/3/models?kind=moto");

        Assert.AreEqual(HttpStatusCode.OK, cars);
        Assert.AreEqual(HttpStatusCode.NotFound, motos);
    }

    [TestMethod]
    public async Task Modelos_DeMarcaSemModelos_Devolve200Vazio()
    {
        using WebFactory factory = NewFactory();

        (HttpStatusCode status, JsonElement body, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/brands/3/models?kind=car");

        Assert.AreEqual(HttpStatusCode.OK, status);
        Assert.AreEqual(0, body.GetArrayLength());
    }

    [TestMethod]
    public async Task Anos_EmOrdemDecrescente()
    {
        using WebFactory factory = NewFactory();

        (HttpStatusCode status, JsonElement body, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/models/11/years?kind=car");

        Assert.AreEqual(HttpStatusCode.OK, status);
        CollectionAssert.AreEqual(new[] { 2021, 2020, 2019, 2018 }, body.EnumerateArray().Select(e => e.GetInt32()).ToArray(), "números simples, do mais novo ao mais antigo");
    }

    [TestMethod]
    public async Task Anos_DeModeloSemAnos_Devolve200Vazio_EDeModeloInexistenteOuDeOutroTipo_404()
    {
        using WebFactory factory = NewFactory();

        (HttpStatusCode empty, JsonElement body, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/models/12/years?kind=car");
        (HttpStatusCode missing, _, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/models/99/years?kind=car");
        (HttpStatusCode otherKind, _, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/models/11/years?kind=moto");

        Assert.AreEqual(HttpStatusCode.OK, empty);
        Assert.AreEqual(0, body.GetArrayLength());
        Assert.AreEqual(HttpStatusCode.NotFound, missing);
        Assert.AreEqual(HttpStatusCode.NotFound, otherKind, "o modelo 11 só existe nos carros");
    }

    [TestMethod]
    public async Task Versoes_EmOrdemAlfabetica_ComIdsQueColidemEntreCarroEMoto()
    {
        using WebFactory factory = NewFactory();

        (_, JsonElement cars, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/models/11/years/2019/versions?kind=car");
        (_, JsonElement motos, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/models/10/years/2022/versions?kind=moto");
        (_, JsonElement carVersionOfSameModel, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/models/10/years/2022/versions?kind=car");

        CollectionAssert.AreEqual(new[] { "Advance", "EX 2.0", "LX" }, Names(cars));
        CollectionAssert.AreEqual(new[] { 101, 102, 100 }, Ids(cars));
        CollectionAssert.AreEqual(new[] { "Standard" }, Names(motos), "a versão 100 de moto não é a versão 100 de carro");
        Assert.AreEqual(0, carVersionOfSameModel.GetArrayLength(), "o modelo 10 de carros tem o ano 2022, sem versões");
    }

    [TestMethod]
    public async Task Versoes_DeAnoSemVersoes_Devolve200Vazio_EDeAnoQueOModeloNaoTem_404()
    {
        using WebFactory factory = NewFactory();

        (HttpStatusCode empty, JsonElement body, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/models/11/years/2021/versions?kind=car");
        (HttpStatusCode missingYear, _, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/models/11/years/2005/versions?kind=car");
        (HttpStatusCode missingModel, _, _) = await GetAsync(factory, "/api/v1/vehicle-catalog/models/99/years/2019/versions?kind=car");

        Assert.AreEqual(HttpStatusCode.OK, empty);
        Assert.AreEqual(0, body.GetArrayLength());
        Assert.AreEqual(HttpStatusCode.NotFound, missingYear);
        Assert.AreEqual(HttpStatusCode.NotFound, missingModel);
    }

    [TestMethod]
    [DataRow(1949)]
    [DataRow(2101)]
    [DataRow(0)]
    public async Task Versoes_ComAnoForaDe1950A2100_Devolve400(int year)
    {
        using WebFactory factory = NewFactory();

        (HttpStatusCode status, JsonElement body, _) = await GetAsync(factory, $"/api/v1/vehicle-catalog/models/11/years/{year}/versions?kind=car");

        Assert.AreEqual(HttpStatusCode.BadRequest, status);
        Assert.IsTrue(body.GetProperty("errors").TryGetProperty("year", out _));
    }

    [TestMethod]
    [DataRow("/api/v1/vehicle-catalog/brands/0/models?kind=car")]
    [DataRow("/api/v1/vehicle-catalog/brands/abc/models?kind=car")]
    [DataRow("/api/v1/vehicle-catalog/models/-1/years?kind=car")]
    public async Task IdForaDaRota_Devolve404(string url)
    {
        using WebFactory factory = NewFactory();

        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync(url);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
