using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Ads;
using GazetaMarketplace.Core.Search;
using GazetaMarketplace.Core.Seo;
using GazetaMarketplace.Core.Showcase;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog.Core;

namespace GazetaMarketplace.Web.Tests.Support;

/// <summary>Host de teste em memória (Template A de rules/testing.md): sem banco, sem rede.</summary>
internal sealed class WebFactory : WebApplicationFactory<Program>
{
    public const string RemoteIpHeader = "X-Test-Remote-IP";
    public const string RoleHeader = "X-Test-Papel";

    private readonly Action<IServiceCollection> _services;
    private readonly string _environment;
    private readonly Dictionary<string, string> _configuration;
    private readonly bool _withDatabase;
    private readonly SqliteConnection _connection;
    private readonly string _logsFolder = Path.Combine(Path.GetTempPath(), "gazeta-web-" + Guid.NewGuid().ToString("N"));

    // comBanco liga um SQLite em memória no lugar do SQL Server, com os papéis semeados, e um relógio controlável (Relogio)
    public WebFactory(string environment = "Testing", Dictionary<string, string> configuration = null, Action<IServiceCollection> services = null, bool withDatabase = false, Action<AppDbContext> seed = null)
    {
        _withDatabase = withDatabase;
        if (withDatabase)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // O esquema precisa existir antes de o host subir: rotinas de partida (o Administrador inicial) já usam o banco
            using AppDbContext context = new(
                new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new FakeCurrentUser(), Clock);
            context.Database.EnsureCreated();
            seed?.Invoke(context);
        }

        _services = services;
        _environment = environment;
        _configuration = configuration ?? [];
        // O TestServer atende em HTTP por padrão; o site exige HTTPS
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    public CollectorSink Logs { get; } = new();

    /// <summary>Relógio do site quando <c>comBanco</c>: avançar o tempo envelhece sessão, bloqueio e cookie.</summary>
    public FakeClock Clock { get; } = new();

    /// <summary>Configuração completa que o site exige em Production (ADR-011). A pasta de chaves é uma pasta temporária nova por chamada: o site grava nela as chaves do Data Protection.</summary>
    public static Dictionary<string, string> ProductionConfiguration() => new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Server=(local);Database=Teste;Integrated Security=true",
        ["PhotoStorage:BasePath"] = "/dados/fotos",
        ["DataProtection:KeysDirectory"] = Path.Combine(Path.GetTempPath(), "gazeta-chaves-" + Guid.NewGuid().ToString("N")),
        ["SendGrid:ApiKey"] = "chave-de-teste",
        ["SendGrid:FromEmail"] = "noreply@exemplo.com.br",
        ["Site:BaseUrl"] = "https://gazeta.exemplo.com.br"
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            // O limite de pedidos de login/recuperação (5 por 15 min, por IP) vale em produção; os testes entram várias vezes do mesmo IP, então o padrão do host de teste é alto.
            // Quem prova o limite (AuthRateLimitTests, RateLimiterTests) passa o valor na configuração
            configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["RateLimiting:AuthPermits"] = "1000",

