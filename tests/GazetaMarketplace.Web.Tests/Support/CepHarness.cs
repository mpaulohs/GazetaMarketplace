using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>ViaCEP de mentira: o teste define a resposta de cada CEP e conta as chamadas.</summary>
internal sealed class FakeCepLookup : ICepLookup
{
    private int _calls;

    public Func<string, CepLookupResult> Respond { get; set; } = _ => new CepLookupResult("Campinas", "SP", 3509502);

    /// <summary>Executada no meio de uma consulta (para simular outra pessoa gravando o mesmo CEP ao mesmo tempo).</summary>
    public Func<string, Task> Meanwhile { get; set; }

    public int Calls => Volatile.Read(ref _calls);

    public List<string> Asked { get; } = [];

    public async Task<CepLookupResult> LookupAsync(string cep, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        lock (Asked)
        {
            Asked.Add(cep);
        }

        if (Meanwhile is not null)
        {
            await Meanwhile(cep);
        }

        return Respond(cep);
    }

    public static CepLookupResult Unavailable() => throw new ServiceUnavailableException(CepRules.UnavailableMessage);
}

/// <summary>Site de teste (SQLite) com um Administrador, um Redator e o ViaCEP falso no lugar do real.</summary>
internal sealed class CepHarness : IDisposable
{
    private CepHarness(WebFactory factory, FakeCepLookup lookup, HttpClient writer, HttpClient admin)
    {
        Factory = factory;
        Lookup = lookup;
        Writer = writer;
        Admin = admin;
    }

    public WebFactory Factory { get; }

    public FakeCepLookup Lookup { get; }

    public HttpClient Writer { get; }

    public HttpClient Admin { get; }

    public static async Task<CepHarness> StartAsync(Action<IServiceCollection> extraServices = null, bool fakeLookup = true, Dictionary<string, string> configuration = null)
    {
        FakeCepLookup lookup = new();
        WebFactory factory = new(withDatabase: true, configuration: configuration, services: services =>
        {
            if (fakeLookup)
            {
                services.RemoveAll<ICepLookup>();
                services.AddSingleton<ICepLookup>(lookup);
            }

            extraServices?.Invoke(services);
        });
        await factory.CreateUserAsync(PanelFixture.AdminEmail, "Marcos Silva", PanelFixture.Password, RoleNames.Administrator);
        await factory.CreateUserAsync(PanelFixture.WriterEmail, "Ana Souza", PanelFixture.Password, RoleNames.Writer);
        return new CepHarness(
            factory,
            lookup,
            await PanelFixture.SignedInAsync(factory, PanelFixture.WriterEmail),
            await PanelFixture.SignedInAsync(factory, PanelFixture.AdminEmail));
    }

    public HttpClient Anonymous() => TeamClient.Create(Factory);

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> work)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        return await work(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public Task<List<CepCacheEntry>> CacheAsync() => WithDbAsync(db => db.CepCache.AsNoTracking().OrderBy(e => e.Cep).ToListAsync());

    public Task AddCitiesAsync(params City[] cities) => WithDbAsync(async db =>
    {
        db.Cities.AddRange(cities);
        await db.SaveChangesAsync();
        return 0;
    });

    public static async Task<JsonDocument> JsonAsync(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    public void Dispose()
    {
        Writer.Dispose();
        Admin.Dispose();
        Factory.Dispose();
    }
}
