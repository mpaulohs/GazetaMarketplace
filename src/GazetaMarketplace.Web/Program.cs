using System;
using GazetaMarketplace.Core;
using GazetaMarketplace.Infrastructure;
using GazetaMarketplace.Infrastructure.Configuracao;
using GazetaMarketplace.Infrastructure.Logging;
using GazetaMarketplace.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// preserveStaticLogger: o logger vive só no host (testes em paralelo não disputam o Log.Logger)
builder.Host.UseSerilog(
    (contexto, servicos, configuracao) => SerilogConfiguration.Configurar(
        configuracao,
        contexto.Configuration["Logging:FileDirectory"],
        contexto.HostingEnvironment.IsProduction(),
        servicos.GetServices<ILogEventSink>()),
    preserveStaticLogger: true);

// Add services to the container..
builder.Services.AddControllersWithViews();
builder.Services.AddOpcoes(builder.Configuration, builder.Environment.IsProduction());
builder.Services.AddCore();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseWhen(
    contexto => contexto.Request.Path.StartsWithSegments("/api"),
    api => api.UseMiddleware<ExceptionHandlingMiddleware>());
app.UseWhen(
    contexto => !contexto.Request.Path.StartsWithSegments("/api"),
    paginas => paginas.UseExceptionHandler("/Home/Error"));
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

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
