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
public sealed class FileLoggingTests
#pragma warning restore CA1515
{
    [TestMethod]
    public void Log_VaiParaArquivoJsonDiarioNaPastaConfigurada_ComNiveisCorretos()
    {
        string folder = Path.Combine(Path.GetTempPath(), "gazeta-logs-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (Logger logger = SerilogConfiguration.Configure(new LoggerConfiguration(), folder, production: true).CreateLogger())
            {
                logger.Information("Linha importante {Email}", "ana@exemplo.com");
                logger.Debug("Linha de depuração");
                logger.ForContext("SourceContext", "Microsoft.AspNetCore.Hosting").Information("Ruído do framework");
                logger.ForContext("SourceContext", "Microsoft.AspNetCore.Hosting").Warning("Aviso do framework");
            }

            string[] files = Directory.GetFiles(folder, "gazeta-*.json");
            Assert.HasCount(1, files);
            string content = File.ReadAllText(files[0]);
            StringAssert.Contains(content, "Linha importante");
            StringAssert.Contains(content, "a***@exemplo.com");
            Assert.DoesNotContain("ana@exemplo.com", content);
            Assert.DoesNotContain("Linha de depuração", content);
            Assert.DoesNotContain("Ruído do framework", content);
            StringAssert.Contains(content, "Aviso do framework");
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
    public void Retencao_ApagaOsArquivosAlemDosUltimos14()
    {
        string folder = Path.Combine(Path.GetTempPath(), "gazeta-logs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            for (int dia = 1; dia <= 20; dia++)
            {
                File.WriteAllText(Path.Combine(folder, $"gazeta-202001{dia:00}.json"), "{}");
            }

            using (Logger logger = SerilogConfiguration.Configure(new LoggerConfiguration(), folder, production: true).CreateLogger())
            {
                logger.Information("abre o arquivo de hoje");
            }

            // 14 arquivos retidos no total, contando o de hoje
            Assert.HasCount(14, Directory.GetFiles(folder, "gazeta-*.json"));
            Assert.IsFalse(File.Exists(Path.Combine(folder, "gazeta-20200101.json")));
            Assert.IsTrue(File.Exists(Path.Combine(folder, "gazeta-20200120.json")));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
