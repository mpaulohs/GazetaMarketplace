using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Saude;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class HealthTests
#pragma warning restore CA1515
{
    private static FabricaWeb Criar(FakeProntidaoDoBanco fake) =>
        new(servicos: s => s.AddSingleton<IProntidaoDoBanco>(fake));

    [TestMethod]
    public async Task Live_Responde200_SemBanco()
    {
        FakeProntidaoDoBanco fake = new() { Falha = new InvalidOperationException("não deveria ser chamado") };
        using FabricaWeb fabrica = Criar(fake);
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync("/health/live");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        Assert.AreEqual(0, fake.Chamadas, "/health/live não pode tocar no banco");
    }

    [TestMethod]
    public async Task Ready_SemBanco_Responde503_SemDetalhes()
    {
        FakeProntidaoDoBanco fake = new() { Falha = new InvalidOperationException("Server=segredo;Password=abc") };
        using FabricaWeb fabrica = Criar(fake);
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync("/health/ready");

        string corpo = await resposta.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        Assert.AreEqual("Unhealthy", corpo);
        Assert.DoesNotContain("segredo", corpo);

        fake.Falha = null;
        fake.Resultado = new ProntidaoDoBanco(false, false);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, (await cliente.GetAsync("/health/ready")).StatusCode);
    }

    [TestMethod]
    public async Task Ready_ComMigrationPendente_Responde503()
    {
        FakeProntidaoDoBanco fake = new() { Resultado = new ProntidaoDoBanco(true, false) };
        using FabricaWeb fabrica = Criar(fake);
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync("/health/ready");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        Assert.AreEqual("Unhealthy", await resposta.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Ready_ComBancoEMigrationEmDia_Responde200()
    {
        using FabricaWeb fabrica = Criar(new FakeProntidaoDoBanco());
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync("/health/ready");

        Assert.AreEqual(HttpStatusCode.OK, resposta.StatusCode);
        Assert.AreEqual("Healthy", await resposta.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Ready_SemImplementacaoRealDoBanco_Responde503()
    {
        // Até a tarefa 0.6, a implementação provisória nunca declara o banco pronto
        using FabricaWeb fabrica = new();
        using HttpClient cliente = fabrica.CreateClient();

        HttpResponseMessage resposta = await cliente.GetAsync("/health/ready");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
    }
}
