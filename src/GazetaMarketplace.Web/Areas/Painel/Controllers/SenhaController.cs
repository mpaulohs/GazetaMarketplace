using System.Linq;
using System.Threading.Tasks;
using GazetaMarketplace.Core.Equipe;
using GazetaMarketplace.Infrastructure.Identidade;
using GazetaMarketplace.Web.Areas.Painel.Models;
using GazetaMarketplace.Web.Navegacao;
using GazetaMarketplace.Web.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GazetaMarketplace.Web.Areas.Painel.Controllers;

/// <summary>
/// "Defina sua nova senha" (US-006-S09). Não herda de <see cref="PainelControllerBase"/> de propósito: aquele filtro
/// leva para esta própria tela e a pessoa ficaria presa num redirecionamento sem fim.
/// </summary>
[Area("Painel")]
[Authorize(Policy = PoliticasDeAcesso.Redator)]
[Route("painel")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class SenhaController(
    UserManager<UsuarioIdentity> usuarios,
    SignInManager<UsuarioIdentity> acesso,
    ILogger<SenhaController> log) : Controller
{
    public const string MensagemIgualAProvisoria = "A nova senha precisa ser diferente da provisória";

    [HttpGet("definir-senha")]
    public async Task<IActionResult> DefinirSenha()
    {
        UsuarioIdentity usuario = await usuarios.GetUserAsync(User);
        return usuario is { MustChangePassword: true } ? View(new DefinirSenhaViewModel()) : LocalRedirect(PaginaInicial());
    }

    [HttpPost("definir-senha")]
    public async Task<IActionResult> DefinirSenha(DefinirSenhaViewModel modelo)
    {
        UsuarioIdentity usuario = await usuarios.GetUserAsync(User);
        if (usuario is not { MustChangePassword: true })
        {
            return LocalRedirect(PaginaInicial());
        }

        string nova = modelo.NovaSenha;
        modelo.NovaSenha = null;
        modelo.ConfirmarSenha = null; // nenhuma senha volta para a tela

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        if (await usuarios.CheckPasswordAsync(usuario, nova))
        {
            ModelState.AddModelError(nameof(DefinirSenhaViewModel.NovaSenha), MensagemIgualAProvisoria);
            return View(modelo);
        }

        // Atômico: o token valida a política antes de gravar, então uma senha fraca deixa a provisória intacta
        string token = await usuarios.GeneratePasswordResetTokenAsync(usuario);
        IdentityResult troca = await usuarios.ResetPasswordAsync(usuario, token, nova);
        if (!troca.Succeeded)
        {
            foreach (IdentityError erro in troca.Errors)
            {
                ModelState.AddModelError(nameof(DefinirSenhaViewModel.NovaSenha), erro.Description);
            }

            log.LogWarning("Troca de senha recusada para o usuário {UsuarioId}: {Erros}", usuario.Id, string.Join(", ", troca.Errors.Select(e => e.Code)));
            return View(modelo);
        }

        usuario.MustChangePassword = false;
        await usuarios.UpdateAsync(usuario);

        // O carimbo de segurança mudou: emite um cookie novo, já sem o aviso de senha provisória
        await acesso.RefreshSignInAsync(usuario);
        log.LogInformation("Usuário {UsuarioId} definiu a nova senha no primeiro acesso", usuario.Id);
        return LocalRedirect(PaginaInicial());
    }

    private string PaginaInicial() => User.IsInRole(Papeis.Administrador) ? RotasDoPainel.Fila : RotasDoPainel.Anuncios;
}
