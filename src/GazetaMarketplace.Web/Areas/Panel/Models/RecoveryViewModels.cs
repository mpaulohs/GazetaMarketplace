using System.ComponentModel.DataAnnotations;
using GazetaMarketplace.Core.Team;

namespace GazetaMarketplace.Web.Areas.Panel.Models;

/// <summary>"Esqueci minha senha" (US-007-S01). Depois do envio a mesma tela mostra a confirmação neutra, exista a conta ou não.</summary>
public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido")]
    [StringLength(254, ErrorMessage = "Informe um e-mail válido")]
    [Display(Name = "E-mail")]
    public string Email { get; set; }

    /// <summary>Verdadeiro depois de aceitar o pedido: mostra a mensagem neutra no lugar do formulário.</summary>
    public bool Sent { get; set; }
}

/// <summary>"Definir nova senha", aberta pelo link do e-mail (US-007-S02). Nenhuma senha volta para a tela.</summary>
public sealed class RecoverPasswordViewModel
{
    public int Id { get; set; }

    /// <summary>Código do link; vai e volta num campo oculto.</summary>
    public string Code { get; set; }

    [Required(ErrorMessage = "Informe a nova senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [Compare(nameof(NewPassword), ErrorMessage = PasswordRecoveryMessages.PasswordsDoNotMatch)]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmPassword { get; set; }
}
