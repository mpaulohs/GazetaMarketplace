using System;
using System.IO;
using System.Linq;
using GazetaMarketplace.Infrastructure.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Core;

namespace GazetaMarketplace.Web.Tests.Logging;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class LogEmArquivoTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void Log_VaiParaArquivoJsonDiarioNaPastaConfigurada_ComNiveisCorretos()
    {
        string pasta = Path.Combine(Path.GetTempPath(), "gazeta-logs-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (Logger logger = SerilogConfiguration.Configurar(new LoggerConfiguration(), pasta, producao: true).CreateLogger())
            {
                logger.Information("Linha importante {Email}", "ana@exemplo.com");
                logger.Debug("Linha de depuração");
                logger.ForContext("SourceContext", "Microsoft.AspNetCore.Hosting").Information("Ruído do framework");
                logger.ForContext("SourceContext", "Microsoft.AspNetCore.Hosting").Warning("Aviso do framework");
            }

            string[] arquivos = Directory.GetFiles(pasta, "gazeta-*.json");
            Assert.HasCount(1, arquivos);
            string conteudo = File.ReadAllText(arquivos[0]);
            StringAssert.Contains(conteudo, "Linha importante");
            StringAssert.Contains(conteudo, "a***@exemplo.com");
            Assert.DoesNotContain("ana@exemplo.com", conteudo);
            Assert.DoesNotContain("Linha de depuração", conteudo);
            Assert.DoesNotContain("Ruído do framework", conteudo);
            StringAssert.Contains(conteudo, "Aviso do framework");
        }
        finally
        {
            if (Directory.Exists(pasta))
            {
                Directory.Delete(pasta, recursive: true);
            }
        }
    }

    [TestMethod]
    public void Retencao_ApagaOsArquivosAlemDosUltimos14()
    {
        string pasta = Path.Combine(Path.GetTempPath(), "gazeta-logs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        try
        {
            for (int dia = 1; dia <= 20; dia++)
            {
                File.WriteAllText(Path.Combine(pasta, $"gazeta-202001{dia:00}.json"), "{}");
            }

            using (Logger logger = SerilogConfiguration.Configurar(new LoggerConfiguration(), pasta, producao: true).CreateLogger())
            {
                logger.Information("abre o arquivo de hoje");
            }

            // 14 arquivos retidos no total, contando o de hoje
            Assert.HasCount(14, Directory.GetFiles(pasta, "gazeta-*.json"));
            Assert.IsFalse(File.Exists(Path.Combine(pasta, "gazeta-20200101.json")));
            Assert.IsTrue(File.Exists(Path.Combine(pasta, "gazeta-20200120.json")));
        }
        finally
        {
            Directory.Delete(pasta, recursive: true);
        }
    }
}
