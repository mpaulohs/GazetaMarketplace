using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Core.Configuration;

/// <summary>
/// Endereço público do site, usado para montar os links enviados por e-mail. Em Production é obrigatório:
/// montar o link a partir do cabeçalho Host da requisição deixaria um atacante apontar o e-mail de
/// redefinição para um endereço dele (envenenamento do link de redefinição).
/// </summary>
public sealed class SiteOptions
{
    public const string SectionName = "Site";

    [Required]
    [Url]
    public string BaseUrl { get; set; }
}
