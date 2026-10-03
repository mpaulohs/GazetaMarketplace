using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Areas.Painel.Models;
using GazetaMarketplace.Web.Models;
using GazetaMarketplace.Web.Navigation;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace GazetaMarketplace.Web.Areas.Painel.Controllers;

/// <summary>Entrar e sair da equipe (US-006). Toda falha mostra a mesma mensagem: nada revela se a conta existe.</summary>
[Area("Painel")]
[AllowAnonymous]
[Route("painel")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ContaController(
    UserManager<UsuarioIdentity> users,
    SignInManager<UsuarioIdentity> acesso,
    ContadorDeFalhasDeLogin falhas,
    ILogger<ContaController> log) : Controller
{
    public const string MensagemDeFalha = "E-mail ou senha inválidos, ou conta desativada";

    public const string MensagemDeExcesso = "Muitas tentativas. Tente novamente em alguns minutos.";

    private static readonly PasswordHasher<UsuarioIdentity> Hasher = new();

    // Hash de uma senha qualquer: e-mail inexistente gasta o mesmo tempo que senha errada (não revela contas)
    private static readonly string HashFalso = Hasher.HashPassword(new UsuarioIdentity(), "Falsa!Senha1");

    [HttpGet("entrar")]
    public IActionResult SignIn(
        [FromQuery(Name = "ReturnUrl")] string retorno,
        [FromQuery(Name = IdentidadeExtensions.ParametroSessaoExpirada)] string expirada)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(PaginaInicialDe(User.IsInRole(RoleNames.Administrator)));
        }

        return View(new EntrarViewModel { Retorno = retorno, SessaoExpirada = expirada == "1" });
    }

    [HttpPost("entrar")]
    public async Task<IActionResult> SignIn(EntrarViewModel modelo)
    {
        string source = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        string submittedPassword = modelo.Senha;
        modelo.Senha = null; // a senha nunca volta para a tela (S04)

        if (falhas.EstaBloqueado(source))
        {
            log.LogWarning("Entrada recusada: origem {Origem} bloqueada por excesso de falhas", source);
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            Response.Headers.RetryAfter = Math.Ceiling(falhas.EsperaRestante(source).TotalSeconds).ToString(CultureInfo.InvariantCulture);
            ModelState.AddModelError(string.Empty, MensagemDeExcesso);
            return View(modelo);
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        UsuarioIdentity user = await users.FindByEmailAsync(modelo.Email.Trim());
        SignInResult result;
        if (user is null)
        {
            Hasher.VerifyHashedPassword(new UsuarioIdentity(), HashFalso, submittedPassword);
            result = SignInResult.Failed;
        }
        else
        {
            result = await acesso.PasswordSignInAsync(user, submittedPassword, isPersistent: false, lockoutOnFailure: true);
        }

        if (result.Succeeded)
        {
            bool administrador = await users.IsInRoleAsync(user, RoleNames.Administrator);
            falhas.Zerar(source);
            log.LogInformation("Entrada do usuário {UsuarioId} ({Papel})", user.Id, administrador ? RoleNames.Administrator : RoleNames.Writer);

            // Senha provisória (S09): a troca vem antes de qualquer página, inclusive a que a pessoa tentava abrir
            return user.MustChangePassword
                ? LocalRedirect(PanelRoutes.SetPassword)
                : LocalRedirect(RetornoLocalOu(modelo.Retorno, PaginaInicialDe(administrador)));
        }

        RegistrarFalha(source, modelo.Email, user, result);

        // Mesma mensagem para senha errada, e-mail inexistente, conta desativada e conta bloqueada (S04, S05)
        ModelState.AddModelError(string.Empty, MensagemDeFalha);
        return View(modelo);
    }

    [HttpPost("sair")]
    public async Task<IActionResult> SignOutAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            log.LogInformation("Saída do usuário {UsuarioId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        await acesso.SignOutAsync();
        return Redirect(PanelRoutes.SignIn);
    }

    [HttpGet("acesso-negado")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View(new PageStateViewModel
        {
            Title = "Você não tem permissão para acessar esta página",
            ActionText = "Ir para o painel",
            ActionUrl = PaginaInicialDe(User.IsInRole(RoleNames.Administrator))
        });
    }

    private static string PaginaInicialDe(bool administrador) => administrador ? PanelRoutes.ReviewQueue : PanelRoutes.Ads;

    // RC-18: o endereço de retorno só vale se for local (e não a própria entrada); senão vai para a página inicial do papel
    private string RetornoLocalOu(string retorno, string defaultValue) =>
        !string.IsNullOrEmpty(retorno) && Url.IsLocalUrl(retorno) && !retorno.StartsWith(PanelRoutes.SignIn, StringComparison.OrdinalIgnoreCase)
            ? retorno
            : defaultValue;

    private void RegistrarFalha(string source, string email, UsuarioIdentity user, SignInResult result)
    {
        bool fechouBloqueio = falhas.RegistrarFalha(source);
        string motivo = user is null ? "conta inexistente"
            : result.IsLockedOut ? "conta bloqueada"
            : result.IsNotAllowed ? "conta desativada"
            : "senha incorreta";

        // Quem digita a senha no campo de e-mail não pode ir parar no log: só entra o que parece e-mail
        string paraLog = email.Contains('@', StringComparison.Ordinal) && email.Length <= 254 ? email : "(formato inválido)";
        log.LogWarning("Falha de entrada de {Email} a partir de {Origem}: {Motivo}", paraLog, source, motivo);

        if (result.IsLockedOut)
        {
            log.LogWarning("Conta do usuário {UsuarioId} bloqueada por tentativas de senha", user.Id);
        }

        if (fechouBloqueio)
        {
            log.LogWarning(
                "Origem {Origem} bloqueada por {Limite} falhas de entrada em {Janela} minutos",
                source, ContadorDeFalhasDeLogin.Limite, ContadorDeFalhasDeLogin.Window.TotalMinutes);
        }
    }
}
