using System;
using GazetaMarketplace.Core.Configuracao;
using GazetaMarketplace.Core.Equipe;
using GazetaMarketplace.Infrastructure.Data;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Navegacao;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GazetaMarketplace.Web.Seguranca;

/// <summary>Identity com cookie para a equipe (ADR-003): senha, bloqueio de conta, sessão e políticas por papel.</summary>
public static class IdentidadeExtensions
{
    /// <summary>Nome do cookie da sessão da equipe.</summary>
    public const string NomeDoCookie = "Gazeta.Equipe";

    /// <summary>Acrescentado ao endereço de entrada quando o cookie existia mas já não vale (S08).</summary>
    public const string ParametroSessaoExpirada = "expirada";

    // Quem desativa uma conta perde o acesso em até este intervalo (NFR-08)
    private static readonly TimeSpan IntervaloDeRevalidacao = TimeSpan.FromMinutes(5);

    public static IServiceCollection AddIdentidade(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ContadorDeFalhasDeLogin>();

        services.AddIdentity<UsuarioIdentity, PapelIdentity>(opcoes =>
            {
                // NFR-07
                opcoes.Password.RequiredLength = 8;
                opcoes.Password.RequireUppercase = true;
                opcoes.Password.RequireLowercase = true;
                opcoes.Password.RequireDigit = true;
                opcoes.Password.RequireNonAlphanumeric = true;

                // NFR-06: além do contador por IP, a própria conta trava por 15 minutos depois de 5 falhas
                opcoes.Lockout.AllowedForNewUsers = true;
                opcoes.Lockout.MaxFailedAccessAttempts = ContadorDeFalhasDeLogin.Limite;
                opcoes.Lockout.DefaultLockoutTimeSpan = ContadorDeFalhasDeLogin.Janela;

                opcoes.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager<SignInManagerDaEquipe>()
            .AddClaimsPrincipalFactory<FabricaDeClaimsDaEquipe>()
            .AddErrorDescriber<DescritorDeErrosDaEquipe>()
            .AddDefaultTokenProviders();

        // NFR-09: link de redefinição de 1 hora (a troca de senha do primeiro acesso usa o mesmo mecanismo)
        services.Configure<DataProtectionTokenProviderOptions>(opcoes => opcoes.TokenLifespan = TimeSpan.FromHours(1));

        // S17: cria o primeiro Administrador a partir das variáveis de ambiente, se ainda não houver nenhum
        services.AddHostedService<BootstrapAdminInitializer>();

        services.AddOptions<SecurityStampValidatorOptions>().Configure<TimeProvider>((opcoes, tempo) =>
        {
            opcoes.ValidationInterval = IntervaloDeRevalidacao;
            opcoes.TimeProvider = tempo;
        });

        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<IOptions<AutenticacaoOptions>, TimeProvider>(ConfigurarCookie);

        services.AddAuthorizationBuilder()
            .AddPolicy(PoliticasDeAcesso.Administrador, politica => politica.RequireRole(Papeis.Administrador))
            .AddPolicy(PoliticasDeAcesso.Redator, politica => politica.RequireRole(Papeis.Redator, Papeis.Administrador));

        return services;
    }

    private static void ConfigurarCookie(CookieAuthenticationOptions cookie, IOptions<AutenticacaoOptions> autenticacao, TimeProvider tempo)
    {
        cookie.TimeProvider = tempo;
        cookie.Cookie.Name = NomeDoCookie;
        cookie.Cookie.HttpOnly = true;
        cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        cookie.Cookie.SameSite = SameSiteMode.Lax;
        cookie.ExpireTimeSpan = TimeSpan.FromMinutes(autenticacao.Value.SessaoMinutos);
        cookie.SlidingExpiration = true;
        cookie.LoginPath = RotasDoPainel.Entrar;
        cookie.LogoutPath = RotasDoPainel.Sair;
        cookie.AccessDeniedPath = RotasDoPainel.AcessoNegado;

        // Quem chega com um cookie que já não vale teve a sessão expirada (S08); quem nunca entrou (S07) não vê o aviso
        cookie.Events.OnRedirectToLogin = contexto =>
        {
            string destino = contexto.Request.Cookies.ContainsKey(NomeDoCookie)
                ? QueryHelpers.AddQueryString(contexto.RedirectUri, ParametroSessaoExpirada, "1")
                : contexto.RedirectUri;
            contexto.Response.Redirect(destino);
            return System.Threading.Tasks.Task.CompletedTask;
        };
    }
}
