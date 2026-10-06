using System;
using System.Globalization;
using System.IO;
using System.Linq;
using GazetaMarketplace.Core;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure;
using GazetaMarketplace.Infrastructure.Configuration;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Infrastructure.Logging;
using GazetaMarketplace.Web.Assets;
using GazetaMarketplace.Web.Filters;
using GazetaMarketplace.Web.HealthChecks;
using GazetaMarketplace.Web.Middleware;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;

var builder = WebApplication.CreateBuilder(args);

// Cultura fixa pt-BR (NFR-20): R$, vírgula decimal e dd/mm/aaaa em qualquer servidor
CultureInfo ptBr = new("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBr;
CultureInfo.DefaultThreadCurrentUICulture = ptBr;

// preserveStaticLogger: o logger vive só no host (testes em paralelo não disputam o Log.Logger)
builder.Host.UseSerilog(
    (context, services, configuration) => SerilogConfiguration.Configure(
        configuration,
        context.Configuration["Logging:FileDirectory"],
        context.HostingEnvironment.IsProduction(),
        services.GetServices<ILogEventSink>()),
    preserveStaticLogger: true);

// Add services to the container..
builder.Services.AddControllersWithViews(options =>
{
    // Antiforgery em toda escrita (NFR-11): páginas pelo filtro do MVC, JSON pelo cabeçalho (ADR-003)
    options.Filters.Add(new AntiforgeryJsonFilter());
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddHttpsRedirection(options => options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect);
// Porta HTTPS: em Production o padrão é 443; em desenvolvimento, a do launchSettings (detecção automática)
builder.Services.AddOptions<HttpsRedirectionOptions>().Configure<IConfiguration, IHostEnvironment>((options, configuration, environment) =>
    options.HttpsPort = configuration.GetValue<int?>("HttpsRedirection:HttpsPort") ?? (environment.IsProduction() ? 443 : null));
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseAndMigrationHealthCheck>("banco", tags: ["ready"]);
builder.Services.AddRateLimiters();
builder.Services.AddPublicResponseCompression();
// Versão (?v=) dos scripts de página: hash da página e dos módulos que ela importa; em Development recalcula a cada pedido
builder.Services.AddSingleton<IModuleVersions>(services =>
{
    IWebHostEnvironment environment = services.GetRequiredService<IWebHostEnvironment>();
    return new ModuleVersions(environment.WebRootFileProvider, cache: !environment.IsDevelopment());
});
builder.Services.AddSecureForwarding();
// Nenhuma política CORS: site e endpoints JSON são da mesma origem (ARCHITECTURE.md §7)
builder.Services.AddAppOptions(builder.Configuration, builder.Environment.IsProduction());
// Chaves do Data Protection (cookie de login, antiforgery, links de redefinição de senha) numa pasta persistente fora da raiz do site (ADR-011, RC-16).
// Sem a pasta, na hospedagem compartilhada a reciclagem do pool pode gerar um anel novo e derrubar a sessão da equipe e os links já enviados. A pasta é obrigatória em Production (validada em AddAppOptions).
// O caminho é lido na hora de montar o anel (não aqui), porque a configuração do host de teste só existe depois que este arquivo rodou
builder.Services.AddDataProtection().SetApplicationName("GazetaMarketplace");
builder.Services.AddOptions<KeyManagementOptions>().Configure<IConfiguration, ILoggerFactory>((options, configuration, loggers) =>
{
    string keysDirectory = configuration[$"{KeyStorageOptions.SectionName}:{nameof(KeyStorageOptions.KeysDirectory)}"];
    if (!string.IsNullOrWhiteSpace(keysDirectory))
    {
        options.XmlRepository = new FileSystemXmlRepository(new DirectoryInfo(keysDirectory), loggers);
    }
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTeamIdentity();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddCore();
builder.Services.AddScoped<GazetaMarketplace.Web.Areas.Panel.Models.AdFormFactory>();
builder.Services.AddScoped<GazetaMarketplace.Web.Models.AdDetailFactory>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEmailSender(builder.Environment.IsProduction());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSecureForwarding(app.Configuration);
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>(app.Environment.IsProduction());
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    api => api.UseMiddleware<ExceptionHandlingMiddleware>());
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    pages => pages.UseExceptionHandler("/Home/Error"));
// Resposta de erro sem corpo (endereço que não existe, NotFound() do painel) vira uma página em português com o mesmo status; API, verificações de saúde e arquivos (.js, .php...) ficam como estão
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/health") && !Path.HasExtension(context.Request.Path.Value),
    pages => pages.UseStatusCodePagesWithReExecute("/Home/Status/{0}"));

app.UseHttpsRedirection();
app.UsePublicResponseCompression();
// Sem providers: ignora Accept-Language e cookies, a cultura é sempre pt-BR
RequestLocalizationOptions location = new()
{
    DefaultRequestCulture = new RequestCulture(ptBr),
    SupportedCultures = [ptBr],
    SupportedUICultures = [ptBr]
};
location.RequestCultureProviders.Clear();
app.UseRequestLocalization(location);
app.UseRouting();
app.UseVersionedStaticAssetCache();
app.UseMiddleware<BodyLimitMiddleware>();

app.UseAuthentication();
// Depois da autenticação: a política "cep" conta por usuário e precisa saber quem é. Os limites por IP (global e login) não dependem disso
app.UseRateLimiter();
app.UseAuthorization();

app.MapStaticAssets();

// Vivo = processo respondendo, sem tocar em nada; pronto = banco acessível e migration em dia (ADR-010)
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = record => record.Tags.Contains("ready") });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


try
{
    app.Run();
}
catch (Exception ex) when (ex is OptionsValidationException or InvalidBootstrapException || ex is AggregateException { InnerExceptions: [OptionsValidationException, ..] })
{
    // Configuração incompleta em Production: o site não sobe e o motivo fica no log (ADR-011)
    app.Logger.LogCritical(ex, "Configuração inválida; o site não foi iniciado.");
    throw;
}

// Necessário para o WebApplicationFactory dos testes
public partial class Program
{
}
