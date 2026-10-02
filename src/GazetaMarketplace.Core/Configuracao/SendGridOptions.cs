using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Core.Configuracao;

/// <summary>Envio de e-mail pelo SendGrid (ADR-009).</summary>
public sealed class SendGridOptions
{
    public const string SectionName = "SendGrid";

    [Required]
    public string ApiKey { get; set; }

    [Required]
    [EmailAddress]
    public string FromEmail { get; set; }
}
