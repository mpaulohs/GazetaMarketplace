using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Web.Areas.Painel.Models;

/// <summary>Formulário de entrada da equipe. O endereço de retorno só vale se for local (RC-18).</summary>
public sealed class EntrarViewModel
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; }

    public string Retorno { get; set; }

    /// <summary>Verdadeiro quando o cookie da sessão existia mas venceu (S08).</summary>
    public bool SessaoExpirada { get; set; }
}
