using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Exceptions;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Core.Location;
using GazetaMarketplace.Core.VehicleCatalog;
using GazetaMarketplace.Infrastructure.Ads;
using GazetaMarketplace.Infrastructure.Categories;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Infrastructure.Location;
using GazetaMarketplace.Web.Tests.Catalog;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Serviço de CEP de mentira: o teste define a resposta e conta as consultas.</summary>
internal sealed class FakeCepService : ICepService
{
    public Func<string, CepResult> Respond { get; set; } = cep => new CepResult(cep, "Campinas", "SP", CepResult.FromViaCep);

    public List<string> Asked { get; } = [];

    public Task<CepResult> GetAsync(string cep, CancellationToken cancellationToken)
    {
        Asked.Add(cep);
        return Task.FromResult(Respond(cep));
    }

    public static CepResult NotFound() => throw new NotFoundException(CepRules.NotFoundMessage);

    public static CepResult Unavailable() => throw new ServiceUnavailableException(CepRules.UnavailableMessage);
}

/// <summary>
/// SQLite em memória com as categorias, o catálogo pequeno (<see cref="SmallCatalog"/>), as cidades de SP e o <see cref="AdDraftService"/> real.
/// Só o serviço de CEP é de mentira. Sem <c>JSON_VALUE</c> nem CHECKs (como no <see cref="AdDb"/>); o esquema real é provado na integração.
/// </summary>
internal sealed class DraftDb : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _services;
    private int _users;

    public DraftDb()
    {
        _connection.Open();
        Clock = new FakeClock();
        User = new FakeCurrentUser();
        Cep = new FakeCepService();

        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<ICurrentUser>(User);
        services.AddSingleton<ICepService>(Cep);
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        services.AddScoped<IAuditLog, AuditLog>();
        services.AddScoped<IAdService, AdService>();
        services.AddScoped<IAdDraftService, AdDraftService>();
        services.AddScoped<ICityDirectory, CityDirectory>();
        services.AddSingleton<CategoryTree>();
        services.AddSingleton<ICategoryTree>(provider => provider.GetRequiredService<CategoryTree>());
        services.AddSingleton<GazetaMarketplace.Infrastructure.VehicleCatalog.VehicleCatalog>();
        services.AddSingleton<IVehicleCatalog>(provider => provider.GetRequiredService<GazetaMarketplace.Infrastructure.VehicleCatalog.VehicleCatalog>());
        _services = services.BuildServiceProvider();

        using IServiceScope scope = _services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
        SmallCatalog.Seed(context);
        context.Cities.AddRange(
            new City { IbgeCode = 3509502, Name = "Campinas", Uf = "SP", NameSearch = "campinas" },
            new City { IbgeCode = 3550308, Name = "São Paulo", Uf = "SP", NameSearch = "sao paulo" });
        context.SaveChanges();
    }

    public FakeClock Clock { get; }

    public FakeCurrentUser User { get; }

    public FakeCepService Cep { get; }

    public Task<T> WithScopeAsync<T>(Func<IServiceProvider, Task<T>> work) => Run(work);

    public Task<T> WithContextAsync<T>(Func<AppDbContext, Task<T>> work) => Run(provider => work(provider.GetRequiredService<AppDbContext>()));

    public Task<AdDraftResult> CreateAsync(AdDraftInput input) => Run(provider => provider.GetRequiredService<IAdDraftService>().CreateAsync(input, CancellationToken.None));

    public Task<AdDraftResult> UpdateAsync(int id, byte[] rowVersion, AdDraftInput input) =>
        Run(provider => provider.GetRequiredService<IAdDraftService>().UpdateAsync(id, rowVersion, input, CancellationToken.None));

    /// <summary>Cria um usuário (o anúncio tem chave estrangeira para o autor), faz dele o usuário atual e devolve o id.</summary>
    public async Task<int> SignInAsync(bool administrator = false)
    {
        int n = ++_users;
        int id = await WithContextAsync(async context =>
        {
            AppUser user = new() { UserName = $"user{n}@exemplo.com.br", NormalizedUserName = $"USER{n}@EXEMPLO.COM.BR", Email = $"user{n}@exemplo.com.br", FullName = $"Pessoa {n}" };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user.Id;
        });
        User.UserId = id;
        User.IsAdministrator = administrator;
        return id;
    }

    public Task<Ad> LoadAdAsync(int id) => WithContextAsync(context => context.Ads.AsNoTracking().SingleAsync(a => a.Id == id));

    public Task<List<AuditEntry>> AuditAsync() => WithContextAsync(context => context.AuditEntries.AsNoTracking().OrderBy(e => e.Id).ToListAsync());

    public Task<int> CountAdsAsync() => WithContextAsync(context => context.Ads.CountAsync());

    private async Task<T> Run<T>(Func<IServiceProvider, Task<T>> work)
    {
        using IServiceScope scope = _services.CreateScope();
        return await work(scope.ServiceProvider);
    }

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
    }

    /// <summary>Um formulário com só o título e as demais chaves em branco.</summary>
    public static AdDraftInput Input(
        string title = "Honda Civic 2018",
        int? categoryId = null,
        string description = null,
        string price = null,
        string cep = null,
        string city = null,
        string uf = null,
        string locationCep = null,
        bool manual = false,
        IReadOnlyDictionary<string, string[]> fields = null) =>
        new(title, description, categoryId, price, cep, city, uf, locationCep, manual, fields ?? new Dictionary<string, string[]>());

    public static Dictionary<string, string[]> Fields(params (string Key, string Value)[] values)
    {
        Dictionary<string, string[]> result = [];
        foreach ((string key, string value) in values)
        {
            result[key] = [value];
        }

        return result;
    }
}
