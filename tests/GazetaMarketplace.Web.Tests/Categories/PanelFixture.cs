using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Categories;
using GazetaMarketplace.Core.Entities;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GazetaMarketplace.Web.Tests.Categories;

/// <summary>Quantos anúncios cada categoria tem, definido pelo teste. Só para as telas que mostram a contagem; as regras de exclusão rodam contra anúncios reais (<c>realAds</c>).</summary>
internal sealed class FakeCategoryUsage : ICategoryUsage
{
    public Dictionary<int, int> Counts { get; } = [];

    public Task<IReadOnlyDictionary<int, int>> CountAdsAsync(IReadOnlyCollection<int> categoryIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<int, int>>(categoryIds.Distinct().ToDictionary(id => id, id => Counts.GetValueOrDefault(id)));
}

/// <summary>Site de teste com um Administrador e um Redator, o cliente do Administrador já logado e atalhos para a tela de categorias.</summary>
internal sealed class PanelFixture : System.IDisposable
{
    public const string AdminEmail = "marcos@exemplo.com.br";
    public const string WriterEmail = "ana.souza@exemplo.com.br";
    public const string Password = "Senha@Forte1";
    public const string Page = "/painel/categorias";

    private PanelFixture(WebFactory factory, FakeCategoryUsage usage, HttpClient admin)
    {
        Factory = factory;
        Usage = usage;
        Admin = admin;
    }

    public WebFactory Factory { get; }

    public FakeCategoryUsage Usage { get; }

    public HttpClient Admin { get; }

    /// <param name="realAds">Usa a contagem real de anúncios (<c>AdsCategoryUsage</c>) em vez da contagem definida pelo teste.</param>
    public static async Task<PanelFixture> StartAsync(bool realAds = false)
    {
        FakeCategoryUsage usage = new();
        WebFactory factory = new(withDatabase: true, services: services =>
        {
            if (!realAds)
            {
                services.RemoveAll<ICategoryUsage>();
                services.AddSingleton<ICategoryUsage>(usage);
            }
        });
        await factory.CreateUserAsync(AdminEmail, "Marcos Silva", Password, RoleNames.Administrator);
        await factory.CreateUserAsync(WriterEmail, "Ana Souza", Password, RoleNames.Writer);
        HttpClient admin = await SignedInAsync(factory, AdminEmail);
        return new PanelFixture(factory, usage, admin);
    }

    public static async Task<HttpClient> SignedInAsync(WebFactory factory, string email)
    {
        HttpClient client = TeamClient.Create(factory);
        HttpResponseMessage entry = await client.SignInAsync(email, Password);
        Assert.AreEqual(HttpStatusCode.Redirect, entry.StatusCode, "a conta de teste precisa entrar");
        return client;
    }

    public Task<HttpResponseMessage> CreateAsync(string name, int? parentId, HttpClient client = null) =>
        (client ?? Admin).PostFormAsync($"{Page}/nova", $"{Page}/nova", new Dictionary<string, string> { ["Name"] = name, ["ParentId"] = parentId?.ToString() ?? string.Empty });

    public Task<HttpResponseMessage> RenameAsync(int id, string name) =>
        Admin.PostFormAsync(Page, $"{Page}/{id}/editar", new Dictionary<string, string> { ["Name"] = name });

    public Task<HttpResponseMessage> MoveAsync(int id, string direction) =>
        Admin.PostFormAsync(Page, $"{Page}/{id}/mover/{direction}");

    public Task<HttpResponseMessage> DeleteAsync(int id) =>
        Admin.PostFormAsync(Page, $"{Page}/{id}/excluir");

    /// <summary>A lista de categorias como o Administrador a vê (já decodificada).</summary>
    public async Task<string> IndexAsync() => await (await Admin.GetAsync(Page)).TextAsync();

    /// <summary>Segue o redirecionamento de uma ação e devolve a página de destino.</summary>
    public async Task<string> FollowAsync(HttpResponseMessage response)
    {
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        return await (await Admin.GetAsync(response.Destination())).TextAsync();
    }

    public async Task<T> WithDbAsync<T>(System.Func<AppDbContext, Task<T>> work)
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        return await work(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>Grava um anúncio de verdade na categoria, já na situação pedida, escrito pela Ana (Redator).</summary>
    public async Task<int> AddAdAsync(int categoryId, byte status = AdStatus.Draft)
    {
        return await WithDbAsync(async db =>
        {
            int author = await db.Users.Where(u => u.Email == WriterEmail).Select(u => u.Id).SingleAsync();
            int decider = await db.Users.Where(u => u.Email == AdminEmail).Select(u => u.Id).SingleAsync();
            Ad ad = AdFactory.At(status, author, "Anúncio de teste", categoryId, decider);
            db.Ads.Add(ad);
            await db.SaveChangesAsync();
            return ad.Id;
        });
    }

    public Task<Category> FindAsync(int id) => WithDbAsync(db => db.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id));

    public Task<Category> FindByNameAsync(string name) => WithDbAsync(db => db.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Name == name));

    public Task<int> CountAsync() => WithDbAsync(db => db.Categories.CountAsync());

    public Task<List<AuditEntry>> AuditAsync() =>
        WithDbAsync(db => db.AuditEntries.AsNoTracking().Where(e => e.TargetType == "Category").OrderBy(e => e.Id).ToListAsync());

    public async Task<CategoryTreeSnapshot> TreeAsync()
    {
        using IServiceScope scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ICategoryTree>().GetAsync(CancellationToken.None);
    }

    public void Dispose()
    {
        Admin.Dispose();
        Factory.Dispose();
    }
}
