using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Health;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class HealthTests
#pragma warning restore CA1515
{
    private static WebFactory Create(FakeDatabaseReadiness fake) =>
        new(services: s => s.AddSingleton<IDatabaseReadiness>(fake));

    [TestMethod]
    public async Task Live_Responde200_SemBanco()
    {
        FakeDatabaseReadiness fake = new() { Failure = new InvalidOperationException("não deveria ser chamado") };
        using WebFactory factory = Create(fake);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/live");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(0, fake.Calls, "/health/live não pode tocar no banco");
    }

    [TestMethod]
    public async Task Ready_SemBanco_Responde503_SemDetalhes()
    {
        FakeDatabaseReadiness fake = new() { Failure = new InvalidOperationException("Server=segredo;Password=abc") };
        using WebFactory factory = Create(fake);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/ready");

        string body = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.AreEqual("Unhealthy", body);
        Assert.DoesNotContain("segredo", body);

        fake.Failure = null;
        fake.Result = new DatabaseReadiness(false, false);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [TestMethod]
    public async Task Ready_ComMigrationPendente_Responde503()
    {
        FakeDatabaseReadiness fake = new() { Result = new DatabaseReadiness(true, false) };
        using WebFactory factory = Create(fake);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/ready");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.AreEqual("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Ready_ComBancoEMigrationEmDia_Responde200()
    {
        using WebFactory factory = Create(new FakeDatabaseReadiness());
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/ready");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("Healthy", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Ready_SemBancoConfigurado_Responde503()
    {
        // Sem cadeia de conexão (Testing), a implementação real não consegue verificar e declara não pronto
        using WebFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/ready");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
