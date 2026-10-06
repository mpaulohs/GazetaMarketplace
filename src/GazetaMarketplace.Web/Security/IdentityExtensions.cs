using System;
using GazetaMarketplace.Core.Configuration;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Web.Navigation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Security;

/// <summary>Identity com cookie para a equipe (ADR-003): senha, bloqueio de conta, sessão e políticas por papel.</summary>
public static class IdentityExtensions
{
    /// <summary>Nome do cookie da sessão da equipe.</summary>
    public const string CookieName = "Gazeta.Team";

    /// <summary>Acrescentado ao endereço de entrada quando o cookie existia mas já não vale (S08).</summary>
    public const string SessionExpiredParameter = "expirada";

    // Quem desativa uma conta perde o acesso em até este intervalo (NFR-08)
    private static readonly TimeSpan RevalidationInterval = TimeSpan.FromMinutes(5);

    public static IServiceCollection AddTeamIdentity(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<LoginFailureCounter>();

        services.AddIdentity<AppUser, AppRole>(options =>
            {
                // NFR-07
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;

                // NFR-06: além do contador por IP, a própria conta trava por 15 minutos depois de 5 falhas
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = LoginFailureCounter.Limit;
                options.Lockout.DefaultLockoutTimeSpan = LoginFailureCounter.Window;

                options.User.RequireUniqueEmail = true;

                // US-007: o link de redefinição usa o token próprio (relógio do site, "expirou" separado de "já usado")
                options.Tokens.PasswordResetTokenProvider = RecoveryTokenProvider.Name;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager<TeamSignInManager>()
            .AddClaimsPrincipalFactory<TeamClaimsPrincipalFactory>()
            .AddErrorDescriber<TeamIdentityErrorDescriber>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<RecoveryTokenProvider>(RecoveryTokenProvider.Name);

        // NFR-09: link de redefinição de 1 hora (a troca de senha do primeiro acesso usa o mesmo mecanismo)
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(1));

        // S17: cria o primeiro Administrador a partir das variáveis de ambiente, se ainda não houver nenhum
        services.AddHostedService<BootstrapAdminInitializer>();

        services.AddOptions<SecurityStampValidatorOptions>().Configure<TimeProvider>((options, time) =>
        {
            options.ValidationInterval = RevalidationInterval;
            options.TimeProvider = time;
        });

        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<IOptions<AuthenticationOptions>, TimeProvider>(ConfigureCookie);

        services.AddAuthorizationBuilder()
            .AddPolicy(AccessPolicies.Administrator, policy => policy.RequireRole(RoleNames.Administrator))
            .AddPolicy(AccessPolicies.Writer, policy => policy.RequireRole(RoleNames.Writer, RoleNames.Administrator));

        return services;
    }

    private static void ConfigureCookie(CookieAuthenticationOptions cookie, IOptions<AuthenticationOptions> authentication, TimeProvider time)
    {
        cookie.TimeProvider = time;
        cookie.Cookie.Name = CookieName;
        cookie.Cookie.HttpOnly = true;
        cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        cookie.Cookie.SameSite = SameSiteMode.Lax;
        cookie.ExpireTimeSpan = TimeSpan.FromMinutes(authentication.Value.SessionMinutes);
        cookie.SlidingExpiration = true;
        cookie.LoginPath = PanelRoutes.SignIn;
        cookie.LogoutPath = PanelRoutes.SignOut;
        cookie.AccessDeniedPath = PanelRoutes.AccessDenied;

        // Quem chega com um cookie que já não vale teve a sessão expirada (S08); quem nunca entrou (S07) não vê o aviso
        cookie.Events.OnRedirectToLogin = context =>
        {
            // Endpoints JSON não redirecionam para uma página: respondem 401 no contrato de erros (ARCHITECTURE §8)
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                return ApiProblem.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Entre para continuar.");
            }

            string destination = context.Request.Cookies.ContainsKey(CookieName)
                ? QueryHelpers.AddQueryString(context.RedirectUri, SessionExpiredParameter, "1")
                : context.RedirectUri;
            context.Response.Redirect(destination);
            return System.Threading.Tasks.Task.CompletedTask;
        };

        cookie.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                return ApiProblem.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden, "FORBIDDEN", "Você não tem permissão para esta operação.");
            }

            // Antes de redirecionar: aqui o endereço ainda é o que a pessoa pediu (a tela de destino já não diz qual foi)
            AccessDeniedLog.Write(context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(AccessDeniedLog.Category), context.HttpContext);
            context.Response.Redirect(context.RedirectUri);
            return System.Threading.Tasks.Task.CompletedTask;
        };
    }
}
