using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Core.Configuration;

/// <summary>Sessão da equipe (NFR-08). Não é segredo; o valor existe para testes e E2E encurtarem o tempo.</summary>
public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    /// <summary>Minutos sem uso até a sessão expirar; padrão 30 (S16).</summary>
    [Range(1, 120)]
    public int SessionMinutes { get; set; } = 30;
}
