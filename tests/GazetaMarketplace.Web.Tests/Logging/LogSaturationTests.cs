using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GazetaMarketplace.Infrastructure.Logging;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Logging;

/// <summary>SC-04 (log saturável por tráfego anônimo) e SC-12 (o CEP do vendedor não vai ao log do HttpClient).</summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class LogSaturationTests
#pragma warning restore CA1515
{
    private static string NewFolder() => Path.Combine(Path.GetTempPath(), "gazeta-logs-" + Guid.NewGuid().ToString("N"));

    [TestMethod]
    [Timeout(20000, CooperativeCancellation = true)]
    public void TextoLongoSemArroba_NaoTravaOMascaramento() // SC-04: a regex de e-mail era quadrática e sem tempo-limite
    {
        CollectorSink collector = new();
        ILogger logger = new LoggerConfiguration().Enrich.With(new MaskingEnricher()).WriteTo.Sink(collector).CreateLogger();
        string path = "/" + new string('a', 300_000);

        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        logger.Warning("Limite excedido em {Path}", path);
        watch.Stop();

        Assert.IsLessThan(2000, watch.ElapsedMilliseconds, "o mascaramento tem de ter teto de tempo");
        string all = string.Join(" ", collector.Events.Select(CollectorSink.AllAsText));
        Assert.DoesNotContain(new string('a', 5000), all, "o texto que não deu para mascarar não pode seguir inteiro para o arquivo");
    }

    [TestMethod]
    public void ArquivoDeLog_TemTetoDeTamanho_ERola() // SC-04: sem teto, o arquivo diário chegava a 1 GB e o log de segurança parava
    {
        string folder = NewFolder();
        try
        {
            using (Logger logger = SerilogConfiguration.Configure(new LoggerConfiguration(), folder, production: true, fileSizeLimitBytes: 20_000).CreateLogger())
            {
                for (int i = 0; i < 400; i++)
                {
                    logger.Warning("Evento {Numero} {Texto}", i, new string('x', 200));
                }
            }

            string[] files = Directory.GetFiles(folder, "gazeta-*.json");
            Assert.IsGreaterThan(1, files.Length, "ao passar do teto o arquivo rola em vez de descartar");
            string all = string.Concat(files.Select(File.ReadAllText));
            StringAssert.Contains(all, "\"Numero\":399", "o último evento não foi descartado");
            Assert.IsTrue(files.All(f => new FileInfo(f).Length < 20_000 + 2_000), "nenhum arquivo passa do teto (mais um evento)");
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task Limite429_GravaUmAvisoPorIpPorMinuto_ComCaminhoCortado() // SC-04
    {
        using WebFactory factory = new(configuration: new System.Collections.Generic.Dictionary<string, string> { ["RateLimiting:GlobalPerMinute"] = "3" });
        using HttpClient client = factory.CreateClient();
        string longPath = "/" + new string('b', 3000);

        for (int i = 0; i < 40; i++)
        {
            HttpRequestMessage request = new(HttpMethod.Get, longPath);
            request.Headers.Add(WebFactory.RemoteIpHeader, "203.0.113.50");
            await client.SendAsync(request);
        }

        LogEvent[] warnings = factory.Logs.Events.Where(e => CollectorSink.Text(e).Contains("Limite de requisições excedido", StringComparison.Ordinal)).ToArray();
        Assert.HasCount(1, warnings, "37 recusas do mesmo IP no mesmo minuto geram um só aviso");
        Assert.IsLessThan(300, CollectorSink.Text(warnings[0]).Length, "o caminho vai cortado");
    }

    [TestMethod]
    public void LogDoHttpClient_NaoGravaAUrlComOCep() // SC-12
    {
        CollectorSink collector = new();
        using Logger logger = SerilogConfiguration.Configure(new LoggerConfiguration(), logsFolder: null, production: true, extraSinks: [collector]).CreateLogger();

        ILogger http = logger.ForContext("SourceContext", "System.Net.Http.HttpClient.ViaCep.ClientHandler");
        http.Information("Sending HTTP request GET https://viacep.com.br/ws/01310100/json/");
        http.Information("Received HTTP response headers after 120ms - 200");

        string all = string.Join(" ", collector.Events.Select(CollectorSink.AllAsText));
        Assert.DoesNotContain("01310100", all);
        Assert.IsEmpty(collector.Events, "o HttpClient só fala em Warning ou acima");

        http.Warning("Falha ao chamar o serviço");
        Assert.HasCount(1, collector.Events);
    }
}
