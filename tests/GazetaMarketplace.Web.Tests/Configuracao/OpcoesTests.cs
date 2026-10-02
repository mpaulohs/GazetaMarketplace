using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuracao;
using GazetaMarketplace.Infrastructure.Configuracao;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Configuracao;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class OpcoesTests
#pragma warning restore CA1515
{
    private static Dictionary<string, string> ConfiguracaoCompleta() => new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Server=(local);Database=Teste;Integrated Security=true",
        ["PhotoStorage:BasePath"] = "/dados/fotos",
        ["Logging:FileDirectory"] = "/dados/logs",
        ["DataProtection:KeysDirectory"] = "/dados/chaves",
        ["SendGrid:ApiKey"] = "chave-de-teste",
        ["SendGrid:FromEmail"] = "noreply@exemplo.com.br"
    };

    private static IHost Montar(Dictionary<string, string> valores, bool producao, bool comVariaveisDeAmbiente = false)
    {
        // DisableDefaults: o teste não lê appsettings nem variáveis de ambiente da máquina
        HostApplicationBuilder builder = new(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration.AddInMemoryCollection(valores);
        if (comVariaveisDeAmbiente)
        {
            builder.Configuration.AddEnvironmentVariables();
        }

        builder.Services.AddOpcoes(builder.Configuration, producao);
        return builder.Build();
    }

    [TestMethod]
    public async Task Producao_SemPastaDeFotos_FalhaNaPartida()
    {
        Dictionary<string, string> valores = ConfiguracaoCompleta();
        valores.Remove("PhotoStorage:BasePath");

        using IHost host = Montar(valores, producao: true);

        OptionsValidationException erro = await Assert.ThrowsExactlyAsync<OptionsValidationException>(() => host.StartAsync());
        StringAssert.Contains(erro.Message, "BasePath");
    }

    [TestMethod]
    public async Task Producao_SemChaveSendGrid_FalhaNaPartida()
    {
        Dictionary<string, string> valores = ConfiguracaoCompleta();
        valores.Remove("SendGrid:ApiKey");

        using IHost host = Montar(valores, producao: true);

        OptionsValidationException erro = await Assert.ThrowsExactlyAsync<OptionsValidationException>(() => host.StartAsync());
        StringAssert.Contains(erro.Message, "ApiKey");
    }

    [TestMethod]
    public async Task Producao_SemConexaoPastaDeLogsOuChaves_FalhaNaPartida()
    {
        Dictionary<string, string> valores = ConfiguracaoCompleta();
        valores.Remove("ConnectionStrings:DefaultConnection");
        valores.Remove("Logging:FileDirectory");
        valores.Remove("DataProtection:KeysDirectory");

        using IHost host = Montar(valores, producao: true);

        // Várias opções inválidas: o ValidateOnStart junta os erros num AggregateException
        AggregateException erro = await Assert.ThrowsExactlyAsync<AggregateException>(() => host.StartAsync());
        Assert.HasCount(3, erro.InnerExceptions);
        Assert.IsTrue(erro.InnerExceptions.All(e => e is OptionsValidationException));
        string mensagens = string.Join(" ", erro.InnerExceptions.Select(e => e.Message));
        StringAssert.Contains(mensagens, "DefaultConnection");
        StringAssert.Contains(mensagens, "FileDirectory");
        StringAssert.Contains(mensagens, "KeysDirectory");
    }

    [TestMethod]
    public async Task Producao_ComTudoConfigurado_Sobe()
    {
        using IHost host = Montar(ConfiguracaoCompleta(), producao: true);

        await host.StartAsync();

        Assert.AreEqual("/dados/fotos", host.Services.GetRequiredService<IOptions<PhotoStorageOptions>>().Value.BasePath);
        await host.StopAsync();
    }

    [TestMethod]
    public async Task Desenvolvimento_SemNada_Sobe()
    {
        using IHost host = Montar([], producao: false);

        await host.StartAsync();
        await host.StopAsync();
    }

    [TestMethod]
    [DoNotParallelize] // altera uma variável de ambiente do processo
    public void VariavelDeAmbiente_TemPrioridadeSobreAppsettings()
    {
        const string variavel = "PhotoStorage__BasePath";
        string anterior = Environment.GetEnvironmentVariable(variavel);
        try
        {
            Environment.SetEnvironmentVariable(variavel, "/do/ambiente");
            Dictionary<string, string> valores = ConfiguracaoCompleta();
            valores["PhotoStorage:BasePath"] = "/do/appsettings";

            using IHost host = Montar(valores, producao: true, comVariaveisDeAmbiente: true);

            Assert.AreEqual("/do/ambiente", host.Services.GetRequiredService<IOptions<PhotoStorageOptions>>().Value.BasePath);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variavel, anterior);
        }
    }
}
