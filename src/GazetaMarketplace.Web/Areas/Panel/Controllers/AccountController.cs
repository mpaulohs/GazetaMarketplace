using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Team;
using GazetaMarketplace.Infrastructure.Identity;
using GazetaMarketplace.Web.Areas.Panel.Models;
using GazetaMarketplace.Web.Models;
using GazetaMarketplace.Web.Navigation;
using GazetaMarketplace.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace GazetaMarketplace.Web.Areas.Panel.Controllers;

/// <summary>Entrar e sair da equipe (US-006). Toda falha mostra a mesma mensagem: nada revela se a conta existe.</summary>
[Area("Panel")]
[AllowAnonymous]
[Route("painel")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class AccountController(
    UserManager<AppUser> users,
    SignInManager<AppUser> access,
    LoginFailureCounter failures,
    IPasswordRecovery recovery,
    ILogger<AccountController> log) : Controller
{
    public const string InvalidCredentialsMessage = "E-mail ou senha inválidos, ou conta desativada";

    /// <summary>Acrescentado ao endereço de entrada depois de redefinir a senha pelo link (US-007-S02).</summary>
    public const string ChangedParameter = "alterada";

    public const string TooManyAttemptsMessage = "Muitas tentativas. Tente novamente em alguns minutos.";

    private static readonly PasswordHasher<AppUser> Hasher = new();

    // Hash de uma senha qualquer: e-mail inexistente gasta o mesmo tempo que senha errada (não revela contas)
    private static readonly string DummyHash = Hasher.HashPassword(new AppUser(), "Falsa!Senha1");

    [HttpGet("entrar")]
    public IActionResult SignIn(
        [FromQuery(Name = "ReturnUrl")] string returnUrl,
        [FromQuery(Name = IdentityExtensions.SessionExpiredParameter)] string expired,
        [FromQuery(Name = ChangedParameter)] string changed)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(HomePageFor(User.IsInRole(RoleNames.Administrator)));
        }

        return View(new SignInViewModel { ReturnUrl = returnUrl, SessionExpired = expired == "1", PasswordChanged = changed == "1" });
    }

    [HttpPost("entrar")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> SignIn(SignInViewModel model)
    {
        string source = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        string submittedPassword = model.Password;
        model.Password = ""; // a senha nunca volta para a tela (S04)

        if (failures.IsBlocked(source))
        {
            log.LogWarning("Entrada recusada: origem {Source} bloqueada por excesso de falhas", source);
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            Response.Headers.RetryAfter = Math.Ceiling(failures.RemainingWait(source).TotalSeconds).ToString(CultureInfo.InvariantCulture);
            ModelState.AddModelError(string.Empty, TooManyAttemptsMessage);
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        AppUser user = await users.FindByEmailAsync(model.Email.Trim());
        SignInResult result;
        if (user is null)
        {
            Hasher.VerifyHashedPassword(new AppUser(), DummyHash, submittedPassword);
            result = SignInResult.Failed;
        }
        else
        {
            result = await access.PasswordSignInAsync(user, submittedPassword, isPersistent: false, lockoutOnFailure: true);
        }

        if (result.Succeeded)
        {
            bool administrator = await users.IsInRoleAsync(user, RoleNames.Administrator);
            failures.Reset(source);
            log.LogInformation("Entrada do usuário {UserId} ({Role})", user.Id, administrator ? RoleNames.Administrator : RoleNames.Writer);

            // Senha provisória (S09): a troca vem antes de qualquer página, inclusive a que a pessoa tentava abrir
            return user.MustChangePassword
                ? LocalRedirect(PanelRoutes.SetPassword)
                : LocalRedirect(LocalReturnUrlOr(model.ReturnUrl, HomePageFor(administrator)));
        }

        RecordFailure(source, model.Email, user, result);

        // Mesma mensagem para senha errada, e-mail inexistente, conta desativada e conta bloqueada (S04, S05)
        ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
        return View(model);
    }

    [HttpGet("esqueci-minha-senha")]
    public IActionResult Forgot() => View(new ForgotPasswordViewModel());

    // A resposta é a mesma exista a conta ou não, venha o envio a falhar ou não (US-007-S03, RC-13): a conta só é procurada depois, em segundo plano
    [HttpPost("esqueci-minha-senha")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> Forgot(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string source = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        await recovery.RequestAsync(model.Email, source, $"{Request.Scheme}://{Request.Host}", HttpContext.TraceIdentifier, cancellationToken);
        return View(new ForgotPasswordViewModel { Sent = true });
    }

    [HttpGet("redefinir-senha")]
    public async Task<IActionResult> Reset(int id, string code, CancellationToken cancellationToken)
    {
        RecoveryLinkState state = await recovery.CheckLinkAsync(id, code, cancellationToken);
        return state == RecoveryLinkState.Valid
            ? View(new RecoverPasswordViewModel { Id = id, Code = code })
            : LinkProblem(state);
    }

    [HttpPost("redefinir-senha")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> Reset(RecoverPasswordViewModel model, CancellationToken cancellationToken)
    {
        RecoveryLinkState state = await recovery.CheckLinkAsync(model.Id, model.Code, cancellationToken);
        if (state != RecoveryLinkState.Valid)
        {
            return LinkProblem(state);
        }

        string newPassword = model.NewPassword;
        model.NewPassword = null;
        model.ConfirmPassword = null; // nenhuma senha volta para a tela

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        PasswordResetResult result = await recovery.ResetAsync(model.Id, model.Code, newPassword, cancellationToken);
        if (result.Succeeded)
        {
            return Redirect(PanelRoutes.SignIn + "?" + ChangedParameter + "=1");
        }

        if (result.LinkState != RecoveryLinkState.Valid)
        {
            return LinkProblem(result.LinkState);
        }

        foreach (string error in result.PasswordErrors)
        {
            ModelState.AddModelError(nameof(RecoverPasswordViewModel.NewPassword), error);
        }

        return View(model);
    }

    [HttpPost("sair")]
    public async Task<IActionResult> SignOutAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            log.LogInformation("Saída do usuário {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        await access.SignOutAsync();
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
            ActionUrl = HomePageFor(User.IsInRole(RoleNames.Administrator))
        });
    }

    // US-007-S04 e S05: link vencido ou já usado, com o botão para pedir outro
    private IActionResult LinkProblem(RecoveryLinkState state) => View("LinkProblem", new PageStateViewModel
    {
        Title = state switch
        {
            RecoveryLinkState.Expired => PasswordRecoveryMessages.LinkExpired,
            RecoveryLinkState.Used => PasswordRecoveryMessages.LinkUsed,
            _ => PasswordRecoveryMessages.LinkInvalid
        },
        ActionText = "Pedir novo link",
        ActionUrl = PanelRoutes.ForgotPassword
    });

    private static string HomePageFor(bool administrator) => administrator ? PanelRoutes.ReviewQueue : PanelRoutes.Ads;

    // RC-18: o endereço de retorno só vale se for local (e não a própria entrada); senão vai para a página inicial do papel
    private string LocalReturnUrlOr(string returnUrl, string defaultValue) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.StartsWith(PanelRoutes.SignIn, StringComparison.OrdinalIgnoreCase)
            ? returnUrl
            : defaultValue;

    private void RecordFailure(string source, string email, AppUser user, SignInResult result)
    {
        bool blockEnded = failures.RecordFailure(source);
        string reason = user is null ? "conta inexistente"
            : result.IsLockedOut ? "conta bloqueada"
            : result.IsNotAllowed ? "conta desativada"
            : "senha incorreta";

        // Quem digita a senha no campo de e-mail não pode ir parar no log: só entra o que parece e-mail
        string forLog = email.Contains('@', StringComparison.Ordinal) && email.Length <= 254 ? email : "(formato inválido)";
        log.LogWarning("Falha de entrada de {Email} a partir de {Source}: {Reason}", forLog, source, reason);

        if (result.IsLockedOut)
        {
            log.LogWarning("Conta do usuário {UserId} bloqueada por tentativas de senha", user.Id);
        }

        if (blockEnded)
        {
            log.LogWarning(
                "Origem {Source} bloqueada por {Limit} falhas de entrada em {Window} minutos",
                source, LoginFailureCounter.Limit, LoginFailureCounter.Window.TotalMinutes);
        }
    }
}
