using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Core.Configuration;

/// <summary>Envio de e-mail pelo SendGrid (ADR-009).</summary>
public sealed class SendGridOptions
{
    public const string SectionName = "SendGrid";

    [Required]
    public string ApiKey { get; set; }

    [Required]
    [EmailAddress]
    public string FromEmail { get; set; }

    /// <summary>Endereço da API; só muda em testes de ponta a ponta, que apontam para um SendGrid de mentira local.</summary>
    [Url]
    public string BaseUrl { get; set; } = "https://api.sendgrid.com";
}
