using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Equipe;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identidade;
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

namespace GazetaMarketplace.Web.Tests.Suporte;

/// <summary>Host de teste em memória (Template A de rules/testing.md): sem banco, sem rede.</summary>
internal sealed class FabricaWeb : WebApplicationFactory<Program>
{
    public const string CabecalhoIpRemoto = "X-Test-Remote-IP";
    public const string CabecalhoPapel = "X-Test-Papel";

    private readonly Action<IServiceCollection> _servicos;
    private readonly string _ambiente;
    private readonly Dictionary<string, string> _configuracao;
    private readonly bool _comBanco;
    private readonly SqliteConnection _conexao;
    private readonly string _pastaDeLogs = Path.Combine(Path.GetTempPath(), "gazeta-web-" + Guid.NewGuid().ToString("N"));

    // comBanco liga um SQLite em memória no lugar do SQL Server, com os papéis semeados, e um relógio controlável (Relogio)
    public FabricaWeb(string ambiente = "Testing", Dictionary<string, string> configuracao = null, Action<IServiceCollection> servicos = null, bool comBanco = false, Action<AppDbContext> semear = null)
    {
        _comBanco = comBanco;
        if (comBanco)
        {
            _conexao = new SqliteConnection("DataSource=:memory:");
            _conexao.Open();

            // O esquema precisa existir antes de o host subir: rotinas de partida (o Administrador inicial) já usam o banco
            using AppDbContext contexto = new(
                new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexao).Options, new UsuarioFalso(), Relogio);
            contexto.Database.EnsureCreated();
            semear?.Invoke(contexto);
        }

        _servicos = servicos;
        _ambiente = ambiente;
        _configuracao = configuracao ?? [];
        // O TestServer atende em HTTP por padrão; o site exige HTTPS
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    public ColetorSink Logs { get; } = new();

    /// <summary>Relógio do site quando <c>comBanco</c>: avançar o tempo envelhece sessão, bloqueio e cookie.</summary>
    public RelogioFalso Relogio { get; } = new();

    /// <summary>Configuração completa que o site exige em Production (ADR-011).</summary>
    public static Dictionary<string, string> ConfiguracaoDeProducao() => new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Server=(local);Database=Teste;Integrated Security=true",
        ["PhotoStorage:BasePath"] = "/dados/fotos",
        ["DataProtection:KeysDirectory"] = "/dados/chaves",
        ["SendGrid:ApiKey"] = "chave-de-teste",
        ["SendGrid:FromEmail"] = "noreply@exemplo.com.br"
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_ambiente);
        builder.ConfigureAppConfiguration((_, configuracao) =>
        {
            configuracao.AddInMemoryCollection(_configuracao);
            configuracao.AddInMemoryCollection(new Dictionary<string, string> { ["Logging:FileDirectory"] = _pastaDeLogs });
        });
        builder.ConfigureTestServices(services =>
        {
            services.AddControllersWithViews().AddApplicationPart(typeof(ApiTesteController).Assembly);
            services.AddSingleton<ILogEventSink>(Logs);
            services.AddSingleton<IStartupFilter, IpRemotoDeTeste>();
            services.AddSingleton<IStartupFilter, PapelDeTeste>();
            if (_comBanco)
            {
                // EF Core 9+ acumula as configurações do provedor: tirar só as opções deixaria o SQL Server junto
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(opcoes => opcoes.UseSqlite(_conexao));
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Relogio);
            }

            _servicos?.Invoke(services);
        });
    }

    /// <summary>Insere uma conta direto no banco, para ser usada em <c>semear</c> (antes de o host subir).</summary>
    public static void SemearUsuario(AppDbContext contexto, string email, int papelId, bool ativo = true)
    {
        UsuarioIdentity usuario = new()
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            FullName = "Semente",
            IsActive = ativo,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        usuario.PasswordHash = new PasswordHasher<UsuarioIdentity>().HashPassword(usuario, "Senha@Forte1");
        contexto.Users.Add(usuario);
        contexto.SaveChanges();
        contexto.UserRoles.Add(new IdentityUserRole<int> { UserId = usuario.Id, RoleId = papelId });
        contexto.SaveChanges();
    }

    public async Task<IReadOnlyList<UsuarioIdentity>> ListarUsuariosAsync()
    {
        using IServiceScope escopo = Services.CreateScope();
        return await escopo.ServiceProvider.GetRequiredService<AppDbContext>().Users.AsNoTracking().ToListAsync();
    }

    /// <summary>Cria uma conta da equipe direto no banco de testes (a tela de usuários só chega na 1.3).</summary>
    public async Task<UsuarioIdentity> CriarUsuarioAsync(
        string email, string nome, string senha, string papel, bool ativo = true, bool trocarSenha = false)
    {
        using IServiceScope escopo = Services.CreateScope();
        UserManager<UsuarioIdentity> usuarios = escopo.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        UsuarioIdentity usuario = new() { UserName = email, Email = email, FullName = nome, IsActive = ativo, MustChangePassword = trocarSenha };

        IdentityResult criado = await usuarios.CreateAsync(usuario, senha);
        if (!criado.Succeeded)
        {
            throw new InvalidOperationException("Usuário de teste não criado: " + string.Join("; ", criado.Errors.Select(e => e.Code)));
        }

        await usuarios.AddToRoleAsync(usuario, papel);
        return usuario;
    }

    public async Task AlterarUsuarioAsync(string email, Action<UsuarioIdentity> alterar)
    {
        using IServiceScope escopo = Services.CreateScope();
        UserManager<UsuarioIdentity> usuarios = escopo.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        UsuarioIdentity usuario = await usuarios.FindByEmailAsync(email);
        alterar(usuario);
        await usuarios.UpdateAsync(usuario);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _conexao?.Dispose();
        }

        if (disposing && Directory.Exists(_pastaDeLogs))
        {
            try
            {
                Directory.Delete(_pastaDeLogs, recursive: true);
            }
            catch (IOException)
            {
                // arquivo ainda preso pelo Serilog: o diretório temporário é descartável
            }
        }
    }

    /// <summary>O TestServer não tem IP de origem; este filtro o simula a partir de um cabeçalho de teste.</summary>
    private sealed class IpRemotoDeTeste : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((HttpContext contexto, Func<Task> proximo) =>
            {
                if (contexto.Request.Headers.TryGetValue(CabecalhoIpRemoto, out Microsoft.Extensions.Primitives.StringValues valor))
                {
                    contexto.Connection.RemoteIpAddress = IPAddress.Parse(valor.ToString());
                }

                return proximo();
            });
            next(app);
        };
    }

    /// <summary>Simula uma pessoa da equipe autenticada a partir de um cabeçalho de teste (o Identity só chega na tarefa 1.1).</summary>
    private sealed class PapelDeTeste : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((HttpContext contexto, Func<Task> proximo) =>
            {
                if (contexto.Request.Headers.TryGetValue(CabecalhoPapel, out Microsoft.Extensions.Primitives.StringValues papel))
                {
                    ClaimsIdentity identidade = new(
                        [new Claim(ClaimTypes.Name, "Ana Souza"), new Claim(ClaimTypes.Role, papel.ToString())],
                        "Teste");
                    contexto.User = new ClaimsPrincipal(identidade);
                }

                return proximo();
            });
            next(app);
        };
    }
}
