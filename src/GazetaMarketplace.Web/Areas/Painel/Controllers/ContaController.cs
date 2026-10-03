using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Equipe;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Areas.Painel.Models;
using GazetaMarketplace.Web.Models;
using GazetaMarketplace.Web.Navegacao;
using GazetaMarketplace.Web.Seguranca;
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
    UserManager<UsuarioIdentity> usuarios,
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
    public IActionResult Entrar(
        [FromQuery(Name = "ReturnUrl")] string retorno,
        [FromQuery(Name = IdentidadeExtensions.ParametroSessaoExpirada)] string expirada)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(PaginaInicialDe(User.IsInRole(Papeis.Administrador)));
        }

        return View(new EntrarViewModel { Retorno = retorno, SessaoExpirada = expirada == "1" });
    }

    [HttpPost("entrar")]
    public async Task<IActionResult> Entrar(EntrarViewModel modelo)
    {
        string origem = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        string senha = modelo.Senha;
        modelo.Senha = null; // a senha nunca volta para a tela (S04)

        if (falhas.EstaBloqueado(origem))
        {
            log.LogWarning("Entrada recusada: origem {Origem} bloqueada por excesso de falhas", origem);
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            Response.Headers.RetryAfter = Math.Ceiling(falhas.EsperaRestante(origem).TotalSeconds).ToString(CultureInfo.InvariantCulture);
            ModelState.AddModelError(string.Empty, MensagemDeExcesso);
            return View(modelo);
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        UsuarioIdentity usuario = await usuarios.FindByEmailAsync(modelo.Email.Trim());
        SignInResult resultado;
        if (usuario is null)
        {
            Hasher.VerifyHashedPassword(new UsuarioIdentity(), HashFalso, senha);
            resultado = SignInResult.Failed;
        }
        else
        {
            resultado = await acesso.PasswordSignInAsync(usuario, senha, isPersistent: false, lockoutOnFailure: true);
        }

        if (resultado.Succeeded)
        {
            bool administrador = await usuarios.IsInRoleAsync(usuario, Papeis.Administrador);
            falhas.Zerar(origem);
            log.LogInformation("Entrada do usuário {UsuarioId} ({Papel})", usuario.Id, administrador ? Papeis.Administrador : Papeis.Redator);
            return LocalRedirect(RetornoLocalOu(modelo.Retorno, PaginaInicialDe(administrador)));
        }

        RegistrarFalha(origem, modelo.Email, usuario, resultado);

        // Mesma mensagem para senha errada, e-mail inexistente, conta desativada e conta bloqueada (S04, S05)
        ModelState.AddModelError(string.Empty, MensagemDeFalha);
        return View(modelo);
    }

    [HttpPost("sair")]
    public async Task<IActionResult> Sair()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            log.LogInformation("Saída do usuário {UsuarioId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        await acesso.SignOutAsync();
        return Redirect(RotasDoPainel.Entrar);
    }

    [HttpGet("acesso-negado")]
    public IActionResult AcessoNegado()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View(new EstadoPaginaViewModel
        {
            Titulo = "Você não tem permissão para acessar esta página",
            AcaoTexto = "Ir para o painel",
            AcaoUrl = PaginaInicialDe(User.IsInRole(Papeis.Administrador))
        });
    }

    private static string PaginaInicialDe(bool administrador) => administrador ? RotasDoPainel.Fila : RotasDoPainel.Anuncios;

    // RC-18: o endereço de retorno só vale se for local (e não a própria entrada); senão vai para a página inicial do papel
    private string RetornoLocalOu(string retorno, string padrao) =>
        !string.IsNullOrEmpty(retorno) && Url.IsLocalUrl(retorno) && !retorno.StartsWith(RotasDoPainel.Entrar, StringComparison.OrdinalIgnoreCase)
            ? retorno
            : padrao;

    private void RegistrarFalha(string origem, string email, UsuarioIdentity usuario, SignInResult resultado)
    {
        bool fechouBloqueio = falhas.RegistrarFalha(origem);
        string motivo = usuario is null ? "conta inexistente"
            : resultado.IsLockedOut ? "conta bloqueada"
            : resultado.IsNotAllowed ? "conta desativada"
            : "senha incorreta";

        // Quem digita a senha no campo de e-mail não pode ir parar no log: só entra o que parece e-mail
        string paraLog = email.Contains('@', StringComparison.Ordinal) && email.Length <= 254 ? email : "(formato inválido)";
        log.LogWarning("Falha de entrada de {Email} a partir de {Origem}: {Motivo}", paraLog, origem, motivo);

        if (resultado.IsLockedOut)
        {
            log.LogWarning("Conta do usuário {UsuarioId} bloqueada por tentativas de senha", usuario.Id);
        }

        if (fechouBloqueio)
        {
            log.LogWarning(
                "Origem {Origem} bloqueada por {Limite} falhas de entrada em {Janela} minutos",
                origem, ContadorDeFalhasDeLogin.Limite, ContadorDeFalhasDeLogin.Janela.TotalMinutes);
        }
    }
}
