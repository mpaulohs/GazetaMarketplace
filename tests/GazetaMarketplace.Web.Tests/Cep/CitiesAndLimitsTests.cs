using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Web.Tests.Categories;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Cep;

/// <summary><c>GET /api/v1/cities?uf=</c> (lista do preenchimento manual) e o limite de 30 consultas de CEP por minuto por usuário.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CitiesAndLimitsTests
#pragma warning restore CA1515
{
    private static City Row(int code, string name, string uf) =>
        new() { IbgeCode = code, Name = name, Uf = uf, NameSearch = GazetaMarketplace.Core.Search.Normalizer.Normalize(name) };

    [TestMethod]
    public async Task SemLogin_Devolve401()
    {
        using CepHarness harness = await CepHarness.StartAsync();
        using HttpClient anonymous = harness.Anonymous();

        HttpResponseMessage response = await anonymous.GetAsync("/api/v1/cities?uf=SP");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task ListaDaUf_EmOrdemAlfabeticaSemAcento_SoDaquelaUf_ComCodigoENome()
    {
        using CepHarness harness = await CepHarness.StartAsync();
        await harness.AddCitiesAsync(
            Row(3550308, "São Paulo", "SP"), Row(3509502, "Campinas", "SP"), Row(3500105, "Adamantina", "SP"), Row(3500303, "Águas de Lindóia", "SP"),
            Row(3304557, "Rio de Janeiro", "RJ"));

        HttpResponseMessage response = await harness.Writer.GetAsync("/api/v1/cities?uf=SP");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = await CepHarness.JsonAsync(response);
        string[] names = [.. body.RootElement.EnumerateArray().Select(c => c.GetProperty("name").GetString())];
        CollectionAssert.AreEqual(new[] { "Adamantina", "Águas de Lindóia", "Campinas", "São Paulo" }, names, "ordem sem considerar acento; nada do RJ");
        JsonElement first = body.RootElement[0];
        Assert.AreEqual(3500105, first.GetProperty("ibgeCode").GetInt32());
        CollectionAssert.AreEquivalent(new[] { "ibgeCode", "name" }, first.EnumerateObject().Select(p => p.Name).ToArray());
    }

    [TestMethod]
    [DataRow("sp")]
    [DataRow("Sp")]
    [DataRow(" SP ")]
    public async Task Uf_NaoDiferenciaCaixa(string uf)
    {
        using CepHarness harness = await CepHarness.StartAsync();
        await harness.AddCitiesAsync(Row(3509502, "Campinas", "SP"));

        using JsonDocument body = await CepHarness.JsonAsync(await harness.Writer.GetAsync("/api/v1/cities?uf=" + System.Uri.EscapeDataString(uf)));

        Assert.AreEqual(1, body.RootElement.GetArrayLength());
    }

    [TestMethod]
    public async Task UfSemCarga_DevolveListaVazia_ATelaMostraCampoDeTexto()
    {
        using CepHarness harness = await CepHarness.StartAsync();
        await harness.AddCitiesAsync(Row(3509502, "Campinas", "SP"));

        HttpResponseMessage response = await harness.Writer.GetAsync("/api/v1/cities?uf=AC");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("[]", (await response.Content.ReadAsStringAsync()).Trim());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("XX")]
    [DataRow("S")]
    [DataRow("SPP")]
    [DataRow("12")]
    public async Task UfInvalidaOuAusente_400(string uf)
    {
        using CepHarness harness = await CepHarness.StartAsync();

        HttpResponseMessage withValue = await harness.Writer.GetAsync("/api/v1/cities?uf=" + uf);
        HttpResponseMessage without = await harness.Writer.GetAsync("/api/v1/cities");

        Assert.AreEqual(HttpStatusCode.BadRequest, withValue.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, without.StatusCode);
        using JsonDocument problem = await CepHarness.JsonAsync(withValue);
        Assert.AreEqual("VALIDATION_ERROR", problem.RootElement.GetProperty("code").GetString());
    }

    [TestMethod]
    public async Task Resposta_FicaNoNavegadorPorDezMinutos_SemCompartilhar()
    {
        using CepHarness harness = await CepHarness.StartAsync();

        HttpResponseMessage response = await harness.Writer.GetAsync("/api/v1/cities?uf=SP");

        string cache = response.Headers.CacheControl!.ToString();
        StringAssert.Contains(cache, "private");
        StringAssert.Contains(cache, "max-age=600");
    }

    [TestMethod]
    public async Task Consulta31_NoMesmoMinuto_Devolve429_OutroUsuarioNaoEhAfetado()
    {
        using CepHarness harness = await CepHarness.StartAsync();

        for (int i = 1; i <= 30; i++)
        {
            Assert.AreEqual(HttpStatusCode.OK, (await harness.Writer.GetAsync("/api/v1/cep/13015100")).StatusCode, $"consulta {i}");
        }

        HttpResponseMessage blocked = await harness.Writer.GetAsync("/api/v1/cep/13015100");
        HttpResponseMessage other = await harness.Admin.GetAsync("/api/v1/cep/13015100");

        Assert.AreEqual(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.IsTrue(blocked.Headers.Contains("Retry-After"));
        using JsonDocument problem = await CepHarness.JsonAsync(blocked);
        Assert.AreEqual("RATE_LIMITED", problem.RootElement.GetProperty("code").GetString());
        Assert.AreEqual(HttpStatusCode.OK, other.StatusCode, "o limite é por usuário, não por IP: a outra pessoa na mesma rede segue consultando");
    }

    [TestMethod]
    public async Task ListaDeCidades_NaoGastaOLimiteDoCep()
    {
        using CepHarness harness = await CepHarness.StartAsync();

        for (int i = 0; i < 30; i++)
        {
            await harness.Writer.GetAsync("/api/v1/cep/13015100");
        }

        Assert.AreEqual(HttpStatusCode.OK, (await harness.Writer.GetAsync("/api/v1/cities?uf=SP")).StatusCode);
    }
}
