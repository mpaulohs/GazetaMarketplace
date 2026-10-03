using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Web.Areas.Painel.Models;

/// <summary>Troca obrigatória da senha provisória (US-006-S09). Nenhum dos dois campos volta para a tela.</summary>
public sealed class DefinirSenhaViewModel
{
    [Required(ErrorMessage = "Informe a nova senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nova senha")]
    public string NovaSenha { get; set; }

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não são iguais.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmarSenha { get; set; }
}
