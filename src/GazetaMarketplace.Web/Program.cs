using System;
using GazetaMarketplace.Core;
using GazetaMarketplace.Infrastructure;
using GazetaMarketplace.Infrastructure.Configuracao;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container..
builder.Services.AddControllersWithViews();
builder.Services.AddOpcoes(builder.Configuration, builder.Environment.IsProduction());
builder.Services.AddCore();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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
