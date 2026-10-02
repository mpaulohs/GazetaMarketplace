using GazetaMarketplace.Web.Tests.Suporte;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

namespace GazetaMarketplace.Web.Tests.Suporte;

/// <summary>Host de teste em memória (Template A de rules/testing.md): sem banco, sem rede.</summary>
internal sealed class FabricaWeb : WebApplicationFactory<Program>
{
    public ColetorSink Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.AddControllersWithViews().AddApplicationPart(typeof(ApiTesteController).Assembly);
            services.AddSingleton<ILogEventSink>(Logs);
        });
    }
}