                // Em produção o limite de falhas por origem é 5 com proxies conhecidos e 20 sem eles; os testes de entrada contam com 5
                ["RateLimiting:LoginFailuresPerOrigin"] = "5"
            });
            configuration.AddInMemoryCollection(_configuration);
            configuration.AddInMemoryCollection(new Dictionary<string, string> { ["Logging:FileDirectory"] = _logsFolder });
        });
        builder.ConfigureTestServices(services =>
        {
            services.AddControllersWithViews().AddApplicationPart(typeof(TestApiController).Assembly);
            services.AddSingleton<ILogEventSink>(Logs);
            services.AddSingleton<IStartupFilter, TestRemoteIp>();
            services.AddSingleton<IStartupFilter, TestRole>();
            if (_withDatabase)
            {
                // EF Core 9+ acumula as configurações do provedor: tirar só as opções deixaria o SQL Server junto
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Clock);

                // A lista do painel lê com Dapper em T-SQL (só SQL Server); no SQLite dos testes entra o repositório de mentira (a consulta real é provada na integração)
                services.RemoveAll<IPanelAdListReadRepository>();
                services.AddSingleton<StubPanelAdListRepository>();
                services.AddSingleton<IPanelAdListReadRepository>(sp => sp.GetRequiredService<StubPanelAdListRepository>());

                // O mesmo para a vitrine pública (página inicial e categorias)
                services.RemoveAll<IShowcaseReadRepository>();
                services.AddSingleton<StubShowcaseRepository>();
                services.AddSingleton<IShowcaseReadRepository>(sp => sp.GetRequiredService<StubShowcaseRepository>());

                // E para o mapa do site (NFR-21)
                services.RemoveAll<ISitemapReadRepository>();
                services.AddSingleton<StubSitemapReadRepository>();
                services.AddSingleton<ISitemapReadRepository>(sp => sp.GetRequiredService<StubSitemapReadRepository>());

                // E para a busca (US-002)
                services.RemoveAll<ISearchReadRepository>();
                services.AddSingleton<StubSearchReadRepository>();
                services.AddSingleton<ISearchReadRepository>(sp => sp.GetRequiredService<StubSearchReadRepository>());
            }

            _services?.Invoke(services);
        });
    }

    /// <summary>Insere uma conta direto no banco, para ser usada em <c>semear</c> (antes de o host subir).</summary>
    public static void SeedUser(AppDbContext context, string email, int roleId, bool active = true)
    {
        AppUser user = new()
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            FullName = "Semente",
            IsActive = active,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, "Senha@Forte1");
        context.Users.Add(user);
        context.SaveChanges();
        context.UserRoles.Add(new IdentityUserRole<int> { UserId = user.Id, RoleId = roleId });
        context.SaveChanges();
    }

    public async Task<IReadOnlyList<AppUser>> ListUsersAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.AsNoTracking().ToListAsync();
    }

    /// <summary>Cria uma conta da equipe direto no banco de testes (a tela de usuários só chega na 1.3).</summary>
    public async Task<AppUser> CreateUserAsync(
        string email, string name, string password, string role, bool active = true, bool mustChangePassword = false)
    {
        using IServiceScope scope = Services.CreateScope();
        UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        AppUser user = new() { UserName = email, Email = email, FullName = name, IsActive = active, MustChangePassword = mustChangePassword };

        IdentityResult created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException("Usuário de teste não criado: " + string.Join("; ", created.Errors.Select(e => e.Code)));
        }

        await users.AddToRoleAsync(user, role);
        return user;
    }

    public async Task UpdateUserAsync(string email, Action<AppUser> change)
    {
        using IServiceScope scope = Services.CreateScope();
        UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        AppUser user = await users.FindByEmailAsync(email);
        change(user);
        await users.UpdateAsync(user);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }

        if (disposing && Directory.Exists(_logsFolder))
        {
            try
            {
                Directory.Delete(_logsFolder, recursive: true);
            }
            catch (IOException)
            {
                // arquivo ainda preso pelo Serilog: o diretório temporário é descartável
            }
        }
    }

    /// <summary>O TestServer não tem IP de origem; este filtro o simula a partir de um cabeçalho de teste.</summary>
    private sealed class TestRemoteIp : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((HttpContext context, Func<Task> next) =>
            {
                if (context.Request.Headers.TryGetValue(RemoteIpHeader, out Microsoft.Extensions.Primitives.StringValues value))
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(value.ToString());
                }

                return next();
            });
            next(app);
        };
    }

    /// <summary>Simula uma pessoa da equipe autenticada a partir de um cabeçalho de teste (o Identity só chega na tarefa 1.1).</summary>
    private sealed class TestRole : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((HttpContext context, Func<Task> next) =>
            {
                if (context.Request.Headers.TryGetValue(RoleHeader, out Microsoft.Extensions.Primitives.StringValues role))
                {
                    ClaimsIdentity identity = new(
                        [new Claim(ClaimTypes.Name, "Ana Souza"), new Claim(ClaimTypes.Role, role.ToString())],
                        "Teste");
                    context.User = new ClaimsPrincipal(identity);
                }

                return next();
            });
            next(app);
        };
    }
}
