using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Configuration;

[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public sealed class OptionsTests
#pragma warning restore CA1515
{
    private static Dictionary<string, string> FullConfiguration() => new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Server=(local);Database=Teste;Integrated Security=true",
        ["PhotoStorage:BasePath"] = "/dados/fotos",
        ["Logging:FileDirectory"] = "/dados/logs",
        ["DataProtection:KeysDirectory"] = "/dados/chaves",
        ["SendGrid:ApiKey"] = "chave-de-teste",
        ["SendGrid:FromEmail"] = "noreply@exemplo.com.br"
    };

    private static IHost Assemble(Dictionary<string, string> valores, bool production, bool withEnvironmentVariables = false)
    {
        // DisableDefaults: o teste não lê appsettings nem variáveis de ambiente da máquina
        HostApplicationBuilder builder = new(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration.AddInMemoryCollection(valores);
        if (withEnvironmentVariables)
        {
            builder.Configuration.AddEnvironmentVariables();
        }

        builder.Services.AddAppOptions(builder.Configuration, production);
        return builder.Build();
    }

    [TestMethod]
    public async Task Producao_SemPastaDeFotos_FalhaNaPartida()
    {
        Dictionary<string, string> valores = FullConfiguration();
        valores.Remove("PhotoStorage:BasePath");

        using IHost host = Assemble(valores, production: true);

        OptionsValidationException error = await Assert.ThrowsExactlyAsync<OptionsValidationException>(() => host.StartAsync());
        StringAssert.Contains(error.Message, "BasePath");
    }

    [TestMethod]
    public async Task Autenticacao_SemValor_UsaPadraoDe30Minutos()
    {
        using IHost host = Assemble(FullConfiguration(), production: false);

        await host.StartAsync();

        Assert.AreEqual(30, host.Services.GetRequiredService<IOptions<AutenticacaoOptions>>().Value.SessaoMinutos);
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("121")]
    [DataRow("-5")]
    public async Task Autenticacao_ForaDe1A120_FalhaNaPartida_EmQualquerAmbiente(string minutes)
    {
        Dictionary<string, string> valores = FullConfiguration();
        valores["Autenticacao:SessaoMinutos"] = minutes;

        using IHost host = Assemble(valores, production: false);

        OptionsValidationException error = await Assert.ThrowsExactlyAsync<OptionsValidationException>(() => host.StartAsync());
        StringAssert.Contains(error.Message, "SessaoMinutos");
    }

    [TestMethod]
    [DataRow("1")]
    [DataRow("120")]
    public async Task Autenticacao_NosLimites_Sobe(string minutes)
    {
        Dictionary<string, string> valores = FullConfiguration();
        valores["Autenticacao:SessaoMinutos"] = minutes;
        using IHost host = Assemble(valores, production: false);

        await host.StartAsync();

        Assert.AreEqual(int.Parse(minutes, System.Globalization.CultureInfo.InvariantCulture), host.Services.GetRequiredService<IOptions<AutenticacaoOptions>>().Value.SessaoMinutos);
    }

    [TestMethod]
    public async Task Producao_SemChaveSendGrid_FalhaNaPartida()
    {
        Dictionary<string, string> valores = FullConfiguration();
        valores.Remove("SendGrid:ApiKey");

        using IHost host = Assemble(valores, production: true);

        OptionsValidationException error = await Assert.ThrowsExactlyAsync<OptionsValidationException>(() => host.StartAsync());
        StringAssert.Contains(error.Message, "ApiKey");
    }

    [TestMethod]
    public async Task Producao_SemConexaoPastaDeLogsOuChaves_FalhaNaPartida()
    {
        Dictionary<string, string> valores = FullConfiguration();
        valores.Remove("ConnectionStrings:DefaultConnection");
        valores.Remove("Logging:FileDirectory");
        valores.Remove("DataProtection:KeysDirectory");

        using IHost host = Assemble(valores, production: true);

        // Várias opções inválidas: o ValidateOnStart junta os erros num AggregateException
        AggregateException error = await Assert.ThrowsExactlyAsync<AggregateException>(() => host.StartAsync());
        Assert.HasCount(3, error.InnerExceptions);
        Assert.IsTrue(error.InnerExceptions.All(e => e is OptionsValidationException));
        string messages = string.Join(" ", error.InnerExceptions.Select(e => e.Message));
        StringAssert.Contains(messages, "DefaultConnection");
        StringAssert.Contains(messages, "FileDirectory");
        StringAssert.Contains(messages, "KeysDirectory");
    }

    [TestMethod]
    public async Task Producao_ComTudoConfigurado_Sobe()
    {
        using IHost host = Assemble(FullConfiguration(), production: true);

        await host.StartAsync();

        Assert.AreEqual("/dados/fotos", host.Services.GetRequiredService<IOptions<PhotoStorageOptions>>().Value.BasePath);
        await host.StopAsync();
    }

    [TestMethod]
    public async Task Desenvolvimento_SemNada_Sobe()
    {
        using IHost host = Assemble([], production: false);

        await host.StartAsync();
        await host.StopAsync();
    }

    [TestMethod]
    [DoNotParallelize] // altera uma variável de ambiente do processo
    public void VariavelDeAmbiente_TemPrioridadeSobreAppsettings()
    {
        const string variable = "PhotoStorage__BasePath";
        string previous = Environment.GetEnvironmentVariable(variable);
        try
        {
            Environment.SetEnvironmentVariable(variable, "/do/ambiente");
            Dictionary<string, string> valores = FullConfiguration();
            valores["PhotoStorage:BasePath"] = "/do/appsettings";

            using IHost host = Assemble(valores, production: true, withEnvironmentVariables: true);

            Assert.AreEqual("/do/ambiente", host.Services.GetRequiredService<IOptions<PhotoStorageOptions>>().Value.BasePath);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, previous);
        }
    }
}
