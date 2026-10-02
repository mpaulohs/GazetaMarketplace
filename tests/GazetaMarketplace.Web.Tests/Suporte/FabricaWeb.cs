using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly string _pastaDeLogs = Path.Combine(Path.GetTempPath(), "gazeta-web-" + Guid.NewGuid().ToString("N"));

    public FabricaWeb(string ambiente = "Testing", Dictionary<string, string> configuracao = null, Action<IServiceCollection> servicos = null)
    {
        _servicos = servicos;
        _ambiente = ambiente;
        _configuracao = configuracao ?? [];
        // O TestServer atende em HTTP por padrão; o site exige HTTPS
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    public ColetorSink Logs { get; } = new();

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
            _servicos?.Invoke(services);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
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
