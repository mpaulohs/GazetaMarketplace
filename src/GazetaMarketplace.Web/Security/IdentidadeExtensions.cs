using System;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Navigation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Security;

/// <summary>Identity com cookie para a equipe (ADR-003): senha, bloqueio de conta, sessão e políticas por papel.</summary>
public static class IdentidadeExtensions
{
    /// <summary>Nome do cookie da sessão da equipe.</summary>
    public const string NomeDoCookie = "Gazeta.Equipe";

    /// <summary>Acrescentado ao endereço de entrada quando o cookie existia mas já não vale (S08).</summary>
    public const string ParametroSessaoExpirada = "expirada";

    // Quem desativa uma conta perde o acesso em até este intervalo (NFR-08)
    private static readonly TimeSpan IntervaloDeRevalidacao = TimeSpan.FromMinutes(5);

    public static IServiceCollection AddTeamIdentity(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ContadorDeFalhasDeLogin>();

        services.AddIdentity<UsuarioIdentity, PapelIdentity>(options =>
            {
                // NFR-07
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;

                // NFR-06: além do contador por IP, a própria conta trava por 15 minutos depois de 5 falhas
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = ContadorDeFalhasDeLogin.Limite;
                options.Lockout.DefaultLockoutTimeSpan = ContadorDeFalhasDeLogin.Window;

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager<SignInManagerDaEquipe>()
            .AddClaimsPrincipalFactory<FabricaDeClaimsDaEquipe>()
            .AddErrorDescriber<DescritorDeErrosDaEquipe>()
            .AddDefaultTokenProviders();

        // NFR-09: link de redefinição de 1 hora (a troca de senha do primeiro acesso usa o mesmo mecanismo)
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(1));

        // S17: cria o primeiro Administrador a partir das variáveis de ambiente, se ainda não houver nenhum
        services.AddHostedService<BootstrapAdminInitializer>();

        services.AddOptions<SecurityStampValidatorOptions>().Configure<TimeProvider>((options, time) =>
        {
            options.ValidationInterval = IntervaloDeRevalidacao;
            options.TimeProvider = time;
        });

        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<IOptions<AutenticacaoOptions>, TimeProvider>(ConfigurarCookie);

        services.AddAuthorizationBuilder()
            .AddPolicy(PoliticasDeAcesso.Administrator, politica => politica.RequireRole(RoleNames.Administrator))
            .AddPolicy(PoliticasDeAcesso.Writer, politica => politica.RequireRole(RoleNames.Writer, RoleNames.Administrator));

        return services;
    }

    private static void ConfigurarCookie(CookieAuthenticationOptions cookie, IOptions<AutenticacaoOptions> autenticacao, TimeProvider time)
    {
        cookie.TimeProvider = time;
        cookie.Cookie.Name = NomeDoCookie;
        cookie.Cookie.HttpOnly = true;
        cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        cookie.Cookie.SameSite = SameSiteMode.Lax;
        cookie.ExpireTimeSpan = TimeSpan.FromMinutes(autenticacao.Value.SessaoMinutos);
        cookie.SlidingExpiration = true;
        cookie.LoginPath = PanelRoutes.SignIn;
        cookie.LogoutPath = PanelRoutes.SignOut;
        cookie.AccessDeniedPath = PanelRoutes.AccessDenied;

        // Quem chega com um cookie que já não vale teve a sessão expirada (S08); quem nunca entrou (S07) não vê o aviso
        cookie.Events.OnRedirectToLogin = context =>
        {
            string destino = context.Request.Cookies.ContainsKey(NomeDoCookie)
                ? QueryHelpers.AddQueryString(context.RedirectUri, ParametroSessaoExpirada, "1")
                : context.RedirectUri;
            context.Response.Redirect(destino);
            return System.Threading.Tasks.Task.CompletedTask;
        };
    }
}
