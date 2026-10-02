using System.ComponentModel.DataAnnotations;

namespace GazetaMarketplace.Core.Configuracao;

/// <summary>Pasta dos arquivos de log, fora da raiz do site (ADR-010).</summary>
public sealed class LogStorageOptions
{
    public const string SectionName = "Logging";

    [Required]
    public string FileDirectory { get; set; }
}
