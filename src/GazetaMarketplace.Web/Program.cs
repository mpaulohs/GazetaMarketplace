using System;
using GazetaMarketplace.Core;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure;
using GazetaMarketplace.Infrastructure.Configuracao;
using GazetaMarketplace.Infrastructure.Logging;
using GazetaMarketplace.Web.Filters;
using GazetaMarketplace.Web.Middleware;
using GazetaMarketplace.Web.Seguranca;
using System.Globalization;
using GazetaMarketplace.Web.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Cultura fixa pt-BR (NFR-20): R$, vírgula decimal e dd/mm/aaaa em qualquer servidor
CultureInfo ptBr = new("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBr;
CultureInfo.DefaultThreadCurrentUICulture = ptBr;

// preserveStaticLogger: o logger vive só no host (testes em paralelo não disputam o Log.Logger)
builder.Host.UseSerilog(
    (contexto, servicos, configuracao) => SerilogConfiguration.Configurar(
        configuracao,
        contexto.Configuration["Logging:FileDirectory"],
        contexto.HostingEnvironment.IsProduction(),
        servicos.GetServices<ILogEventSink>()),
    preserveStaticLogger: true);

// Add services to the container..
builder.Services.AddControllersWithViews(opcoes =>
{
    // Antiforgery em toda escrita (NFR-11): páginas pelo filtro do MVC, JSON pelo cabeçalho (ADR-003)
    opcoes.Filters.Add(new AntiforgeryJsonFilter());
    opcoes.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAntiforgery(opcoes =>
{
    opcoes.HeaderName = "RequestVerificationToken";
    opcoes.Cookie.HttpOnly = true;
    opcoes.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opcoes.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddHttpsRedirection(opcoes => opcoes.RedirectStatusCode = StatusCodes.Status308PermanentRedirect);
// Porta HTTPS: em Production o padrão é 443; em desenvolvimento, a do launchSettings (detecção automática)
builder.Services.AddOptions<HttpsRedirectionOptions>().Configure<IConfiguration, IHostEnvironment>((opcoes, configuracao, ambiente) =>
    opcoes.HttpsPort = configuracao.GetValue<int?>("HttpsRedirection:HttpsPort") ?? (ambiente.IsProduction() ? 443 : null));
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseAndMigrationHealthCheck>("banco", tags: ["ready"]);
builder.Services.AddLimitadores();
builder.Services.AddEncaminhamentoSeguro();
// Nenhuma política CORS: site e endpoints JSON são da mesma origem (ARCHITECTURE.md §7)
builder.Services.AddOpcoes(builder.Configuration, builder.Environment.IsProduction());
builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentidade();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtualHttp>();
builder.Services.AddCore();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseEncaminhamentoSeguro(app.Configuration);
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>(app.Environment.IsProduction());
app.UseWhen(
    contexto => contexto.Request.Path.StartsWithSegments("/api"),
    api => api.UseMiddleware<ExceptionHandlingMiddleware>());
app.UseWhen(
    contexto => !contexto.Request.Path.StartsWithSegments("/api"),
    paginas => paginas.UseExceptionHandler("/Home/Error"));

app.UseHttpsRedirection();
// Sem providers: ignora Accept-Language e cookies, a cultura é sempre pt-BR
RequestLocalizationOptions localizacao = new()
{
    DefaultRequestCulture = new RequestCulture(ptBr),
    SupportedCultures = [ptBr],
    SupportedUICultures = [ptBr]
};
localizacao.RequestCultureProviders.Clear();
app.UseRequestLocalization(localizacao);
app.UseRouting();
app.UseMiddleware<LimiteCorpoMiddleware>();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Vivo = processo respondendo, sem tocar em nada; pronto = banco acessível e migration em dia (ADR-010)
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = registro => registro.Tags.Contains("ready") });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


try
{
    app.Run();
}
catch (Exception ex) when (ex is OptionsValidationException || ex is AggregateException { InnerExceptions: [OptionsValidationException, ..] })
{
    // Configuração incompleta em Production: o site não sobe e o motivo fica no log (ADR-011)
    app.Logger.LogCritical(ex, "Configuração inválida; o site não foi iniciado.");
    throw;
}

// Necessário para o WebApplicationFactory dos testes
public partial class Program
{
}
