using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Cep;

/// <summary><c>GET /api/v1/cep/{cep}</c> (ADR-007): só a equipe, 8 dígitos, 404 sem abrir o manual, 503 para a tela tentar de novo, sem rua nem bairro.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class CepEndpointTests
#pragma warning restore CA1515
{
    private const string Cep = "13015100";

    private static string Url(string cep) => "/api/v1/cep/" + cep;

    [TestMethod]
    public async Task SemLogin_Devolve401_EmJson_SemRedirecionar_ESemConsultar()
    {
        using CepHarness harness = await CepHarness.StartAsync();
        using HttpClient anonymous = harness.Anonymous();

        HttpResponseMessage response = await anonymous.GetAsync(Url(Cep));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "401, não o redirecionamento para a página de entrada");
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using JsonDocument problem = await CepHarness.JsonAsync(response);
        Assert.AreEqual("UNAUTHORIZED", problem.RootElement.GetProperty("code").GetString());
        Assert.IsTrue(problem.RootElement.TryGetProperty("traceId", out _));
        Assert.AreEqual(0, harness.Lookup.Calls);
    }

    [TestMethod]
    public async Task Redator_EAdministrador_Consultam_ERecebemCidadeUfEOrigem()
    {
        using CepHarness harness = await CepHarness.StartAsync();

        foreach (HttpClient client in new[] { harness.Writer, harness.Admin })
        {
            HttpResponseMessage response = await client.GetAsync(Url(Cep));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            using JsonDocument body = await CepHarness.JsonAsync(response);
            Assert.AreEqual(Cep, body.RootElement.GetProperty("cep").GetString());
            Assert.AreEqual("Campinas", body.RootElement.GetProperty("city").GetString());
            Assert.AreEqual("SP", body.RootElement.GetProperty("uf").GetString());
            StringAssert.Contains(response.Content.Headers.ContentType!.MediaType, "json");
        }

        using JsonDocument second = await CepHarness.JsonAsync(await harness.Writer.GetAsync(Url(Cep)));
        Assert.AreEqual("cache", second.RootElement.GetProperty("source").GetString());
        Assert.AreEqual(1, harness.Lookup.Calls, "a segunda consulta saiu do cache");
    }

    [TestMethod]
    [DataRow("1234567")]
    [DataRow("123456789")]
    [DataRow("abcdefgh")]
    [DataRow("13015-100")]
    [DataRow("1301510a")]
    [DataRow("1301 100")]
    public async Task CepIncompletoOuMalformado_NaoConsultaOViaCep_400(string cep)
    {
        using CepHarness harness = await CepHarness.StartAsync();

        HttpResponseMessage response = await harness.Writer.GetAsync(Url(cep));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, cep);
        using JsonDocument problem = await CepHarness.JsonAsync(response);
        Assert.AreEqual("VALIDATION_ERROR", problem.RootElement.GetProperty("code").GetString());
        Assert.AreEqual("Informe um CEP com 8 dígitos", problem.RootElement.GetProperty("errors").GetProperty("cep")[0].GetString());
        Assert.AreEqual(0, harness.Lookup.Calls, "S23: incompleto não consulta nada");
        Assert.IsEmpty(await harness.CacheAsync());
    }

    [TestMethod]
    public async Task ViaCepCom5xxOuSemRede_Devolve503_ENaoEntraNoCache()
    {
        using CepHarness harness = await CepHarness.StartAsync();
        harness.Lookup.Respond = _ => FakeCepLookup.Unavailable();

        HttpResponseMessage response = await harness.Writer.GetAsync(Url(Cep));

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using JsonDocument problem = await CepHarness.JsonAsync(response);
        Assert.AreEqual("CEP_SERVICE_UNAVAILABLE", problem.RootElement.GetProperty("code").GetString());
        Assert.AreEqual("Não foi possível buscar o CEP agora", problem.RootElement.GetProperty("detail").GetString());
        Assert.IsEmpty(await harness.CacheAsync());
    }

    [TestMethod]
    public async Task CepInexistente_Devolve404_NaoEFalhaDoServico_ENaoEntraNoCache()
    {
        using CepHarness harness = await CepHarness.StartAsync();
        harness.Lookup.Respond = _ => null;

        HttpResponseMessage first = await harness.Writer.GetAsync(Url("99999999"));
        HttpResponseMessage second = await harness.Writer.GetAsync(Url("99999999"));

        Assert.AreEqual(HttpStatusCode.NotFound, first.StatusCode);
        using JsonDocument problem = await CepHarness.JsonAsync(first);
        Assert.AreEqual("NOT_FOUND", problem.RootElement.GetProperty("code").GetString(), "o código que NÃO abre o preenchimento manual");
        Assert.AreEqual("CEP não encontrado. Confira os números.", problem.RootElement.GetProperty("detail").GetString());
        Assert.AreEqual(HttpStatusCode.NotFound, second.StatusCode);
        Assert.AreEqual(2, harness.Lookup.Calls, "inexistente não é guardado: um CEP criado depois não fica escondido");
        Assert.IsEmpty(await harness.CacheAsync());
    }

    [TestMethod]
    public async Task Resposta_TemSoCepCidadeUfEOrigem_ENuncaRuaNemBairro()
    {
        // O ViaCEP real, com o cliente de verdade e um servidor de mentira: rua e bairro chegam na resposta externa e não podem sair daqui
        const string payload = """
            {"cep":"13015-100","logradouro":"Rua Barão de Jaguara","complemento":"lado par","bairro":"Centro","localidade":"Campinas","uf":"SP","ibge":"3509502","gia":"2446","ddd":"19","siafi":"6291"}
            """;
        using CepHarness harness = await CepHarness.StartAsync(fakeLookup: false, extraServices: services => WireViaCep(services, _ => Respond(HttpStatusCode.OK, payload)));

        HttpResponseMessage response = await harness.Writer.GetAsync(Url(Cep));

        string text = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
        using JsonDocument body = JsonDocument.Parse(text);
        CollectionAssert.AreEquivalent(new[] { "cep", "city", "uf", "source" }, body.RootElement.EnumerateObject().Select(p => p.Name).ToArray());
        foreach (string leaked in new[] { "Rua", "Jaguara", "Centro", "lado par", "logradouro", "bairro" })
        {
            Assert.DoesNotContain(leaked, text, leaked);
        }

        CepCacheEntry stored = (await harness.CacheAsync()).Single();
        Assert.AreEqual("Campinas", stored.City);
        CollectionAssert.AreEquivalent(
            new[] { "Cep", "City", "Uf", "IbgeCode", "FetchedAt" },
            typeof(CepCacheEntry).GetProperties().Select(p => p.Name).ToArray(), "o cache não tem onde guardar rua nem bairro");
        CollectionAssert.AreEquivalent(new[] { "City", "Uf", "IbgeCode" }, typeof(CepLookupResult).GetProperties().Select(p => p.Name).ToArray());
    }

    [TestMethod]
    public async Task ClienteReal_ConsultaARotaFixaDoViaCep_ComTempoLimiteDeCincoSegundos()
    {
        List<Uri> requested = [];
        using CepHarness harness = await CepHarness.StartAsync(fakeLookup: false, extraServices: services => WireViaCep(services, request =>
        {
            requested.Add(request.RequestUri);
            return Respond(HttpStatusCode.OK, """{"localidade":"Campinas","uf":"SP","ibge":"3509502"}""");
        }));

        await harness.Writer.GetAsync(Url(Cep));

        Assert.HasCount(1, requested);
        Assert.AreEqual("https://viacep.com.br/ws/13015100/json/", requested[0].ToString());
        using IServiceScope scope = harness.Factory.Services.CreateScope();
        Assert.AreEqual(5, scope.ServiceProvider.GetRequiredService<IOptions<ViaCepOptions>>().Value.TimeoutSeconds);
        Assert.AreEqual(TimeSpan.FromSeconds(5), scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(ICepLookup)).Timeout, "uma tentativa de até 5 s (NFR-24)");
    }

    [TestMethod]
    public async Task Redator_NaoTemRotaDeAdministrador_Mas403EmJson_NaoRedireciona()
    {
        using CepHarness harness = await CepHarness.StartAsync();

        HttpResponseMessage writer = await harness.Writer.GetAsync("/api/v1/teste/admin");
        HttpResponseMessage admin = await harness.Admin.GetAsync("/api/v1/teste/admin");

        Assert.AreEqual(HttpStatusCode.Forbidden, writer.StatusCode, "403 em JSON, não o redirecionamento para 'sem permissão'");
        using JsonDocument problem = await CepHarness.JsonAsync(writer);
        Assert.AreEqual("FORBIDDEN", problem.RootElement.GetProperty("code").GetString());
        Assert.AreEqual(HttpStatusCode.OK, admin.StatusCode);
    }

    [TestMethod]
    public async Task Pagina_ContinuaRedirecionandoOAnonimoParaAEntrada()
    {
        using CepHarness harness = await CepHarness.StartAsync();
        using HttpClient anonymous = harness.Anonymous();

        HttpResponseMessage page = await anonymous.GetAsync("/painel/categorias");

        Assert.AreEqual(HttpStatusCode.Redirect, page.StatusCode, "só /api muda: as páginas da equipe seguem redirecionando");
        StringAssert.Contains(page.Destination(), "/painel/entrar");
    }

    internal static HttpResponseMessage Respond(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    /// <summary>Troca o servidor do ViaCEP por um manipulador de teste, mantendo o cliente tipado, o endereço base e o tempo limite reais.</summary>
    internal static void WireViaCep(IServiceCollection services, Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        services.AddHttpClient<ICepLookup, GazetaMarketplace.Infrastructure.Location.ViaCepLookup>()
            .ConfigurePrimaryHttpMessageHandler(() => new StubHandler(respond));
}

/// <summary>Manipulador HTTP de teste: devolve o que o teste manda, ou lança.</summary>
internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) =>
        Task.FromResult(respond(request));
}
