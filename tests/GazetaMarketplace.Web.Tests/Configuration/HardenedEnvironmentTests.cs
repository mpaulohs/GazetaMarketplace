using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Email;
using GazetaMarketplace.Web.Security;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Configuration;

/// <summary>V-05, SC-09 e SC-01: qualquer ambiente que não seja Development nem Testing sobe com as proteções de produção; sem proxies conhecidos o site avisa na partida.</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class HardenedEnvironmentTests
#pragma warning restore CA1515
{
    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "teste";

        public string ContentRootPath { get; set; } = "/";

        public IFileProvider ContentRootFileProvider { get; set; }
    }

    private static Exception Capture(Action action)
    {
        try
        {
            action();
        }
        catch (Exception error)
        {
            return error;
        }

        Assert.Fail("deveria ter lançado uma exceção");
        return null;
    }

    [TestMethod]
    [DataRow("Production", true)]
    [DataRow("Staging", true)]
    [DataRow("Homologacao", true)]
    [DataRow("Development", false)]
    [DataRow("Testing", false)]
    public void SoDevelopmentETestingFicamDeFora(string name, bool hardened)
    {
        Assert.AreEqual(hardened, new Env(name).IsHardened());
    }

    [TestMethod]
    public async Task Staging_SobeComHstsESendGrid_ComAConfiguracaoDeProducao()
    {
        using WebFactory staging = new("Staging", WebFactory.ProductionConfiguration());
        using HttpClient client = staging.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/teste/log");

        Assert.IsTrue(response.Headers.Contains("Strict-Transport-Security"), "HSTS em Staging");
        Assert.IsInstanceOfType<SendGridEmailSender>(staging.Services.GetService(typeof(IEmailSender)), "o remetente real em Staging");
    }

    [TestMethod]
    public void Staging_SemConfiguracaoDeProducao_NaoSobe()
    {
        using WebFactory staging = new("Staging");

        Exception error = Capture(() => staging.CreateClient());

        Assert.IsTrue(error is OptionsValidationException || error is AggregateException { InnerExceptions: [OptionsValidationException, ..] } || error.InnerException is OptionsValidationException, error.ToString());
    }

    [TestMethod]
    public void Production_SemProxiesConhecidos_AvisaNaPartida() // SC-01
    {
        using WebFactory production = new("Production", WebFactory.ProductionConfiguration());
        using HttpClient client = production.CreateClient();

        LogEvent[] warnings = production.Logs.Events.Where(e => e.Level == LogEventLevel.Warning && CollectorSink.Text(e).Contains("KnownProxies", StringComparison.Ordinal)).ToArray();

        Assert.HasCount(1, warnings);
        StringAssert.Contains(CollectorSink.Text(warnings[0]), "ForwardedHeaders__KnownProxies__0");
        StringAssert.Contains(CollectorSink.Text(warnings[0]), "20 tentativas");
    }

    [TestMethod]
    public void Production_ComProxyConhecido_NaoAvisa()
    {
        Dictionary<string, string> configuration = WebFactory.ProductionConfiguration();
        configuration["ForwardedHeaders:KnownProxies:0"] = "203.0.113.250";
        using WebFactory production = new("Production", configuration);
        using HttpClient client = production.CreateClient();

        Assert.IsFalse(production.Logs.Events.Any(e => CollectorSink.Text(e).Contains("KnownProxies", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Testing_NaoAvisa()
    {
        using WebFactory testing = new("Testing");
        using HttpClient client = testing.CreateClient();

        Assert.IsFalse(testing.Logs.Events.Any(e => CollectorSink.Text(e).Contains("KnownProxies", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Dpapi_PedidoForaDoWindows_FalhaNaPartidaEmVezDeGravarSemCifra()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("Só prova a recusa fora do Windows.");
        }

        Dictionary<string, string> configuration = WebFactory.ProductionConfiguration();
        configuration["DataProtection:ProtectWithDpapi"] = "true";
        using WebFactory production = new("Production", configuration, withDatabase: true);

        Exception error = Capture(() =>
        {
            using HttpClient client = production.CreateClient();
            client.GetAsync("/painel/entrar").GetAwaiter().GetResult();
        });

        StringAssert.Contains(error.ToString(), "DPAPI só existe no Windows");
    }

    [TestMethod]
    public void Dpapi_Desligado_NaoMudaNada()
    {
        Dictionary<string, string> configuration = WebFactory.ProductionConfiguration();
        configuration["DataProtection:ProtectWithDpapi"] = "false";
        using WebFactory production = new("Production", configuration, withDatabase: true);
        using HttpClient client = production.CreateClient();

        HttpResponseMessage page = client.GetAsync("/painel/entrar").GetAwaiter().GetResult();

        Assert.AreEqual(System.Net.HttpStatusCode.OK, page.StatusCode);
    }
}
