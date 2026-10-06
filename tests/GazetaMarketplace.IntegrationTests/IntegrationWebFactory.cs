using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GazetaMarketplace.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GazetaMarketplace.IntegrationTests;

/// <summary>O site inteiro (Program.cs) apontado para um banco do contêiner, em HTTPS, com cookies e sem seguir redirecionamentos.</summary>
internal sealed class IntegrationWebFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly Action<IServiceCollection> _services;
    private readonly IReadOnlyDictionary<string, string> _extraConfiguration;
    private readonly string _logsFolder = Path.Combine(Path.GetTempPath(), "gazeta-int-" + Guid.NewGuid().ToString("N"));

    public IntegrationWebFactory(string connectionString, Action<IServiceCollection> services = null, IReadOnlyDictionary<string, string> extraConfiguration = null)
    {
        _connectionString = connectionString;
        _services = services;
        _extraConfiguration = extraConfiguration ?? new Dictionary<string, string>();
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["ConnectionStrings:DefaultConnection"] = _connectionString,
            ["Logging:FileDirectory"] = _logsFolder,
            // O limite de login (5 por 15 min por IP) vale em produção; os fluxos daqui entram várias vezes do mesmo IP
            ["RateLimiting:AuthPermits"] = "1000"
        }));
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_extraConfiguration));
        builder.ConfigureTestServices(services => _services?.Invoke(services));
    }

    public HttpClient CreateBrowser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    public async Task<AppUser> CreateUserAsync(string email, string name, string password, string role, bool mustChangePassword = false)
    {
        using IServiceScope scope = Services.CreateScope();
        UserManager<AppUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        AppUser user = new() { UserName = email, Email = email, FullName = name, IsActive = true, MustChangePassword = mustChangePassword };
        IdentityResult created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException("Usuário de teste não criado: " + string.Join("; ", created.Errors.Select(e => e.Code)));
        }

        await users.AddToRoleAsync(user, role);
        return user;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
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
}

/// <summary>Navegador mínimo do painel: preenche formulários com o token antiforgery da própria página.</summary>
internal static class Browser
{
    public static async Task<HttpResponseMessage> SignInAsync(this HttpClient client, string email, string password)
    {
        string page = await client.GetStringAsync("/painel/entrar");
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        using FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ReturnUrl"] = string.Empty,
            ["__RequestVerificationToken"] = token
        });
        return await client.PostAsync("/painel/entrar", form);
    }

    public static async Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client, string formPage, string action, IDictionary<string, string> fields = null)
    {
        string page = await client.GetStringAsync(formPage);
        string token = Regex.Match(page, @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""").Groups[1].Value;
        Dictionary<string, string> body = new(fields ?? new Dictionary<string, string>()) { ["__RequestVerificationToken"] = token };
        using FormUrlEncodedContent form = new(body);
        return await client.PostAsync(action, form);
    }

    public static string Destination(this HttpResponseMessage response) =>
        response.Headers.Location.IsAbsoluteUri ? response.Headers.Location.PathAndQuery : response.Headers.Location.OriginalString;

    public static async Task<string> TextAsync(this HttpResponseMessage response) =>
        System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
}
