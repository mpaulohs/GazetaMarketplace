using System.Collections.Generic;
using System.Linq;
using GazetaMarketplace.Infrastructure.Logging;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Logging;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class MaskingTests
#pragma warning restore CA1515
{
    private static (ILogger Logger, CollectorSink Coletor) Create()
    {
        CollectorSink collector = new();
        ILogger logger = new LoggerConfiguration()
            .Enrich.With(new MaskingEnricher())
            .WriteTo.Sink(collector)
            .CreateLogger();
        return (logger, collector);
    }

    [TestMethod]
    public void Email_ApareceMascarado()
    {
        (ILogger logger, CollectorSink collector) = Create();

        logger.Warning("Falha de login de {Email}", "maria.silva@exemplo.com.br");
        logger.Information("Detalhe {Texto}", "Usuário joao@exemplo.com não encontrado");

        string all = string.Join(" ", collector.Events.Select(CollectorSink.AllAsText));
        Assert.DoesNotContain("maria.silva", all);
        Assert.DoesNotContain("joao@", all);
        StringAssert.Contains(all, "m***@exemplo.com.br");
        StringAssert.Contains(all, "j***@exemplo.com");
    }

    [TestMethod]
    public void Senha_NuncaApareceNoLog()
    {
        (ILogger logger, CollectorSink collector) = Create();
        var credentials = new { Email = "ana@exemplo.com", Password = "Segredo123!" };

        logger.Information("Senha {Password} e {NovaSenha} e {Token} e {Authorization}",
            "Segredo123!", "OutraSenha1!", "tok-abc", "Bearer xyz");
        logger.Information("Objeto {@Credenciais}", credentials);
        logger.Information("Link {Url}", "https://site/redefinir?userId=7&token=ABCDEF123456&x=1");
        logger.Information("Dicionário {@Dados}", new Dictionary<string, string> { ["senha"] = "Segredo123!" });

        string all = string.Join(" ", collector.Events.Select(CollectorSink.AllAsText));
        foreach (string secret in new[] { "Segredo123!", "OutraSenha1!", "tok-abc", "Bearer xyz", "ABCDEF123456", "ana@" })
        {
            Assert.DoesNotContain(secret, all, "Vazou: " + secret);
        }

        StringAssert.Contains(all, "***");
        StringAssert.Contains(all, "userId=7");
    }

    [TestMethod]
    public void ValoresComuns_NaoSaoAlterados()
    {
        (ILogger logger, CollectorSink collector) = Create();

        logger.Information("Anúncio {AdId} publicado por {UserId}", 42, 7);

        LogEvent e = collector.Events.Single();
        Assert.AreEqual("Anúncio 42 publicado por 7", e.RenderMessage());
    }
}
