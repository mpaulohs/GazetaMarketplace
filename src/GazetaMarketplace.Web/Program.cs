using System;
using System.Globalization;
using System.Linq;
using GazetaMarketplace.Core;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure;
using GazetaMarketplace.Infrastructure.Configuration;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Infrastructure.Logging;
using GazetaMarketplace.Web.Filters;
using GazetaMarketplace.Web.HealthChecks;
using GazetaMarketplace.Web.Middleware;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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
builder.Services.AddSecureForwarding();
// Nenhuma política CORS: site e endpoints JSON são da mesma origem (ARCHITECTURE.md §7)
builder.Services.AddAppOptions(builder.Configuration, builder.Environment.IsProduction());
builder.Services.AddHttpContextAccessor();
builder.Services.AddTeamIdentity();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddCore();
builder.Services.AddInfrastructure(builder.Configuration);

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

app.UseHttpsRedirection();
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
app.UseMiddleware<BodyLimitMiddleware>();
app.UseRateLimiter();

app.UseAuthentication();
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
