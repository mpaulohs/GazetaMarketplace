using System.Collections.Generic;
using System.Linq;
using GazetaMarketplace.Infrastructure.Logging;
using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Events;

namespace GazetaMarketplace.Web.Tests.Logging;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class MascaramentoTests
#pragma warning restore CA1515
{
    private static (ILogger Logger, ColetorSink Coletor) Criar()
    {
        ColetorSink coletor = new();
        ILogger logger = new LoggerConfiguration()
            .Enrich.With(new MascaramentoEnricher())
            .WriteTo.Sink(coletor)
            .CreateLogger();
        return (logger, coletor);
    }

    [TestMethod]
    public void Email_ApareceMascarado()
    {
        (ILogger logger, ColetorSink coletor) = Criar();

        logger.Warning("Falha de login de {Email}", "maria.silva@exemplo.com.br");
        logger.Information("Detalhe {Texto}", "Usuário joao@exemplo.com não encontrado");

        string tudo = string.Join(" ", coletor.Eventos.Select(ColetorSink.TudoComoTexto));
        Assert.DoesNotContain("maria.silva", tudo);
        Assert.DoesNotContain("joao@", tudo);
        StringAssert.Contains(tudo, "m***@exemplo.com.br");
        StringAssert.Contains(tudo, "j***@exemplo.com");
    }

    [TestMethod]
    public void Senha_NuncaApareceNoLog()
    {
        (ILogger logger, ColetorSink coletor) = Criar();
        var credenciais = new { Email = "ana@exemplo.com", Password = "Segredo123!" };

        logger.Information("Senha {Password} e {NovaSenha} e {Token} e {Authorization}",
            "Segredo123!", "OutraSenha1!", "tok-abc", "Bearer xyz");
        logger.Information("Objeto {@Credenciais}", credenciais);
        logger.Information("Link {Url}", "https://site/redefinir?userId=7&token=ABCDEF123456&x=1");
        logger.Information("Dicionário {@Dados}", new Dictionary<string, string> { ["senha"] = "Segredo123!" });

        string tudo = string.Join(" ", coletor.Eventos.Select(ColetorSink.TudoComoTexto));
        foreach (string segredo in new[] { "Segredo123!", "OutraSenha1!", "tok-abc", "Bearer xyz", "ABCDEF123456", "ana@" })
        {
            Assert.DoesNotContain(segredo, tudo, "Vazou: " + segredo);
        }

        StringAssert.Contains(tudo, "***");
        StringAssert.Contains(tudo, "userId=7");
    }

    [TestMethod]
    public void ValoresComuns_NaoSaoAlterados()
    {
        (ILogger logger, ColetorSink coletor) = Criar();

        logger.Information("Anúncio {AdId} publicado por {UserId}", 42, 7);

        LogEvent e = coletor.Eventos.Single();
        Assert.AreEqual("Anúncio 42 publicado por 7", e.RenderMessage());
    }
}
