using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Web.Areas.Panel.Models;

/// <summary>Troca obrigatória da senha provisória (US-006-S09). Nenhum dos dois campos volta para a tela.</summary>
public sealed class SetPasswordViewModel
{
    [Required(ErrorMessage = "Informe a nova senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [Compare(nameof(NewPassword), ErrorMessage = "As senhas não são iguais.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmPassword { get; set; }
}
