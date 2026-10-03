using System;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Interfaces;
using GazetaMarketplace.Infrastructure.Ads;
using GazetaMarketplace.Infrastructure.Categories;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>
/// SQLite em memória com a carga de categorias, o <see cref="AdService"/> e o <see cref="AdsCategoryUsage"/> reais. O SQLite não tem
/// <c>JSON_VALUE</c> nem <c>ISJSON</c>: as colunas calculadas e os CHECKs saem do modelo (BACKLOG); o esquema real é provado na integração.
/// </summary>
internal sealed class AdDb : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _services;
    private int _users;

    public AdDb()
    {
        _connection.Open();
        Clock = new FakeClock();
        User = new FakeCurrentUser();

        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<ICurrentUser>(User);
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        services.AddScoped<IAuditLog, AuditLog>();
        services.AddScoped<IAdService, AdService>();
        services.AddScoped<ICategoryUsage, AdsCategoryUsage>();
        _services = services.BuildServiceProvider();

        using IServiceScope scope = _services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    public FakeClock Clock { get; }

    /// <summary>Quem está agindo; os testes trocam o id e o papel antes de cada chamada.</summary>
    public FakeCurrentUser User { get; }

    public async Task<T> WithScopeAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        using IServiceScope scope = _services.CreateScope();
        return await work(scope.ServiceProvider);
    }

    public Task WithScopeAsync(Func<IServiceProvider, Task> work) => WithScopeAsync<int>(async provider =>
    {
        await work(provider);
        return 0;
    });

    public Task<T> WithContextAsync<T>(Func<AppDbContext, Task<T>> work) => WithScopeAsync(provider => work(provider.GetRequiredService<AppDbContext>()));

    public Task<T> WithServiceAsync<T>(Func<IAdService, Task<T>> work) => WithScopeAsync(provider => work(provider.GetRequiredService<IAdService>()));

    /// <summary>Cria um usuário (o anúncio tem chave estrangeira para o autor) e devolve o id.</summary>
    public async Task<int> AddUserAsync()
    {
        int n = ++_users;
        return await WithContextAsync(async context =>
        {
            AppUser user = new() { UserName = $"user{n}@exemplo.com.br", NormalizedUserName = $"USER{n}@EXEMPLO.COM.BR", Email = $"user{n}@exemplo.com.br", FullName = $"Pessoa {n}" };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user.Id;
        });
    }

    /// <summary>Grava um anúncio já na situação pedida, percorrendo um caminho válido do Apêndice A.</summary>
    public async Task<int> AddAdAsync(int authorId, byte status = AdStatus.Draft, string title = "Honda Civic 2018", int? categoryId = null)
    {
        // Quem decidiu (publicou ou rejeitou) é uma conta de verdade: a chave estrangeira existe também no SQLite
        int decider = status is AdStatus.Published or AdStatus.Rejected ? await AddUserAsync() : 99;
        Ad ad = AdFactory.At(status, authorId, title, categoryId, decider);
        await WithContextAsync(async context =>
        {
            context.Ads.Add(ad);
            await context.SaveChangesAsync();
            return 0;
        });
        return ad.Id;
    }

    public Task<Ad> LoadAdAsync(int id) => WithContextAsync(context => context.Ads.AsNoTracking().SingleAsync(a => a.Id == id));

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
    }
}

/// <summary>Monta anúncios em qualquer situação sem passar pelo serviço (para testar as regras sobre um estado de partida).</summary>
internal static class AdFactory
{
    public static Ad At(byte status, int authorId = 1, string title = "Honda Civic 2018", int? categoryId = null, int deciderId = 99)
    {
        Ad ad = Ad.CreateDraft(title, authorId);
        ad.SetCategory(categoryId);
        DateTime now = new(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);
        switch (status)
        {
            case AdStatus.Draft:
                break;
            case AdStatus.InReview:
                ad.ApplyTransition(AdStatus.InReview, authorId, now, null);
                break;
            case AdStatus.Published:
                ad.ApplyTransition(AdStatus.InReview, authorId, now, null);
                ad.ApplyTransition(AdStatus.Published, deciderId, now, null);
                break;
            case AdStatus.Rejected:
                ad.ApplyTransition(AdStatus.InReview, authorId, now, null);
                ad.ApplyTransition(AdStatus.Rejected, deciderId, now, "Fotos escuras");
                break;
            case AdStatus.Archived:
                ad.ApplyTransition(AdStatus.Archived, deciderId, now, null);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        return ad;
    }
}
