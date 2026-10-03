using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Core.Configuracao;

/// <summary>Sessão da equipe (NFR-08). Não é segredo; o valor existe para testes e E2E encurtarem o tempo.</summary>
public sealed class AutenticacaoOptions
{
    public const string SectionName = "Autenticacao";

    /// <summary>Minutos sem uso até a sessão expirar; padrão 30 (S16).</summary>
    [Range(1, 120)]
    public int SessaoMinutos { get; set; } = 30;
}
