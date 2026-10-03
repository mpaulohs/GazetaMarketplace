using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Web.Areas.Panel.Models;

/// <summary>Formulário de entrada da equipe. O endereço de retorno só vale se for local (RC-18).</summary>
public sealed class SignInViewModel
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; }

    public string ReturnUrl { get; set; }

    /// <summary>Verdadeiro quando o cookie da sessão existia mas venceu (S08).</summary>
    public bool SessionExpired { get; set; }

    /// <summary>Verdadeiro logo depois de redefinir a senha pelo link do e-mail (US-007-S02).</summary>
    public bool PasswordChanged { get; set; }
}
