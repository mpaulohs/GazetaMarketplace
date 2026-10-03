using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>
/// Cada host de teste (WebApplicationFactory) observa os arquivos de configuração com um inotify do Linux; com centenas de
/// hosts em paralelo o limite padrão (128 instâncias) estoura e o teste falha sem ter relação com o que testa.
/// Os testes nunca editam appsettings durante a execução, então a recarga automática é desligada.
/// Os arquivos estáticos versionados (<c>asp-append-version</c>) também abrem um inotify por host; o monitor por varredura (polling) não usa nenhum.
/// </summary>
[TestClass]
#pragma warning disable CA1515 // Test classes must be public for MSTest
public static class TestHostSettings
#pragma warning restore CA1515
{
    [AssemblyInitialize]
    public static void DisableConfigurationReload(TestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Environment.SetEnvironmentVariable("DOTNET_hostBuilder__reloadConfigOnChange", "false");
        Environment.SetEnvironmentVariable("ASPNETCORE_hostBuilder__reloadConfigOnChange", "false");
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
    }
}
